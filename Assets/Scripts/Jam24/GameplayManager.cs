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
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField, HideInInspector] private GameObject[] levelPrefabs;
        [SerializeField] private Transform levelContainer;
        // [FormerlySerializedAs("cam")]
        // [SerializeField] private Camera gameplayCamera;

        [Header("Actors")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject flipPrefab;
        [SerializeField, Min(0f)] private float flipRespawnDelay = .75f;

        [Header("Rare Fish School")]
        [SerializeField] private GameObject fishSchoolPrefab;
        [SerializeField, Range(0f, 1f)] private float fishSchoolBaseChance = .1f;
        [SerializeField, Range(0f, 1f)] private float fishSchoolMissBonus = .08f;
        [SerializeField, Range(0f, 1f)] private float fishSchoolMaximumChance = .6f;

        public GameObject CurrentLevel { get; private set; }
        public GameObject Player { get; private set; }
        public GameObject Flip { get; private set; }
        public int RemainingFlips { get; private set; }
        public int LevelCount => levelCatalog != null ? levelCatalog.Count : levelPrefabs?.Length ?? 0;
        public event Action<int> FlipCountChanged;

        private bool levelFinished;
        private bool flipRespawnDisabled;
        private LevelDefinition activeDefinition;
        private Coroutine flipRespawnRoutine;
        private int nextFlipSpawnIndex;
        private GameObject activeFishSchool;

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
            GameObject levelPrefab = GetLevelPrefab(levelIndex);

            ClearRuntimeObjects();
            levelFinished = false;

            if (GameFlow.Instance != null)
                GameFlow.Instance.ConfigureTotalLevels(LevelCount);

            Transform levelParent = levelContainer == null ? transform : levelContainer;
            ClearLevelContainer(levelParent);
            CurrentLevel = Instantiate(levelPrefab, levelParent);
            CurrentLevel.name = levelPrefab.name;

            LevelDefinition definition = CurrentLevel.GetComponent<LevelDefinition>();
            if (definition == null)
            {
                return FailLevelLoad(
                    $"Level '{CurrentLevel.name}' needs a LevelDefinition component with explicit references.");
            }

            if (!definition.TryValidate(out string definitionError))
                return FailLevelLoad($"Level '{CurrentLevel.name}' configuration is invalid:\n{definitionError}");
            activeDefinition = definition;
            flipRespawnDisabled = false;
            nextFlipSpawnIndex = 0;
            RemainingFlips = definition.StartingFlipCount;
            FlipCountChanged?.Invoke(RemainingFlips);

            Player = Instantiate(playerPrefab, definition.PlayerSpawn.position, definition.PlayerSpawn.rotation, transform);
            Player.name = playerPrefab.name;
            SpawnFlip();
            TrySpawnFishSchool(levelIndex);

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
            else if (!flipRespawnDisabled)
            {
                flipRespawnRoutine = StartCoroutine(RespawnFlip());
            }
            return true;
        }

        public bool IsFlip(GameObject candidate)
        {
            return Flip != null && candidate != null &&
                   (candidate == Flip || candidate.transform.IsChildOf(Flip.transform));
        }

        public bool TryLosePlayer(GameObject candidate)
        {
            if (levelFinished || Player == null || candidate == null) return false;
            if (candidate != Player && !candidate.transform.IsChildOf(Player.transform)) return false;

            levelFinished = true;
            GameFlow.Instance?.Lose();
            return true;
        }

        public bool TryLoseFromSeaweedTrap(GameObject candidate)
        {
            if (levelFinished || activeDefinition == null ||
                !activeDefinition.LoseWhenFlipTrappedBySeaweed || !IsFlip(candidate)) return false;

            levelFinished = true;
            Debug.Log("A Flip was trapped by seaweed. Level lost.", this);
            GameFlow.Instance?.Lose();
            return true;
        }

        /// <summary>
        /// Prevents the current level's Flip from being recreated after a thief
        /// shark has swept through. Loading a level resets this protection.
        /// </summary>
        public void DisableFlipRespawnForCurrentLevel()
        {
            flipRespawnDisabled = true;
            if (flipRespawnRoutine == null) return;

            StopCoroutine(flipRespawnRoutine);
            flipRespawnRoutine = null;
        }

        /// <summary>
        /// Removes the active Flip count when it is stolen by the thief shark,
        /// without scheduling a replacement Flip.
        /// </summary>
        public bool TryStealFlipPermanently(GameObject candidate)
        {
            if (levelFinished || Flip == null || candidate == null) return false;
            if (candidate != Flip && !candidate.transform.IsChildOf(Flip.transform)) return false;

            DisableFlipRespawnForCurrentLevel();
            Flip = null;
            RemainingFlips = Mathf.Max(0, RemainingFlips - 1);
            FlipCountChanged?.Invoke(RemainingFlips);
            Debug.Log($"Thief Shark stole a Flip permanently. Remaining Flips: {RemainingFlips}.", this);

            if (RemainingFlips <= 0)
            {
                levelFinished = true;
                GameFlow.Instance?.Lose();
            }

            return true;
        }

        public bool IsValidLevelIndex(int levelIndex) =>
            GetLevelPrefab(levelIndex) != null;

        private void HandleFlipFinished()
        {
            if (levelFinished) return;
            levelFinished = true;

            if (GameFlow.Instance == null) return;
            SaveData.CompleteLevel(GameFlow.Instance.CurrentLevel, LevelCount);
            GameFlow.Instance.Win();
        }

        private void SpawnFlip()
        {
            if (activeDefinition == null || levelFinished || flipRespawnDisabled) return;
            Transform spawn = activeDefinition.GetFlipSpawn(nextFlipSpawnIndex);
            if (spawn == null)
            {
                Debug.LogError($"Level '{activeDefinition.name}' has no valid Flip spawn.", activeDefinition);
                return;
            }
            nextFlipSpawnIndex++;

            Flip = Instantiate(flipPrefab, spawn.position, spawn.rotation, transform);
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

        private void TrySpawnFishSchool(int levelIndex)
        {
            if (fishSchoolPrefab == null || Player == null) return;

            bool shouldAppear;
            float chance;
            bool reusedLevelResult;
            if (fishSchoolBaseChance >= .999f)
            {
                // A 100% value is commonly used to preview/test the encounter.
                // It must override a cached miss from an earlier play session.
                shouldAppear = true;
                chance = 1f;
                reusedLevelResult = false;
            }
            else if (SaveData.TryGetFishSchoolResult(levelIndex, out bool previousResult))
            {
                shouldAppear = previousResult;
                chance = SaveData.GetFishSchoolNextChance(fishSchoolBaseChance);
                reusedLevelResult = true;
            }
            else
            {
                reusedLevelResult = false;
                chance = Mathf.Clamp(
                    SaveData.GetFishSchoolNextChance(fishSchoolBaseChance),
                    fishSchoolBaseChance,
                    fishSchoolMaximumChance);
                shouldAppear = UnityEngine.Random.value < chance;
                float nextChance = shouldAppear
                    ? fishSchoolBaseChance
                    : Mathf.Min(fishSchoolMaximumChance, chance + fishSchoolMissBonus);
                SaveData.SaveFishSchoolResult(levelIndex, shouldAppear, nextChance);
            }

            string rollDetails = reusedLevelResult ? "reused level result" : $"chance {chance:P0}";
            Debug.Log($"Fish School for level {levelIndex}: {(shouldAppear ? "appeared" : "missed")} " +
                      $"({rollDetails}).", this);
            if (!shouldAppear) return;

            activeFishSchool = Instantiate(
                fishSchoolPrefab,
                Player.transform.position,
                Quaternion.identity,
                transform);
            activeFishSchool.name = fishSchoolPrefab.name;
            FishSchool school = activeFishSchool.GetComponent<FishSchool>();
            if (school != null) school.Initialize(Player.transform);
        }

        private bool ValidateConfiguration(int levelIndex)
        {
            if (LevelCount == 0)
            {
                Debug.LogError("GameplayManager needs a Level Catalog with at least one level prefab.", this);
                return false;
            }

            if (GetLevelPrefab(levelIndex) == null)
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

        private GameObject GetLevelPrefab(int levelIndex)
        {
            if (levelCatalog != null)
                return levelCatalog.TryGetLevel(levelIndex, out GameObject prefab) ? prefab : null;

            return levelPrefabs != null && levelIndex >= 0 && levelIndex < levelPrefabs.Length
                ? levelPrefabs[levelIndex]
                : null;
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
            if (activeFishSchool != null) Destroy(activeFishSchool);
            CurrentLevel = null;
            activeDefinition = null;
            Player = null;
            Flip = null;
            activeFishSchool = null;
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
