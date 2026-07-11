using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Jam24
{
    public enum PuzzleMode { Countdown, Flowing, Won, Failed }

    /// <summary>Deterministic semi-physics environmental puzzle from the gameplay proposal.</summary>
    public sealed class SoleFlowPuzzle : MonoBehaviour
    {
        public event Action<int> ActionsChanged;
        public event Action<PuzzleMode> ModeChanged;
        public event Action<string> TutorialChanged;
        public event Action<int> StarsEarned;

        public int Actions { get; private set; }
        public int LevelNumber { get; private set; }
        public PuzzleMode Mode { get; private set; }
        public SoleLevel Level { get; private set; }

        private readonly List<EnvironmentMechanism> mechanisms = new();
        private readonly Stack<(EnvironmentMechanism mechanism, int state)> undo = new();
        private readonly List<GameObject> spawned = new();
        private Camera mainCamera;
        private Transform slipper;
        private SpriteRenderer slipperRenderer;
        private int resets;
        private int releases;
        private bool pearlCollected;
        private Coroutine flowRoutine;

        private void Start()
        {
            mainCamera = Camera.main;
            int index = GameFlow.Instance == null ? 0 : GameFlow.Instance.CurrentLevel;
            LoadLevel(index);
        }

        private void Update()
        {
            if (GameFlow.Instance != null && GameFlow.Instance.State == GameState.Paused)
            {
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) GameFlow.Instance.Resume();
                return;
            }
            foreach (EnvironmentMechanism mechanism in mechanisms) mechanism.TickPulse(Time.unscaledTime);
            HandleInput();
        }

        public void LoadLevel(int index)
        {
            Clear();
            LevelNumber = Mathf.Clamp(index, 0, SoleLevelCatalog.Count - 1) + 1;
            Level = SoleLevelCatalog.Get(index);
            BuildEnvironment();
            Actions = 0;
            Mode = PuzzleMode.Countdown;
            ActionsChanged?.Invoke(Actions);
            ModeChanged?.Invoke(Mode);
            TutorialChanged?.Invoke(Level.tutorial);
            Debug.Log($"Sole Flow: loaded level {LevelNumber}/10 '{Level.title}' with {Level.mechanisms.Length} mechanisms.", this);
            flowRoutine = StartCoroutine(BeginRealtimeFlow());
            if (PlayerPrefs.GetInt("jam24.automatedSmoke", 0) == 1)
            {
                PlayerPrefs.DeleteKey("jam24.automatedSmoke");
                StartCoroutine(RunAutomatedSmoke());
            }
        }

        private IEnumerator RunAutomatedSmoke()
        {
            yield return new WaitForSeconds(.2f);
            foreach (EnvironmentMechanism mechanism in mechanisms)
            {
                if (mechanism.Type == MechanismType.PulseCurrent)
                {
                    while (!mechanism.IsCorrect) yield return null;
                    continue;
                }
                while (!mechanism.IsCorrect) mechanism.Cycle();
            }
        }

        private IEnumerator BeginRealtimeFlow()
        {
            TutorialChanged?.Invoke("REAL-TIME — dòng chảy bắt đầu. Điều chỉnh môi trường trước khi dép đi tới!");
            yield return new WaitForSeconds(.85f);
            StartRealtimeFlow();
        }

        public bool CanInteract(EnvironmentMechanism mechanism)
        {
            return Mode == PuzzleMode.Countdown || Mode == PuzzleMode.Flowing;
        }

        public void MechanismChanged(EnvironmentMechanism mechanism, int oldState, bool countAction)
        {
            if (mechanism.Type == MechanismType.RockDiverter)
                foreach (EnvironmentMechanism linked in mechanisms)
                    if (linked.Type == MechanismType.PressureSwitch) linked.SetState(mechanism.IsCorrect ? linked.RequiredState : 0);
            if (countAction)
            {
                undo.Push((mechanism, oldState));
                Actions++;
                ActionsChanged?.Invoke(Actions);
            }
            TutorialChanged?.Invoke(mechanism.IsCorrect ? "Đúng hướng! Dép sẽ vượt qua cơ chế này." : "Dòng đang sai — đổi lại trước khi dép tới!");
        }

        private void StartRealtimeFlow()
        {
            if (Mode != PuzzleMode.Countdown) return;
            releases++;
            SoleAudio.Release();
            Debug.Log($"Sole Flow: REAL-TIME run {releases} started with {mechanisms.FindAll(m => m.IsCorrect).Count}/{mechanisms.Count} mechanisms currently correct.", this);
            SetMode(PuzzleMode.Flowing);
            TutorialChanged?.Invoke("REAL-TIME FLOW — tiếp tục điều khiển môi trường khi dép đang trôi!");
            flowRoutine = StartCoroutine(RunFlow());
        }

        public void Undo()
        {
            if ((Mode != PuzzleMode.Countdown && Mode != PuzzleMode.Flowing) || undo.Count == 0) return;
            var step = undo.Pop();
            step.mechanism.SetState(step.state);
            Actions = Mathf.Max(0, Actions - 1);
            ActionsChanged?.Invoke(Actions);
        }

        public void ResetPuzzle()
        {
            resets++;
            GameFlow.Instance?.PrepareRetry();
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            int index = LevelNumber - 1;
            LoadLevel(index);
            resets = Mathf.Max(1, resets);
        }

        private IEnumerator RunFlow()
        {
            Vector2 from = Level.start;
            for (int i = 0; i < Level.route.Length; i++)
            {
                Vector2 target = Level.route[i];
                yield return MoveSlipper(from, target, 1.05f);
                from = target;
                if (Level.hasPearl && i == 2) pearlCollected = true;

                if (i < mechanisms.Count && !mechanisms[i].IsCorrect)
                {
                    EnvironmentMechanism obstacle = mechanisms[i];
                    TutorialChanged?.Invoke($"NHANH! Điều chỉnh {obstacle.Type} trước khi dép mắc kẹt.");
                    float grace = 0f;
                    while (!obstacle.IsCorrect && grace < 2.25f)
                    {
                        grace += Time.deltaTime;
                        slipper.position = new Vector3(from.x + Mathf.Sin(grace * 9f) * .08f, from.y + Mathf.Sin(grace * 5f) * .06f, -.3f);
                        yield return null;
                    }
                    if (!obstacle.IsCorrect)
                    {
                        yield return FailAt(from, obstacle);
                        yield break;
                    }
                    TutorialChanged?.Invoke("Kịp lúc! Dòng chảy tiếp tục.");
                }
            }

            yield return MoveSlipper(from, Level.nest, 1.1f);
            yield return new WaitForSeconds(.2f);
            Win();
        }

        private IEnumerator MoveSlipper(Vector2 from, Vector2 to, float duration)
        {
            float time = 0f;
            Vector2 velocity = to - from;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float eased = t * t * (3f - 2f * t);
                Vector2 basePosition = Vector2.Lerp(from, to, eased);
                float bob = Mathf.Sin(t * Mathf.PI * 3f) * .13f;
                slipper.position = new Vector3(basePosition.x, basePosition.y + bob, -.3f);
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + Mathf.Sin(t * Mathf.PI * 2f) * 9f;
                slipper.rotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
        }

        private IEnumerator FailAt(Vector2 position, EnvironmentMechanism mechanism)
        {
            TutorialChanged?.Invoke($"Dép mắc tại {mechanism.Type}. Hãy RESET và đổi thiết lập.");
            Color original = slipperRenderer.color;
            for (int i = 0; i < 4; i++)
            {
                slipperRenderer.color = i % 2 == 0 ? new Color32(255,80,80,255) : original;
                yield return new WaitForSeconds(.16f);
            }
            slipperRenderer.color = original;
            SetMode(PuzzleMode.Failed);
            SoleAudio.Failure();
            Debug.Log($"Sole Flow: FAILED at {mechanism.Type}.", this);
            GameFlow.Instance?.Lose();
        }

        private void Win()
        {
            int stars = 1;
            if (Level.parActions == 0 ? releases == 1 : Actions <= Level.parActions) stars++;
            if (resets == 0 && (!Level.hasPearl || pearlCollected)) stars++;
            SetMode(PuzzleMode.Won);
            SoleAudio.Success();
            StarsEarned?.Invoke(stars);
            SaveData.CompleteLevel(GameFlow.Instance == null ? LevelNumber - 1 : GameFlow.Instance.CurrentLevel, Actions, stars, Level.slipperName);
            Debug.Log($"Sole Flow: WIN level {LevelNumber} with {Actions} actions and {stars} stars. Collected '{Level.slipperName}'.", this);
            GameFlow.Instance?.Win(Actions);
        }

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.zKey.wasPressedThisFrame) Undo();
                else if (keyboard.rKey.wasPressedThisFrame) ResetPuzzle();
                else if (keyboard.escapeKey.wasPressedThisFrame) GameFlow.Instance?.TogglePause();
                else
                {
                    KeyControl[] digits = { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key, keyboard.digit5Key,
                        keyboard.digit6Key, keyboard.digit7Key, keyboard.digit8Key, keyboard.digit9Key };
                    for (int i = 0; i < mechanisms.Count && i < digits.Length; i++)
                        if (digits[i].wasPressedThisFrame) { mechanisms[i].Cycle(); break; }
                }
            }

            bool pressed = Mouse.current?.leftButton.wasPressedThisFrame == true;
            Vector2 screen = Mouse.current == null ? default : Mouse.current.position.ReadValue();
            if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
            {
                pressed = true;
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            if (!pressed || mainCamera == null) return;
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -mainCamera.transform.position.z));
            Collider2D hit = Physics2D.OverlapPoint(world);
            hit?.GetComponent<EnvironmentMechanism>()?.Cycle();
        }

        private void BuildEnvironment()
        {
            CreateBackdropShapes();
            for (int i = 0; i < Level.mechanisms.Length; i++)
            {
                MechanismSpec spec = Level.mechanisms[i];
                var go = new GameObject($"{i + 1}_{spec.type}", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(EnvironmentMechanism));
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(spec.position.x, spec.position.y, -.15f);
                go.transform.localScale = Vector3.one * 1.05f;
                var labelObject = new GameObject("Label", typeof(TextMesh));
                labelObject.transform.SetParent(go.transform, false);
                labelObject.transform.localPosition = new Vector3(0, -1.1f, -.05f);
                labelObject.transform.localScale = Vector3.one * .19f;
                TextMesh label = labelObject.GetComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontSize = 32;
                label.characterSize = .08f;
                label.color = Color.white;
                go.GetComponent<EnvironmentMechanism>().Initialize(this, spec, SoleArt.ForMechanism(spec.type));
                mechanisms.Add(go.GetComponent<EnvironmentMechanism>());
                spawned.Add(go);
            }

            slipper = CreateWorldIcon("LostSlipper", Level.start, Color.white, .92f, SoleArt.Get(SoleSprite.Slipper)).transform;
            slipperRenderer = slipper.GetComponent<SpriteRenderer>();
            CreateWorldIcon("OctopusNest", Level.nest, Color.white, 1.45f, SoleArt.Get(SoleSprite.Nest));
            CreateWorldIcon("OctoEngineer", Level.nest + new Vector2(-1.5f,.25f), Color.white, 1.05f, SoleArt.Get(SoleSprite.Octopus));
            AddWorldLabel("OCTO'S NEST", Level.nest + Vector2.down * 1.35f, Color.white, 34);
            AddWorldLabel($"LOST: {Level.slipperName}", Level.start + Vector2.up * 1.05f, new Color32(255,235,173,255), 27);
        }

        private void CreateBackdropShapes()
        {
            Sprite square = MakeSprite("Solid", Color.white, 16, false);
            var seabed = CreateWorldIcon("Seabed", new Vector2(0,-3.75f), new Color32(210,165,103,255), 1, square);
            seabed.transform.localScale = new Vector3(15, .8f, 1);

            var preview = new GameObject("FlowPreview", typeof(LineRenderer));
            preview.transform.SetParent(transform, false);
            LineRenderer line = preview.GetComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = new Color(0.4f, .95f, 1f, .34f);
            line.startWidth = line.endWidth = .16f;
            line.positionCount = Level.route.Length + 2;
            line.SetPosition(0, Level.start);
            for (int i=0;i<Level.route.Length;i++) line.SetPosition(i+1, Level.route[i]);
            line.SetPosition(Level.route.Length+1, Level.nest);
            line.sortingOrder = -1;
            spawned.Add(preview);

            if (Level.hasPearl)
            {
                Vector2 pearlPos = Level.route[Mathf.Min(2, Level.route.Length - 1)] + Vector2.up * .55f;
                CreateWorldIcon("GoldenPearl", pearlPos, new Color32(255,220,88,255), .32f, MakeSprite("Pearl", Color.white, 32, true));
            }
        }

        private GameObject CreateWorldIcon(string name, Vector2 position, Color color, float scale, Sprite sprite)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(position.x, position.y, -.1f);
            go.transform.localScale = Vector3.one * scale;
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 3;
            spawned.Add(go);
            return go;
        }

        private void AddWorldLabel(string content, Vector2 position, Color color, int size)
        {
            var go = new GameObject(content, typeof(TextMesh));
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(position.x, position.y, -.2f);
            TextMesh text = go.GetComponent<TextMesh>();
            text.text = content;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = size;
            text.characterSize = .08f;
            text.color = color;
            spawned.Add(go);
        }

        private void SetMode(PuzzleMode value) { Mode = value; ModeChanged?.Invoke(value); }

        private void Clear()
        {
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            foreach (GameObject go in spawned) if (go != null) Destroy(go);
            spawned.Clear(); mechanisms.Clear(); undo.Clear();
            pearlCollected = false;
        }

        private static Sprite MakeSprite(string name, Color color, int size, bool circle)
        {
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear};
            var pixels = new Color32[size*size]; float c=(size-1)*.5f,r=size*.47f;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) pixels[y*size+x]=(!circle||Vector2.Distance(new Vector2(x,y),new Vector2(c,c))<=r)?color:Color.clear;
            texture.SetPixels32(pixels);texture.Apply();
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        }
    }
}
