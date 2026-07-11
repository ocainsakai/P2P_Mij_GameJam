using UnityEngine;

namespace Jam24
{
    /// <summary>Creates looping, decorative fish groups behind the level geometry.</summary>
    [DisallowMultipleComponent]
    public sealed class BackgroundFishSchool : MonoBehaviour
    {
        [Header("Fish")]
        [SerializeField] private Sprite[] fishSprites;
        [SerializeField, Range(1, 8)] private int groupCount = 4;
        [SerializeField, Range(2, 12)] private int fishPerGroup = 5;
        [SerializeField] private Vector2 scaleRange = new(.22f, .38f);
        [SerializeField] private Color tint = new(1f, 1f, 1f, .72f);
        [SerializeField] private int sortingOrder = -1;

        [Header("Swimming Area")]
        [SerializeField] private Vector2 areaCenter = new(0f, .25f);
        [SerializeField] private Vector2 areaSize = new(17f, 7.5f);
        [SerializeField] private Vector2 speedRange = new(.35f, .75f);
        [SerializeField, Range(0f, 1f)] private float leftSwimmingChance = .35f;
        [SerializeField, Min(0f)] private float verticalWobble = .18f;
        [SerializeField, Min(.1f)] private float wobbleFrequency = .8f;

        private School[] schools;

        private sealed class School
        {
            public Transform root;
            public float speed;
            public float direction;
            public float baseY;
            public float phase;
        }

        private void Awake()
        {
            CreateSchools();
        }

        private void Update()
        {
            if (schools == null) return;

            float halfWidth = areaSize.x * .5f;
            float left = areaCenter.x - halfWidth;
            float right = areaCenter.x + halfWidth;
            float padding = .8f;

            foreach (School school in schools)
            {
                if (school?.root == null) continue;

                Vector3 position = school.root.localPosition;
                position.x += school.speed * school.direction * Time.deltaTime;
                position.y = school.baseY + Mathf.Sin(Time.time * wobbleFrequency + school.phase) * verticalWobble;

                if (school.direction > 0f && position.x > right + padding)
                    position.x = left - padding;
                else if (school.direction < 0f && position.x < left - padding)
                    position.x = right + padding;

                school.root.localPosition = position;
            }
        }

        private void CreateSchools()
        {
            if (fishSprites == null || fishSprites.Length == 0) return;

            schools = new School[groupCount];
            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                GameObject groupObject = new($"Fish Group {groupIndex + 1}");
                Transform groupRoot = groupObject.transform;
                groupRoot.SetParent(transform, false);

                float direction = Random.value < leftSwimmingChance ? -1f : 1f;
                float baseY = Random.Range(areaCenter.y - areaSize.y * .5f, areaCenter.y + areaSize.y * .5f);
                groupRoot.localPosition = new Vector3(
                    Random.Range(areaCenter.x - areaSize.x * .5f, areaCenter.x + areaSize.x * .5f),
                    baseY,
                    -.25f);

                for (int fishIndex = 0; fishIndex < fishPerGroup; fishIndex++)
                    CreateFish(groupRoot, fishIndex, direction);

                schools[groupIndex] = new School
                {
                    root = groupRoot,
                    speed = Random.Range(speedRange.x, speedRange.y),
                    direction = direction,
                    baseY = baseY,
                    phase = Random.Range(0f, Mathf.PI * 2f)
                };
            }
        }

        private void CreateFish(Transform groupRoot, int index, float direction)
        {
            GameObject fishObject = new($"Fish {index + 1}");
            Transform fishTransform = fishObject.transform;
            fishTransform.SetParent(groupRoot, false);

            float row = index - (fishPerGroup - 1) * .5f;
            float spacing = Random.Range(.38f, .62f);
            fishTransform.localPosition = new Vector3(
                -direction * Mathf.Abs(row) * spacing + Random.Range(-.12f, .12f),
                row * .28f + Random.Range(-.12f, .12f),
                0f);

            float scale = Random.Range(scaleRange.x, scaleRange.y);
            fishTransform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = fishObject.AddComponent<SpriteRenderer>();
            renderer.sprite = fishSprites[Random.Range(0, fishSprites.Length)];
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;
            renderer.flipX = direction < 0f;
        }

        private void OnValidate()
        {
            areaSize.x = Mathf.Max(.1f, areaSize.x);
            areaSize.y = Mathf.Max(.1f, areaSize.y);
            scaleRange.x = Mathf.Max(.01f, scaleRange.x);
            scaleRange.y = Mathf.Max(scaleRange.x, scaleRange.y);
            speedRange.x = Mathf.Max(0f, speedRange.x);
            speedRange.y = Mathf.Max(speedRange.x, speedRange.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.25f, .8f, 1f, .45f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(areaCenter, areaSize);
        }
    }
}
