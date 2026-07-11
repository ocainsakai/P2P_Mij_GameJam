using UnityEngine;

namespace Jam24
{
    public sealed class SoleAudio : MonoBehaviour
    {
        private static SoleAudio instance;
        private AudioSource source;
        private AudioClip click, release, success, failure;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = .35f;
            click = Tone("ValveClick", 520, .08f, .18f);
            release = Tone("WaterRelease", 260, .24f, .24f);
            success = Tone("SoleCollected", 760, .42f, .3f, 1.5f);
            failure = Tone("SoleStuck", 145, .32f, .28f, .72f);
        }

        public static void Click() => Ensure().source.PlayOneShot(Ensure().click);
        public static void Release() => Ensure().source.PlayOneShot(Ensure().release);
        public static void Success() => Ensure().source.PlayOneShot(Ensure().success);
        public static void Failure() => Ensure().source.PlayOneShot(Ensure().failure);

        private static SoleAudio Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("[SoleAudio]");
            return go.AddComponent<SoleAudio>();
        }

        private static AudioClip Tone(string name, float frequency, float duration, float volume, float endRatio = 1f)
        {
            const int rate = 22050;
            int samples = Mathf.CeilToInt(duration * rate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float progress = i / (float)samples;
                float hz = Mathf.Lerp(frequency, frequency * endRatio, progress);
                float envelope = Mathf.Sin(Mathf.PI * progress);
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
