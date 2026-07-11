using UnityEngine;

namespace Jam24
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BeachBackdrop : MonoBehaviour
    {
        private void Awake()
        {
            Texture2D texture = Resources.Load<Texture2D>("BeachFlow/beach_background");
            Camera camera = Camera.main;
            if (texture == null || camera == null) return;

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(.72f, .92f, 1f, .62f);
            renderer.sortingOrder = -100;

            float worldHeight = camera.orthographicSize * 2f;
            float worldWidth = worldHeight * camera.aspect;
            Vector2 spriteSize = sprite.bounds.size;
            float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
            transform.localScale = Vector3.one * scale;
            transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 5f);
        }
    }
}
