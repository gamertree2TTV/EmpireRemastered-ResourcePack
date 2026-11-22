using UnityEngine;

namespace Glutherz
{
    /// <summary>Kleine, im Code erzeugte Soundeffekte – das Spiel braucht keine Audiodateien.</summary>
    public static class Sfx
    {
        public static AudioClip Swing, Hit, Clang, Hurt, LevelUp, Pickup;
        static AudioSource source;
        const int Rate = 44100;

        public static void Init(AudioSource src)
        {
            source = src;
            Swing = Make("swing", 0.12f, (t, n) => Noise() * (1f - n) * 0.25f * Mathf.Sin(n * Mathf.PI));
            Hit = Make("hit", 0.1f, (t, n) => (Noise() * 0.6f + Mathf.Sin(t * 2f * Mathf.PI * 120f) * 0.6f) * (1f - n) * (1f - n));
            Clang = Make("clang", 0.5f, (t, n) => (Mathf.Sin(t * 2f * Mathf.PI * 880f) + 0.6f * Mathf.Sin(t * 2f * Mathf.PI * 1318f) + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 2217f)) * 0.3f * Mathf.Exp(-n * 7f));
            Hurt = Make("hurt", 0.18f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (160f - 80f * n)) * 0.6f * (1f - n));
            Pickup = Make("pickup", 0.15f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (600f + 600f * n)) * 0.3f * (1f - n));
            LevelUp = Make("levelup", 0.6f, (t, n) =>
            {
                float f = n < 0.33f ? 523f : (n < 0.66f ? 659f : 784f);
                return Mathf.Sin(t * 2f * Mathf.PI * f) * 0.3f * (1f - n * 0.6f);
            });
        }

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (source != null && clip != null) source.PlayOneShot(clip, volume);
        }

        static float Noise() { return Random.value * 2f - 1f; }

        delegate float Wave(float t, float n);

        static AudioClip Make(string name, float seconds, Wave wave)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, i / (float)count), -1f, 1f);
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
