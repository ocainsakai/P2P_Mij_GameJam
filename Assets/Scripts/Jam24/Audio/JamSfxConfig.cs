using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jam24
{
    [CreateAssetMenu(fileName = "JamSfxConfig", menuName = "Jam24/Audio/SFX Config")]
    public sealed class JamSfxConfig : ScriptableObject
    {
        [Serializable]
        private sealed class UiEntry
        {
            public UiSfxType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volumeScale = 1f;
            [Min(0f)] public float startOffset;
        }

        [Serializable]
        private sealed class GameEntry
        {
            public GameSfxType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volumeScale = 1f;
        }

        [Serializable]
        private sealed class MusicEntry
        {
            public MusicType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volumeScale = 1f;
        }

        [Serializable]
        private sealed class AmbienceEntry
        {
            public AmbienceType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volumeScale = 1f;
        }

        [SerializeField] private List<UiEntry> uiEntries = new();
        [SerializeField] private List<GameEntry> gameEntries = new();
        [SerializeField] private List<MusicEntry> musicEntries = new();
        [SerializeField] private List<AmbienceEntry> ambienceEntries = new();

        private Dictionary<UiSfxType, UiEntry> uiLookup;
        private Dictionary<GameSfxType, GameEntry> gameLookup;
        private Dictionary<MusicType, MusicEntry> musicLookup;
        private Dictionary<AmbienceType, AmbienceEntry> ambienceLookup;

        public bool TryGet(
            UiSfxType type,
            out AudioClip clip,
            out float volumeScale,
            out float startOffset)
        {
            if (uiLookup == null) BuildLookups();
            if (uiLookup.TryGetValue(type, out UiEntry entry) && entry.clip != null)
            {
                clip = entry.clip;
                volumeScale = entry.volumeScale;
                startOffset = entry.startOffset;
                return true;
            }

            clip = null;
            volumeScale = 1f;
            startOffset = 0f;
            return false;
        }

        public bool TryGet(GameSfxType type, out AudioClip clip, out float volumeScale)
        {
            if (gameLookup == null) BuildLookups();
            if (gameLookup.TryGetValue(type, out GameEntry entry) && entry.clip != null)
            {
                clip = entry.clip;
                volumeScale = entry.volumeScale;
                return true;
            }

            clip = null;
            volumeScale = 1f;
            return false;
        }

        public bool TryGet(MusicType type, out AudioClip clip, out float volumeScale)
        {
            if (musicLookup == null) BuildLookups();
            if (musicLookup.TryGetValue(type, out MusicEntry entry) && entry.clip != null)
            {
                clip = entry.clip;
                volumeScale = entry.volumeScale;
                return true;
            }
            clip = null;
            volumeScale = 1f;
            return false;
        }

        public bool TryGet(AmbienceType type, out AudioClip clip, out float volumeScale)
        {
            if (ambienceLookup == null) BuildLookups();
            if (ambienceLookup.TryGetValue(type, out AmbienceEntry entry) && entry.clip != null)
            {
                clip = entry.clip;
                volumeScale = entry.volumeScale;
                return true;
            }
            clip = null;
            volumeScale = 1f;
            return false;
        }

        private void OnEnable() => BuildLookups();

        private void BuildLookups()
        {
            uiLookup = new Dictionary<UiSfxType, UiEntry>();
            foreach (UiEntry entry in uiEntries)
                if (entry != null) uiLookup[entry.type] = entry;

            gameLookup = new Dictionary<GameSfxType, GameEntry>();
            foreach (GameEntry entry in gameEntries)
                if (entry != null) gameLookup[entry.type] = entry;

            musicLookup = new Dictionary<MusicType, MusicEntry>();
            foreach (MusicEntry entry in musicEntries)
                if (entry != null) musicLookup[entry.type] = entry;

            ambienceLookup = new Dictionary<AmbienceType, AmbienceEntry>();
            foreach (AmbienceEntry entry in ambienceEntries)
                if (entry != null) ambienceLookup[entry.type] = entry;
        }
    }
}
