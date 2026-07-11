using UnityEngine;

namespace Jam24
{
    /// <summary>
    /// Decorative school of fish that follows the player and smoothly changes
    /// between animated circle, heart, V and wave formations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FishSchool : MonoBehaviour
    {
        private enum Formation { Circle, Heart, VShape, Wave, SharkArrow }

        [Header("Fish")]
        [SerializeField] private Sprite fishSprite;
        [SerializeField, Range(3, 30)] private int fishCount = 14;
        [SerializeField, Min(.01f)] private float fishScale = .2f;
        [SerializeField] private Color fishTint = Color.white;
        [SerializeField] private int sortingOrder = 6;

        [Header("Following")]
        [SerializeField] private Vector2 followOffset = new(-1.25f, .65f);
        [SerializeField, Min(.01f)] private float followSmoothTime = .45f;

        [Header("Formations")]
        [SerializeField, Min(.1f)] private float formationSize = 1.15f;
        [SerializeField, Min(.1f)] private float formationChangeInterval = 4f;
        [SerializeField, Min(.1f)] private float formationTransitionSpeed = 3.5f;
        [SerializeField, Min(0f)] private float circleRotationSpeed = 35f;
        [SerializeField, Min(0f)] private float individualWobble = .06f;

        private Transform target;
        private Rigidbody2D targetBody;
        private Transform[] fish;
        private SpriteRenderer[] fishRenderers;
        private Formation formation;
        private Vector3 followVelocity;
        private Vector3 previousSchoolPosition;
        private float formationTimer;
        private float facingDirection = 1f;
        private Transform sharkActivationZone;
        private float nextZoneSearchTime;

        public void Initialize(Transform followTarget)
        {
            target = followTarget;
            targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
            if (target != null)
            {
                facingDirection = targetBody != null && targetBody.linearVelocity.x < 0f ? -1f : 1f;
                transform.position = GetFollowPosition();
                previousSchoolPosition = transform.position;
                if (formation == Formation.SharkArrow && FindNearestSharkActivationZone() == null)
                    formation = Formation.Circle;
            }
        }

        private void Awake()
        {
            CreateFish();
            formation = (Formation)Random.Range(0, System.Enum.GetValues(typeof(Formation)).Length);
            formationTimer = formationChangeInterval;
            previousSchoolPosition = transform.position;
        }

        private void Start()
        {
            if (target != null) return;
            OctopusPlayerMovement player = FindAnyObjectByType<OctopusPlayerMovement>();
            if (player != null) Initialize(player.transform);
        }

        private void Update()
        {
            if (target == null)
            {
                OctopusPlayerMovement player = FindAnyObjectByType<OctopusPlayerMovement>();
                if (player != null) Initialize(player.transform);
                return;
            }

            UpdateFacingDirection();
            transform.position = Vector3.SmoothDamp(
                transform.position,
                GetFollowPosition(),
                ref followVelocity,
                followSmoothTime);

            formationTimer -= Time.deltaTime;
            if (formationTimer <= 0f)
            {
                SelectNextFormation();
                formationTimer = formationChangeInterval;
            }

            AnimateFormation();
            previousSchoolPosition = transform.position;
        }

        private void CreateFish()
        {
            fish = new Transform[fishCount];
            fishRenderers = new SpriteRenderer[fishCount];

            for (int i = 0; i < fishCount; i++)
            {
                GameObject fishObject = new($"Fish {i + 1}");
                Transform fishTransform = fishObject.transform;
                fishTransform.SetParent(transform, false);
                fishTransform.localPosition = (Vector3)(Random.insideUnitCircle * .15f);
                float sizeVariation = Mathf.Lerp(.82f, 1.15f, (i % 5) / 4f);
                fishTransform.localScale = Vector3.one * fishScale * sizeVariation;

                SpriteRenderer renderer = fishObject.AddComponent<SpriteRenderer>();
                renderer.sprite = fishSprite;
                renderer.color = fishTint;
                renderer.sortingOrder = sortingOrder + i % 3;

                fish[i] = fishTransform;
                fishRenderers[i] = renderer;
            }
        }

        private Vector3 GetFollowPosition()
        {
            float horizontalOffset = -Mathf.Abs(followOffset.x) * facingDirection;
            return target.position + new Vector3(horizontalOffset, followOffset.y, 0f);
        }

        private void UpdateFacingDirection()
        {
            float horizontalSpeed = targetBody != null
                ? targetBody.linearVelocity.x
                : target.position.x - transform.position.x;
            if (Mathf.Abs(horizontalSpeed) > .08f) facingDirection = Mathf.Sign(horizontalSpeed);
        }

        private void SelectNextFormation()
        {
            int formationCount = System.Enum.GetValues(typeof(Formation)).Length;
            Formation next = formation;
            int attempts = 0;
            while (next == formation && attempts < 10)
            {
                next = (Formation)Random.Range(0, formationCount);
                if (next == Formation.SharkArrow && FindNearestSharkActivationZone() == null)
                    next = formation;
                attempts++;
            }
            formation = next;
        }

        private void AnimateFormation()
        {
            float blend = 1f - Mathf.Exp(-formationTransitionSpeed * Time.deltaTime);
            float schoolVelocityX = (transform.position.x - previousSchoolPosition.x) /
                                    Mathf.Max(Time.deltaTime, .0001f);

            for (int i = 0; i < fish.Length; i++)
            {
                Vector2 slot = GetFormationPosition(i, fish.Length);
                float wobblePhase = Time.time * 2.1f + i * 1.37f;
                slot += new Vector2(Mathf.Cos(wobblePhase), Mathf.Sin(wobblePhase * 1.13f)) * individualWobble;

                Vector3 before = fish[i].localPosition;
                fish[i].localPosition = Vector3.Lerp(before, slot, blend);
                float localDirection = fish[i].localPosition.x - before.x;
                float swimDirection = Mathf.Abs(localDirection) > .002f ? localDirection : schoolVelocityX;
                if (Mathf.Abs(swimDirection) > .01f)
                    fishRenderers[i].flipX = swimDirection < 0f;
            }
        }

        private Vector2 GetFormationPosition(int index, int count)
        {
            float linearNormalized = count <= 1 ? 0f : index / (float)(count - 1);
            float closedNormalized = count <= 0 ? 0f : index / (float)count;
            float angle = closedNormalized * Mathf.PI * 2f;

            switch (formation)
            {
                case Formation.Circle:
                {
                    float rotation = Time.time * circleRotationSpeed * Mathf.Deg2Rad;
                    float circleAngle = angle + rotation;
                    return new Vector2(Mathf.Cos(circleAngle), Mathf.Sin(circleAngle)) * formationSize;
                }
                case Formation.Heart:
                {
                    float sin = Mathf.Sin(angle);
                    float x = 16f * sin * sin * sin;
                    float y = 13f * Mathf.Cos(angle)
                              - 5f * Mathf.Cos(2f * angle)
                              - 2f * Mathf.Cos(3f * angle)
                              - Mathf.Cos(4f * angle);
                    float pulse = 1f + Mathf.Sin(Time.time * 2f) * .035f;
                    return new Vector2(x / 16f, y / 17f) * (formationSize * pulse);
                }
                case Formation.VShape:
                {
                    float x = Mathf.Lerp(-formationSize, formationSize, linearNormalized);
                    float y = Mathf.Abs(x) * .7f - formationSize * .45f;
                    return new Vector2(x, y);
                }
                case Formation.Wave:
                {
                    float x = Mathf.Lerp(-formationSize, formationSize, linearNormalized);
                    float y = Mathf.Sin(linearNormalized * Mathf.PI * 2f + Time.time * 2f) * formationSize * .42f;
                    return new Vector2(x, y);
                }
                default:
                    return GetSharkArrowPosition(index, count);
            }
        }

        private Vector2 GetSharkArrowPosition(int index, int count)
        {
            Transform zone = GetSharkActivationZone();
            if (zone == null)
            {
                float fallbackX = Mathf.Lerp(-formationSize, formationSize, count <= 1 ? 0f : index / (float)(count - 1));
                return new Vector2(fallbackX, 0f);
            }

            int headFishCount = Mathf.Clamp(count / 2, 5, 9);
            Vector2 point;
            if (index == 0)
            {
                point = new Vector2(formationSize, 0f);
            }
            else if (index < headFishCount)
            {
                int pair = (index + 1) / 2;
                int pairCount = Mathf.Max(1, headFishCount / 2);
                float amount = pair / (float)pairCount;
                float side = index % 2 == 0 ? -1f : 1f;
                point = new Vector2(
                    formationSize * (1f - amount * .88f),
                    side * formationSize * amount * .68f);
            }
            else
            {
                int shaftIndex = index - headFishCount;
                int shaftCount = Mathf.Max(1, count - headFishCount);
                float amount = shaftCount <= 1 ? 0f : shaftIndex / (float)(shaftCount - 1);
                point = new Vector2(
                    Mathf.Lerp(-formationSize, formationSize * .2f, amount),
                    (shaftIndex % 2 == 0 ? -.045f : .045f) * formationSize);
            }

            Vector2 worldDirection = zone.position - transform.position;
            Vector2 localDirection = transform.InverseTransformDirection(worldDirection);
            float rotation = Mathf.Atan2(localDirection.y, localDirection.x);
            float cos = Mathf.Cos(rotation);
            float sin = Mathf.Sin(rotation);
            return new Vector2(
                point.x * cos - point.y * sin,
                point.x * sin + point.y * cos);
        }

        private Transform GetSharkActivationZone()
        {
            if (sharkActivationZone != null && sharkActivationZone.gameObject.activeInHierarchy)
                return sharkActivationZone;
            if (Time.time < nextZoneSearchTime) return null;
            return FindNearestSharkActivationZone();
        }

        private Transform FindNearestSharkActivationZone()
        {
            nextZoneSearchTime = Time.time + 1f;
            sharkActivationZone = null;
            float closestDistance = float.PositiveInfinity;
            Vector3 origin = target != null ? target.position : transform.position;

            SharkDetectionZone[] stalkerZones = FindObjectsByType<SharkDetectionZone>(
                FindObjectsInactive.Exclude);
            foreach (SharkDetectionZone zone in stalkerZones)
                ConsiderActivationZone(zone.transform, origin, ref closestDistance);

            ThiefSharkDetectionZone[] thiefZones = FindObjectsByType<ThiefSharkDetectionZone>(
                FindObjectsInactive.Exclude);
            foreach (ThiefSharkDetectionZone zone in thiefZones)
                ConsiderActivationZone(zone.transform, origin, ref closestDistance);

            return sharkActivationZone;
        }

        private void ConsiderActivationZone(Transform candidate, Vector3 origin, ref float closestDistance)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy) return;
            float distance = (candidate.position - origin).sqrMagnitude;
            if (distance >= closestDistance) return;
            closestDistance = distance;
            sharkActivationZone = candidate;
        }

        private void OnValidate()
        {
            fishCount = Mathf.Clamp(fishCount, 3, 30);
            fishScale = Mathf.Max(.01f, fishScale);
            followSmoothTime = Mathf.Max(.01f, followSmoothTime);
            formationSize = Mathf.Max(.1f, formationSize);
            formationChangeInterval = Mathf.Max(.1f, formationChangeInterval);
            formationTransitionSpeed = Mathf.Max(.1f, formationTransitionSpeed);
            circleRotationSpeed = Mathf.Max(0f, circleRotationSpeed);
            individualWobble = Mathf.Max(0f, individualWobble);
        }
    }
}
