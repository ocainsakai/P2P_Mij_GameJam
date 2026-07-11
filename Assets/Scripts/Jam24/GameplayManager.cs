using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jam24
{
    public sealed class GameplayManager : MonoBehaviour
    {
        [Header("Levels")]
        [SerializeField] private GameObject[] levelPrefabs;
        [SerializeField] private Transform levelContainer;
        [FormerlySerializedAs("cam")]
        [SerializeField] private CinemachineCamera gameplayCamera;
        [SerializeField] private CinemachineTargetGroup cameraTargetGroup;
        [SerializeField, Min(0f)] private float playerCameraWeight = 1f;
        [SerializeField, Min(0f)] private float flipCameraWeight = 1f;
        [SerializeField, Min(0f)] private float playerCameraRadius = 1f;
        [SerializeField, Min(0f)] private float flipCameraRadius = 0.6f;
        [SerializeField] private CutsceneManager cutsceneManager;

        [Header("Actors")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject flipPrefab;

        public GameObject CurrentLevel { get; private set; }
        public GameObject Player { get; private set; }
        public GameObject Flip { get; private set; }
        public int LevelCount => levelPrefabs?.Length ?? 0;

        private bool levelFinished;
        private Coroutine deathRoutine;

        private void Awake()
        {
            if (cutsceneManager == null)
            {
                cutsceneManager = GetComponent<CutsceneManager>();
                if (cutsceneManager == null) cutsceneManager = gameObject.AddComponent<CutsceneManager>();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GetComponent<GameplayCheatConsole>() == null)
                gameObject.AddComponent<GameplayCheatConsole>();
#endif
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
            CurrentLevel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            CurrentLevel.transform.localScale = Vector3.one;

            LevelDefinition definition = CurrentLevel.GetComponent<LevelDefinition>();
            if (definition == null)
            {
                return FailLevelLoad(
                    $"Level '{CurrentLevel.name}' needs a LevelDefinition component with explicit references.");
            }

            if (!definition.TryValidate(out string definitionError))
                return FailLevelLoad($"Level '{CurrentLevel.name}' configuration is invalid:\n{definitionError}");

            Player = Instantiate(playerPrefab, definition.PlayerSpawn.position, definition.PlayerSpawn.rotation, transform);
            Player.name = playerPrefab.name;

            Flip = Instantiate(flipPrefab, definition.FlipSpawn.position, definition.FlipSpawn.rotation, transform);
            Flip.name = flipPrefab.name;

            SeaweedTrap2D[] seaweedTraps = CurrentLevel.GetComponentsInChildren<SeaweedTrap2D>(true);
            foreach (SeaweedTrap2D trap in seaweedTraps)
                trap.ConfigureTargets(Player, Flip);

            foreach (Transform finisherTransform in definition.Finishers)
            {
                Collider2D trigger = finisherTransform.GetComponent<Collider2D>();
                trigger.isTrigger = true;
                FinishZone finishZone = finisherTransform.GetComponent<FinishZone>();
                if (finishZone == null) finishZone = finisherTransform.gameObject.AddComponent<FinishZone>();
                finishZone.Configure(Flip, HandleFlipFinished);
            }

            if (definition.DeadZones != null)
            {
                foreach (Transform deadZoneTransform in definition.DeadZones)
                {
                    Collider2D trigger = deadZoneTransform.GetComponent<Collider2D>();
                    trigger.isTrigger = true;
                    DeadZone deadZone = deadZoneTransform.GetComponent<DeadZone>();
                    if (deadZone == null) deadZone = deadZoneTransform.gameObject.AddComponent<DeadZone>();
                    deadZone.Configure(Player, Flip,
                        () => HandlePlayerEnteredDeadZone(definition),
                        HandleFlipEnteredDeadZone);
                }
            }

            SetupCameraTargets();

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

        private void HandleFlipEnteredDeadZone()
        {
            if (levelFinished) return;
            levelFinished = true;

            if (Flip != null)
            {
                cameraTargetGroup?.RemoveMember(Flip.transform);
                if (Flip.TryGetComponent(out Rigidbody2D body)) body.simulated = false;
                Flip.SetActive(false);
            }

            GameFlow.Instance?.Lose();
        }

        private void HandlePlayerEnteredDeadZone(LevelDefinition definition)
        {
            if (levelFinished) return;
            levelFinished = true;
            if (deathRoutine != null) StopCoroutine(deathRoutine);
            deathRoutine = StartCoroutine(PlayPlayerDeath(definition.PlayerDeathCutscene));
        }

        private IEnumerator PlayPlayerDeath(CutsceneSequence cutscene)
        {
            if (Player != null)
            {
                cameraTargetGroup?.RemoveMember(Player.transform);
                if (Player.TryGetComponent(out OctopusPlayerMovement movement)) movement.enabled = false;
                if (Player.TryGetComponent(out Rigidbody2D body))
                {
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                    body.simulated = false;
                }
            }

            if (cutscene != null && cutsceneManager != null)
            {
                yield return cutsceneManager.Play(cutscene);
            }
            else if (Player != null)
            {
                SpriteRenderer renderer = Player.GetComponentInChildren<SpriteRenderer>();
                Vector3 initialScale = Player.transform.localScale;
                Color initialColor = renderer == null ? Color.white : renderer.color;
                const float duration = 0.8f;
                float elapsed = 0f;

                while (elapsed < duration && Player != null)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    Player.transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
                    if (renderer != null)
                    {
                        Color color = initialColor;
                        color.a = 1f - progress;
                        renderer.color = color;
                    }
                    yield return null;
                }
            }

            if (Player != null) Player.SetActive(false);
            deathRoutine = null;
            GameFlow.Instance?.Lose();
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
            Player = null;
            Flip = null;
            ClearCameraTargets();
            return false;
        }

        private void ClearRuntimeObjects()
        {
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }
            ClearCameraTargets();
            if (CurrentLevel != null) Destroy(CurrentLevel);
            if (Player != null) Destroy(Player);
            if (Flip != null) Destroy(Flip);
            CurrentLevel = null;
            Player = null;
            Flip = null;
        }

        private void SetupCameraTargets()
        {
            if (cameraTargetGroup == null)
            {
                var groupObject = new GameObject("GameplayCameraTargetGroup");
                groupObject.transform.SetParent(transform, false);
                cameraTargetGroup = groupObject.AddComponent<CinemachineTargetGroup>();
            }

            ClearCameraTargets();
            cameraTargetGroup.AddMember(Player.transform, playerCameraWeight, playerCameraRadius);
            cameraTargetGroup.AddMember(Flip.transform, flipCameraWeight, flipCameraRadius);
            cameraTargetGroup.RotationMode = CinemachineTargetGroup.RotationModes.Manual;
            cameraTargetGroup.transform.rotation = Quaternion.identity;
            cameraTargetGroup.DoUpdate();

            if (gameplayCamera == null)
                gameplayCamera = FindFirstObjectByType<CinemachineCamera>();

            if (gameplayCamera == null)
            {
                Debug.LogWarning("Gameplay Camera is not assigned; level loaded without camera follow.", this);
                return;
            }

            gameplayCamera.Follow = cameraTargetGroup.transform;
            gameplayCamera.Target.CustomLookAtTarget = false;
            gameplayCamera.PreviousStateIsValid = false;
            gameplayCamera.UpdateCameraState(Vector3.up, -1f);
        }

        private void ClearCameraTargets()
        {
            if (cameraTargetGroup != null)
                cameraTargetGroup.Targets.Clear();

            if (gameplayCamera != null)
                gameplayCamera.PreviousStateIsValid = false;
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
