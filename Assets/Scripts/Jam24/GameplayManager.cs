using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jam24
{
    public sealed class GameplayManager : MonoBehaviour
    {
        public static GameplayManager Instance { get; private set; }

        [Header("Levels")]
        [SerializeField] private GameObject[] levelPrefabs;
        [SerializeField] private Transform levelContainer;
        // [FormerlySerializedAs("cam")]
        // [SerializeField] private Camera gameplayCamera;

        [Header("Actors")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject flipPrefab;
        [SerializeField, Min(0f)] private float flipRespawnDelay = .75f;

        public GameObject CurrentLevel { get; private set; }
        public GameObject Player { get; private set; }
        public GameObject Flip { get; private set; }
        public int RemainingFlips { get; private set; }
        public int LevelCount => levelPrefabs?.Length ?? 0;
        public event Action<int> FlipCountChanged;

        private bool levelFinished;
        private LevelDefinition activeDefinition;
        private Coroutine flipRespawnRoutine;

        // private void LateUpdate()
        // {
        //     if (gameplayCamera == null || Player == null) return;

        //     Vector3 cameraPosition = gameplayCamera.transform.position;
        //     Vector3 playerPosition = Player.transform.position;
        //     gameplayCamera.transform.position = new Vector3(
        //         playerPosition.x,
        //         playerPosition.y,
        //         cameraPosition.z);
        // }

        private void Awake()
        {
            Instance = this;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GetComponent<GameplayCheatConsole>() == null)
                gameObject.AddComponent<GameplayCheatConsole>();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            LoadCurrentLevel();
        }

        public bool LoadCurrentLevel()
        {
            int levelIndex = GameFlow.Instance == null ? 0 : GameFlow.Instance.CurrentLevel;
            return LoadLevel(levelIndex);
        }

        public bool LoadLevel(int levelIndex)
        {
            if (!ValidateConfiguration(levelIndex)) return false;

            ClearRuntimeObjects();
            levelFinished = false;

            if (GameFlow.Instance != null)
                GameFlow.Instance.ConfigureTotalLevels(levelPrefabs.Length);

            Transform levelParent = levelContainer == null ? transform : levelContainer;
            ClearLevelContainer(levelParent);
            CurrentLevel = Instantiate(levelPrefabs[levelIndex], levelParent);
            CurrentLevel.name = levelPrefabs[levelIndex].name;

            LevelDefinition definition = CurrentLevel.GetComponent<LevelDefinition>();
            if (definition == null)
            {
                return FailLevelLoad(
                    $"Level '{CurrentLevel.name}' needs a LevelDefinition component with explicit references.");
            }

            if (!definition.TryValidate(out string definitionError))
                return FailLevelLoad($"Level '{CurrentLevel.name}' configuration is invalid:\n{definitionError}");
            activeDefinition = definition;
            RemainingFlips = definition.StartingFlipCount;
            FlipCountChanged?.Invoke(RemainingFlips);

            Player = Instantiate(playerPrefab, definition.PlayerSpawn.position, definition.PlayerSpawn.rotation, transform);
            Player.name = playerPrefab.name;
            SpawnFlip();

            // if (gameplayCamera == null) gameplayCamera = Camera.main;
            // if (gameplayCamera == null)
            //     Debug.LogWarning("Gameplay Camera is not assigned; level loaded without camera follow.", this);

            return true;
        }

        public bool TryConsumeFlip(GameObject candidate)
        {
            if (levelFinished || Flip == null || candidate == null) return false;
            if (candidate != Flip && !candidate.transform.IsChildOf(Flip.transform)) return false;

            GameObject lostFlip = Flip;
            Flip = null;
            RemainingFlips = Mathf.Max(0, RemainingFlips - 1);
            FlipCountChanged?.Invoke(RemainingFlips);
            Debug.Log($"Grab Crab stole a Flip. Remaining Flips: {RemainingFlips}.", this);
            Destroy(lostFlip);

            if (RemainingFlips <= 0)
            {
                levelFinished = true;
                GameFlow.Instance?.Lose();
            }
            else
            {
                flipRespawnRoutine = StartCoroutine(RespawnFlip());
            }
            return true;
        }

        public bool IsValidLevelIndex(int levelIndex) =>
            levelPrefabs != null && levelIndex >= 0 && levelIndex < levelPrefabs.Length && levelPrefabs[levelIndex] != null;

        private void HandleFlipFinished()
        {
            if (levelFinished) return;
            levelFinished = true;

            if (GameFlow.Instance == null) return;
            SaveData.CompleteLevel(GameFlow.Instance.CurrentLevel, levelPrefabs.Length);
            GameFlow.Instance.Win();
        }

        private void SpawnFlip()
        {
            if (activeDefinition == null || levelFinished) return;
            Flip = Instantiate(flipPrefab, activeDefinition.FlipSpawn.position, activeDefinition.FlipSpawn.rotation, transform);
            Flip.name = flipPrefab.name;

            foreach (Transform finisherTransform in activeDefinition.Finishers)
            {
                Collider2D trigger = finisherTransform.GetComponent<Collider2D>();
                trigger.isTrigger = true;
                FinishZone finishZone = finisherTransform.GetComponent<FinishZone>();
                if (finishZone == null) finishZone = finisherTransform.gameObject.AddComponent<FinishZone>();
                finishZone.Configure(Flip, HandleFlipFinished);
            }
        }

        private IEnumerator RespawnFlip()
        {
            if (flipRespawnDelay > 0f) yield return new WaitForSeconds(flipRespawnDelay);
            flipRespawnRoutine = null;
            SpawnFlip();
        }

        private bool ValidateConfiguration(int levelIndex)
        {
            if (levelPrefabs == null || levelPrefabs.Length == 0)
            {
                Debug.LogError("GameplayManager needs at least one level prefab.", this);
                return false;
            }

            if (levelIndex < 0 || levelIndex >= levelPrefabs.Length || levelPrefabs[levelIndex] == null)
            {
                Debug.LogError($"No level prefab is assigned at index {levelIndex}.", this);
                return false;
            }

            if (playerPrefab == null || flipPrefab == null)
            {
                Debug.LogError("GameplayManager needs both Player Prefab and Flip Prefab.", this);
                return false;
            }

            return true;
        }

        private bool FailLevelLoad(string message)
        {
            Debug.LogError(message, this);
            if (CurrentLevel != null) Destroy(CurrentLevel);
            CurrentLevel = null;
            activeDefinition = null;
            Player = null;
            Flip = null;
            return false;
        }

        private void ClearRuntimeObjects()
        {
            if (flipRespawnRoutine != null)
            {
                StopCoroutine(flipRespawnRoutine);
                flipRespawnRoutine = null;
            }
            if (CurrentLevel != null) Destroy(CurrentLevel);
            if (Player != null) Destroy(Player);
            if (Flip != null) Destroy(Flip);
            CurrentLevel = null;
            activeDefinition = null;
            Player = null;
            Flip = null;
        }

        private void ClearLevelContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                GameObject child = container.GetChild(i).gameObject;
                if (child != CurrentLevel) Destroy(child);
            }
        }

    }
}
