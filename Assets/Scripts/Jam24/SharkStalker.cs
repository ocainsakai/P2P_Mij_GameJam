using System.Collections;
using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// Patrols a fixed horizontal range, warns intruding players, then lunges and bites.
    /// Leaving the detection trigger before the countdown ends cancels the attack.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SharkStalker : MonoBehaviour
    {
        private enum SharkState { Patrolling, Warning, Attacking, Recovering }

        [Header("Patrol")]
        [SerializeField] private Rigidbody2D sharkBody;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField, Min(0.1f)] private float patrolDistance = 3f;
        [SerializeField, Min(0.1f)] private float patrolSpeed = 1.6f;

        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float warningDuration = 5f;
        [SerializeField, Min(0f)] private float warningLeadTime = 3f;
        [SerializeField] private SharkDetectionZone detectionZone;
        [SerializeField] private TextMesh warningLabel;

        [Header("Attack")]
        [SerializeField, Min(0.1f)] private float attackSpeed = 8f;
        [SerializeField, Min(0.05f)] private float biteDistance = 0.55f;
        [SerializeField, Min(0f)] private float bitePause = 0.35f;

        [Header("Attack Video")]
        [SerializeField] private CutsceneManager cutsceneManager;
        [SerializeField] private CutsceneSequence attackSequence;

        private OctopusPlayerMovement target;
        private Vector2 patrolOrigin;
        private int patrolDirection = 1;
        private float warningTimer;
        private SharkState state;
        private Color normalColor;
        private bool loseCutscenePlayed;

        private void Awake()
        {
            if (sharkBody == null) sharkBody = GetComponentInChildren<Rigidbody2D>(true);
            if (bodyRenderer == null && sharkBody != null) bodyRenderer = sharkBody.GetComponent<SpriteRenderer>();
            if (sharkBody == null || bodyRenderer == null)
            {
                Debug.LogError("Shark Stalker needs a SharkBody child with Rigidbody2D and SpriteRenderer.", this);
                enabled = false;
                return;
            }

            normalColor = bodyRenderer.color;

            if (detectionZone == null) detectionZone = GetComponentInChildren<SharkDetectionZone>(true);
            if (warningLabel == null) warningLabel = GetComponentInChildren<TextMesh>(true);
            if (cutsceneManager == null) cutsceneManager = GetComponent<CutsceneManager>();
            if (attackSequence == null) attackSequence = GetComponent<CutsceneSequence>();
            detectionZone?.SetOwner(this);

            sharkBody.bodyType = RigidbodyType2D.Kinematic;
            sharkBody.gravityScale = 0f;
            sharkBody.freezeRotation = true;
            SetWarningVisible(false);
        }

        private void OnEnable()
        {
            GameFlow.StateChanged += HandleGameStateChanged;
            patrolOrigin = sharkBody.position;
            state = SharkState.Patrolling;
            target = null;
            warningTimer = 0f;
            loseCutscenePlayed = false;
        }

        private void OnDisable()
        {
            GameFlow.StateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState gameState)
        {
            if (gameState != GameState.Lose || loseCutscenePlayed) return;
            loseCutscenePlayed = true;
            StartCoroutine(PlayLoseCutscene());
        }

        private IEnumerator PlayLoseCutscene()
        {
            SetWarningVisible(false);
            if (cutsceneManager != null && attackSequence != null)
                yield return cutsceneManager.Play(attackSequence);
        }

        private void Update()
        {
            if (state != SharkState.Warning) return;
            if (target == null)
            {
                CancelWarning();
                return;
            }

            warningTimer += Time.deltaTime;
            float remaining = Mathf.Max(0f, warningDuration - warningTimer);
            bool shouldWarn = remaining <= Mathf.Min(warningLeadTime, warningDuration);
            SetWarningVisible(shouldWarn);
            if (shouldWarn) SetWarningText($"WARNING!\nSHARK IN {remaining:0.0}s");

            if (warningTimer >= warningDuration) BeginAttack();
        }

        private void FixedUpdate()
        {
            if (GameFlow.Instance != null && GameFlow.Instance.State == GameState.Paused) return;

            switch (state)
            {
                case SharkState.Patrolling:
                case SharkState.Warning:
                    Patrol();
                    break;
                case SharkState.Attacking:
                    ChaseTarget();
                    break;
            }
        }

        public void PlayerEntered(OctopusPlayerMovement player)
        {
            if (player == null || state != SharkState.Patrolling) return;
            target = player;
            warningTimer = 0f;
            state = SharkState.Warning;
            bool warnImmediately = warningDuration <= warningLeadTime;
            SetWarningVisible(warnImmediately);
            if (warnImmediately) SetWarningText($"WARNING!\nSHARK IN {warningDuration:0.0}s");
            Debug.Log($"Shark Stalker: player entered danger zone. Attack in {warningDuration:0.0}s.", this);
        }

        public void PlayerExited(OctopusPlayerMovement player)
        {
            if (state == SharkState.Warning && target == player) CancelWarning();
        }

        private void Patrol()
        {
            float targetX = patrolOrigin.x + patrolDistance * patrolDirection;
            Vector2 destination = new(targetX, patrolOrigin.y);
            Vector2 next = Vector2.MoveTowards(sharkBody.position, destination, patrolSpeed * Time.fixedDeltaTime);
            sharkBody.MovePosition(next);

            if (Mathf.Abs(next.x - targetX) < .02f) patrolDirection *= -1;
            bodyRenderer.flipX = patrolDirection < 0;
        }

        private void BeginAttack()
        {
            if (target == null)
            {
                CancelWarning();
                return;
            }

            state = SharkState.Attacking;
            SetWarningVisible(true);
            SetWarningText("SHARK ATTACK!");
            bodyRenderer.color = new Color(1f, .42f, .42f, 1f);
            Debug.Log("Shark Stalker: countdown expired, attacking player.", this);
        }

        private void ChaseTarget()
        {
            if (target == null)
            {
                ReturnToPatrol();
                return;
            }

            Vector2 targetPosition = target.transform.position;
            Vector2 next = Vector2.MoveTowards(sharkBody.position, targetPosition, attackSpeed * Time.fixedDeltaTime);
            sharkBody.MovePosition(next);
            bodyRenderer.flipX = targetPosition.x < sharkBody.position.x;

            if (Vector2.Distance(next, targetPosition) <= biteDistance) StartCoroutine(BitePlayer());
        }

        private IEnumerator BitePlayer()
        {
            if (state == SharkState.Recovering) yield break;
            state = SharkState.Recovering;
            SetWarningText("CHOMP!");

            OctopusPlayerMovement bittenPlayer = target;
            target = null;
            if (bittenPlayer != null)
            {
                bittenPlayer.enabled = false;
                Rigidbody2D playerBody = bittenPlayer.GetComponent<Rigidbody2D>();
                if (playerBody != null) playerBody.linearVelocity = Vector2.zero;
            }

            if (bitePause > 0f) yield return new WaitForSeconds(bitePause);
            loseCutscenePlayed = true;
            if (cutsceneManager != null && attackSequence != null)
                yield return cutsceneManager.Play(attackSequence);

            SetWarningVisible(false);
            bodyRenderer.color = normalColor;

            if (GameFlow.Instance != null)
            {
                Debug.Log("Shark Stalker: player was bitten. Game over.", this);
                GameFlow.Instance.Lose();
            }
            else
            {
                Debug.LogWarning("Shark Stalker could not find GameFlow; pausing as a lose fallback.", this);
                Time.timeScale = 0f;
            }
        }

        private void CancelWarning()
        {
            Debug.Log("Shark Stalker: player escaped before the countdown ended.", this);
            target = null;
            warningTimer = 0f;
            state = SharkState.Patrolling;
            SetWarningVisible(false);
        }

        private void ReturnToPatrol()
        {
            target = null;
            warningTimer = 0f;
            state = SharkState.Patrolling;
            bodyRenderer.color = normalColor;
            SetWarningVisible(false);
        }

        private void SetWarningVisible(bool visible)
        {
            if (warningLabel != null) warningLabel.gameObject.SetActive(visible);
        }

        private void SetWarningText(string value)
        {
            if (warningLabel != null) warningLabel.text = value;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, .75f, .1f, .8f);
            Vector3 center = Application.isPlaying
                ? (Vector3)patrolOrigin
                : sharkBody != null ? sharkBody.transform.position : transform.position;
            Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);
            Gizmos.DrawWireSphere(center + Vector3.left * patrolDistance, .15f);
            Gizmos.DrawWireSphere(center + Vector3.right * patrolDistance, .15f);
        }
    }
}
