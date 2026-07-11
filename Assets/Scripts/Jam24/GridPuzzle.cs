using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jam24
{
    /// <summary>Compact Flow puzzle: drag between matching beach-colored endpoints.</summary>
    public sealed class GridPuzzle : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField, Min(.2f)] private float cellSize = 1f;
        [SerializeField, Range(.05f, .45f)] private float lineWidth = .26f;
        [SerializeField] private Color cellColor = new(1f, .94f, .75f, .92f);
        [SerializeField] private Color gridColor = new(.08f, .35f, .52f, .35f);

        public event Action<int> MovesChanged;
        public event Action<int, int> ConnectionsChanged;
        public int Moves { get; private set; }
        public int LevelNumber { get; private set; }
        public int ConnectedCount { get; private set; }
        public int PairCount => level?.pairs?.Length ?? 0;

        private readonly Dictionary<Vector2Int, int> occupied = new();
        private readonly List<Vector2Int>[] paths = new List<Vector2Int>[5];
        private readonly LineRenderer[] lines = new LineRenderer[5];
        private readonly List<GameObject> visuals = new();
        private LevelDefinition level;
        private Camera mainCamera;
        private int activePair = -1;
        private bool dragging;
        private bool finished;

        private void Start()
        {
            mainCamera = Camera.main;
            int index = GameFlow.Instance == null ? 0 : GameFlow.Instance.CurrentLevel;
            LoadLevel(index);
        }

        private void Update()
        {
            if (finished || (GameFlow.Instance != null && GameFlow.Instance.State != GameState.Playing)) return;
            HandleShortcuts();
            HandlePointer();
        }

        public void LoadLevel(int index)
        {
            ClearBoard();
            LevelNumber = Mathf.Clamp(index, 0, FlowLevelCatalog.Count - 1) + 1;
            level = FlowLevelCatalog.Get(index);
            BuildBoard();
            SetMoves(0);
            ConnectionsChanged?.Invoke(0, PairCount);
            Debug.Log($"Beach Flow: loaded level {LevelNumber}/{FlowLevelCatalog.Count} ({level.width}x{level.height}, {PairCount} flows).", this);
        }

        public void ResetPuzzle() => LoadLevel(LevelNumber - 1);

        public void Undo()
        {
            for (int i = PairCount - 1; i >= 0; i--)
            {
                if (paths[i] == null || paths[i].Count == 0) continue;
                ClearPath(i);
                SetMoves(Mathf.Max(0, Moves - 1));
                RefreshConnections();
                return;
            }
        }

        private void HandleShortcuts()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) GameFlow.Instance?.TogglePause();
            else if (keyboard.rKey.wasPressedThisFrame) ResetPuzzle();
            else if (keyboard.zKey.wasPressedThisFrame) Undo();
        }

        private void HandlePointer()
        {
            Vector2 screen = default;
            bool down = false, held = false, up = false;
            if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
                down = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                held = Touchscreen.current.primaryTouch.press.isPressed;
                up = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
            else if (Mouse.current != null)
            {
                screen = Mouse.current.position.ReadValue();
                down = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                up = Mouse.current.leftButton.wasReleasedThisFrame;
            }

            if (mainCamera == null || (!down && !held && !up)) return;
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -mainCamera.transform.position.z));
            Vector2Int cell = WorldToCell(world);
            if (down) BeginPath(cell);
            else if (held && dragging) ExtendPath(cell);
            if (up || (!held && dragging)) EndPath();
        }

        private void BeginPath(Vector2Int cell)
        {
            int pair = EndpointPairAt(cell);
            if (pair < 0) return;

            ClearPath(pair);
            activePair = pair;
            dragging = true;
            paths[pair] = new List<Vector2Int> { cell };
            occupied[cell] = pair;
            UpdateLine(pair);
        }

        private void ExtendPath(Vector2Int cell)
        {
            if (!Inside(cell) || activePair < 0) return;
            List<Vector2Int> path = paths[activePair];
            Vector2Int last = path[^1];
            if (cell == last) return;
            if (Mathf.Abs(cell.x - last.x) + Mathf.Abs(cell.y - last.y) != 1) return;

            if (path.Count > 1 && cell == path[^2])
            {
                occupied.Remove(last);
                path.RemoveAt(path.Count - 1);
                UpdateLine(activePair);
                return;
            }

            int endpoint = EndpointPairAt(cell);
            if (endpoint >= 0 && endpoint != activePair) return;
            if (occupied.TryGetValue(cell, out int owner) && owner != activePair) return;
            if (path.Contains(cell)) return;

            path.Add(cell);
            occupied[cell] = activePair;
            UpdateLine(activePair);
        }

        private void EndPath()
        {
            if (!dragging) return;
            dragging = false;
            if (activePair >= 0 && paths[activePair].Count > 1) SetMoves(Moves + 1);
            activePair = -1;
            RefreshConnections();
            CheckWin();
        }

        private void BuildBoard()
        {
            transform.position = new Vector3(-(level.width - 1) * cellSize * .5f, -(level.height - 1) * cellSize * .5f, 0);
            Sprite square = MakeSprite("FlowCell", cellColor, 64, false);
            Sprite dot = MakeSprite("FlowDot", Color.white, 64, true);

            for (int y = 0; y < level.height; y++)
            for (int x = 0; x < level.width; x++)
            {
                var go = new GameObject($"Cell_{x}_{y}", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0);
                go.transform.localScale = Vector3.one * cellSize * .9f;
                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.color = ((x + y) & 1) == 0 ? cellColor : Color.Lerp(cellColor, gridColor, .12f);
                renderer.sortingOrder = 0;
                visuals.Add(go);
            }

            for (int i = 0; i < PairCount; i++)
            {
                CreateEndpoint(level.pairs[i].start, level.pairs[i].color, dot, i);
                CreateEndpoint(level.pairs[i].end, level.pairs[i].color, dot, i);
                paths[i] = new List<Vector2Int>();
                var lineObject = new GameObject($"Flow_{level.pairs[i].name}", typeof(LineRenderer));
                lineObject.transform.SetParent(transform, false);
                var line = lineObject.GetComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = line.endColor = level.pairs[i].color;
                line.startWidth = line.endWidth = cellSize * lineWidth;
                line.numCapVertices = 8;
                line.numCornerVertices = 8;
                line.sortingOrder = 2;
                lines[i] = line;
                visuals.Add(lineObject);
            }
        }

        private void CreateEndpoint(Vector2Int cell, Color color, Sprite sprite, int pair)
        {
            var go = new GameObject($"Endpoint_{pair}_{cell.x}_{cell.y}", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(cell.x * cellSize, cell.y * cellSize, -.05f);
            go.transform.localScale = Vector3.one * cellSize * .62f;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 4;
            visuals.Add(go);
        }

        private void ClearPath(int pair)
        {
            if (pair < 0 || paths[pair] == null) return;
            foreach (Vector2Int cell in paths[pair])
                if (occupied.TryGetValue(cell, out int owner) && owner == pair) occupied.Remove(cell);
            paths[pair].Clear();
            UpdateLine(pair);
        }

        private void UpdateLine(int pair)
        {
            LineRenderer line = lines[pair];
            if (line == null) return;
            line.positionCount = paths[pair].Count;
            for (int i = 0; i < paths[pair].Count; i++) line.SetPosition(i, CellToWorld(paths[pair][i]));
        }

        private void RefreshConnections()
        {
            ConnectedCount = 0;
            for (int i = 0; i < PairCount; i++) if (IsConnected(i)) ConnectedCount++;
            ConnectionsChanged?.Invoke(ConnectedCount, PairCount);
        }

        private bool IsConnected(int pair)
        {
            if (paths[pair] == null || paths[pair].Count < 2) return false;
            FlowPair p = level.pairs[pair];
            Vector2Int first = paths[pair][0], last = paths[pair][^1];
            return (first == p.start && last == p.end) || (first == p.end && last == p.start);
        }

        private void CheckWin()
        {
            if (ConnectedCount != PairCount || (level.requireFullBoard && occupied.Count < level.width * level.height)) return;
            finished = true;
            GameFlow.Instance?.Win(Moves);
        }

        private int EndpointPairAt(Vector2Int cell)
        {
            for (int i = 0; i < PairCount; i++)
                if (level.pairs[i].start == cell || level.pairs[i].end == cell) return i;
            return -1;
        }

        private bool Inside(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < level.width && c.y < level.height;
        private Vector2Int WorldToCell(Vector3 world) => new(Mathf.RoundToInt((world.x - transform.position.x) / cellSize), Mathf.RoundToInt((world.y - transform.position.y) / cellSize));
        private Vector3 CellToWorld(Vector2Int cell) => transform.position + new Vector3(cell.x * cellSize, cell.y * cellSize, -.1f);

        private void SetMoves(int value) { Moves = value; MovesChanged?.Invoke(value); }

        private void ClearBoard()
        {
            foreach (GameObject go in visuals) if (go != null) Destroy(go);
            visuals.Clear();
            occupied.Clear();
            for (int i = 0; i < paths.Length; i++) { paths[i] = null; lines[i] = null; }
            finished = dragging = false;
            activePair = -1;
            ConnectedCount = 0;
        }

        private static Sprite MakeSprite(string name, Color color, int size, bool circle)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            float radius = size * .47f, center = (size - 1) * .5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool visible = !circle || Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) <= radius;
                pixels[y * size + x] = visible ? color : Color.clear;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }
    }
}
