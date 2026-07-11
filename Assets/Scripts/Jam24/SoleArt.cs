using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    public enum SoleSprite { Octopus, Slipper, Nest, Jet, Bubbles, Rock, Shell, Seaweed }

    public static class SoleArt
    {
        private static Texture2D keyedTexture;
        private static readonly Dictionary<SoleSprite, Sprite> Cache = new();

        public static Sprite Get(SoleSprite id)
        {
            if (Cache.TryGetValue(id, out Sprite sprite)) return sprite;
            EnsureTexture();
            if (keyedTexture == null) return null;

            int cellWidth = keyedTexture.width / 4;
            int cellHeight = keyedTexture.height / 2;
            int index = (int)id;
            int column = index % 4;
            int rowFromTop = index / 4;
            var rect = new Rect(column * cellWidth, (1 - rowFromTop) * cellHeight, cellWidth, cellHeight);
            sprite = Sprite.Create(keyedTexture, rect, new Vector2(.5f, .5f), cellWidth / 2f, 0, SpriteMeshType.FullRect);
            sprite.name = id.ToString();
            Cache[id] = sprite;
            return sprite;
        }

        public static Sprite ForMechanism(MechanismType type) => type switch
        {
            MechanismType.BubbleColumn => Get(SoleSprite.Bubbles),
            MechanismType.RockDiverter or MechanismType.PressureSwitch => Get(SoleSprite.Rock),
            MechanismType.BounceShell => Get(SoleSprite.Shell),
            MechanismType.SeaweedGate => Get(SoleSprite.Seaweed),
            _ => Get(SoleSprite.Jet)
        };

        private static void EnsureTexture()
        {
            if (keyedTexture != null) return;
            Texture2D source = Resources.Load<Texture2D>("SoleFlow/soleflow_sheet");
            if (source == null) return;

            keyedTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = "SoleFlowSprites_Alpha",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                bool chroma = p.g > 165 && p.g > p.r * 1.35f && p.g > p.b * 1.35f;
                if (chroma) p.a = 0;
                pixels[i] = p;
            }
            keyedTexture.SetPixels32(pixels);
            keyedTexture.Apply(false, false);
        }
    }
}
