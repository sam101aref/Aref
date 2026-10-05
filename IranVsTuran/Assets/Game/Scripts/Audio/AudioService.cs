using System.Collections.Generic;
using System.Threading.Tasks;
using IranVsTuran.Core;
using UnityEngine;

namespace IranVsTuran.Audio
{
    public enum Sfx
    {
        Click,
        Bow,
        Hit,
        Magic,
        Launch,
        Boom,
        Fire,
        Clash,
        Whoosh,
        Horn,
        Build,
        Coin,
        Roar,
        Die,
        LifeLost,
        Victory,
        Defeat,
        Reward,
    }

    public enum MusicMood
    {
        None,
        Calm,
        Battle,
    }

    /// <summary>
    /// Synthesized placeholder sound: effects are built at startup, and two music loops in Shur
    /// (calm with tar, and battle with daf) are composed on a background thread. Respects the
    /// Sound and Music settings.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        const int VoiceCount = 10;
        const float MusicVolume = 0.4f;
        const float FadeSeconds = 1.5f;

        static AudioService instance;

        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly AudioSource[] voices = new AudioSource[VoiceCount];
        AudioSource[] musicSources;
        int nextVoice;
        Task<float[]> calmTask;
        Task<float[]> battleTask;
        AudioClip calmClip;
        AudioClip battleClip;
        int activeMusic;

        public static MusicMood Mood = MusicMood.None;

        public static void Ensure(Transform parent)
        {
            if (instance != null)
                return;
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<AudioService>();
        }

        public static void Play(Sfx sfx, float volume = 1f)
        {
            if (instance != null)
                instance.PlayInternal(sfx, volume);
        }

        void Awake()
        {
            for (var i = 0; i < VoiceCount; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            musicSources = new[] { gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>() };
            foreach (var source in musicSources)
            {
                source.loop = true;
                source.playOnAwake = false;
                source.volume = 0f;
            }

            foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx)))
                clips[sfx] = ToClip(sfx.ToString(), SfxLibrary.Build(sfx));

            calmTask = Task.Run(() => MusicComposer.Compose(1404, 150f, 16, false));
            battleTask = Task.Run(() => MusicComposer.Compose(313, 210f, 16, true));
        }

        void Update()
        {
            if (calmClip == null && calmTask != null && calmTask.IsCompleted)
                calmClip = Finish(ref calmTask, "Music Calm");
            if (battleClip == null && battleTask != null && battleTask.IsCompleted)
                battleClip = Finish(ref battleTask, "Music Battle");
            UpdateMusic();
        }

        void PlayInternal(Sfx sfx, float volume)
        {
            AudioClip clip;
            if (!GameSettings.Sound || !clips.TryGetValue(sfx, out clip))
                return;
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % VoiceCount;
            voice.pitch = Random.Range(0.94f, 1.06f);
            voice.PlayOneShot(clip, volume);
        }

        void UpdateMusic()
        {
            var wanted = Mood == MusicMood.Battle ? battleClip : Mood == MusicMood.Calm ? calmClip : null;
            var active = musicSources[activeMusic];
            if (wanted != null && active.clip != wanted)
            {
                activeMusic = 1 - activeMusic;
                active = musicSources[activeMusic];
                active.clip = wanted;
                active.volume = 0f;
                active.Play();
            }

            var target = GameSettings.Music && wanted != null ? MusicVolume : 0f;
            var step = Time.unscaledDeltaTime / FadeSeconds * MusicVolume;
            for (var i = 0; i < musicSources.Length; i++)
            {
                var source = musicSources[i];
                source.volume = Mathf.MoveTowards(source.volume, i == activeMusic ? target : 0f, step);
                if (i != activeMusic && source.isPlaying && source.volume <= 0f)
                    source.Stop();
            }
        }

        static AudioClip Finish(ref Task<float[]> task, string name)
        {
            var samples = task.Exception == null ? task.Result : null;
            if (task.Exception != null)
                Debug.LogError("[Audio] Music generation failed: " + task.Exception.GetBaseException().Message);
            task = null;
            return samples != null ? ToClip(name, samples) : null;
        }

        static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, Synth.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    /// <summary>Builds each sound effect's samples from the synth toolkit.</summary>
    public static class SfxLibrary
    {
        public static float[] Build(Sfx sfx)
        {
            float[] s;
            switch (sfx)
            {
                case Sfx.Click:
                    s = Synth.HighPass(Synth.Noise(0.03f, 200f, 6), 0.5f);
                    break;
                case Sfx.Bow:
                    s = Synth.Tone(120f, 0.25f, 0.5f, 16f);
                    Synth.Mix(s, Synth.HighPass(Synth.Noise(0.05f, 60f, 1), 0.2f), 0, 0.5f);
                    break;
                case Sfx.Hit:
                    s = Synth.Tone(160f, 0.15f, 0.2f, 30f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.06f, 60f, 3), 0.25f), 0, 0.7f);
                    break;
                case Sfx.Magic:
                    s = Synth.Swell(Synth.LowPass(Synth.Noise(0.35f, 2f, 4), 0.15f));
                    Synth.Mix(s, Synth.Tone(PersianScale.Frequency(PersianScale.TonicD4, 9), 0.35f, 0f, 8f), 0, 0.25f);
                    break;
                case Sfx.Launch:
                    s = Synth.Tone(70f, 0.3f, 0.3f, 14f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.2f, 20f, 5), 0.1f), 0, 0.6f);
                    break;
                case Sfx.Boom:
                    s = Synth.Tone(55f, 0.6f, 0.8f, 7f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.5f, 9f, 7), 0.08f), 0, 1f);
                    break;
                case Sfx.Fire:
                    s = Synth.LowPass(Synth.Noise(0.7f, 4f, 8), 0.3f);
                    Synth.Mix(s, Synth.Tone(80f, 0.5f, 0.3f, 6f), 0, 0.4f);
                    break;
                case Sfx.Clash:
                    s = Synth.Tone(980f, 0.3f, 0f, 14f);
                    Synth.Mix(s, Synth.Tone(1530f, 0.3f, 0f, 16f), 0, 0.5f);
                    Synth.Mix(s, Synth.HighPass(Synth.Noise(0.05f, 80f, 9), 0.4f), 0, 0.5f);
                    break;
                case Sfx.Whoosh:
                    s = Synth.Swell(Synth.LowPass(Synth.Noise(0.5f, 0f, 10), 0.08f));
                    break;
                case Sfx.Horn:
                    s = Horn(PersianScale.Frequency(PersianScale.TonicD4, -7), 1.2f);
                    break;
                case Sfx.Build:
                    s = Synth.Tone(200f, 0.2f, 0.1f, 25f);
                    Synth.Mix(s, Synth.Tone(150f, 0.2f, 0.1f, 25f), Synth.Samples(0.12f), 0.9f);
                    Synth.Mix(s, Synth.LowPass(Synth.Noise(0.1f, 40f, 11), 0.3f), 0, 0.5f);
                    break;
                case Sfx.Coin:
                    s = Synth.Tone(1760f, 0.25f, 0f, 14f);
                    Synth.Mix(s, Synth.Tone(2637f, 0.25f, 0f, 14f), Synth.Samples(0.06f), 0.8f);
                    break;
                case Sfx.Roar:
                    s = Synth.LowPass(Synth.Noise(1.1f, 2.5f, 12), 0.06f);
                    Synth.Mix(s, Synth.Tone(70f, 1.1f, -0.3f, 2.5f), 0, 0.7f);
                    break;
                case Sfx.Die:
                    s = Synth.Tone(240f, 0.25f, -0.5f, 12f);
                    break;
                case Sfx.LifeLost:
                    s = Arpeggio(new[] { 2, 0 }, 0.12f, 0.6f, 40);
                    break;
                case Sfx.Victory:
                    s = Arpeggio(new[] { 0, 2, 4, 7, 9 }, 0.12f, 1.8f, 10);
                    break;
                case Sfx.Defeat:
                    s = Arpeggio(new[] { 4, 3, 1, 0, -3 }, 0.28f, 2.2f, 20);
                    break;
                default:
                    s = Arpeggio(new[] { 7, 9, 11, 14 }, 0.07f, 1f, 30);
                    break;
            }
            return Synth.FadeEdges(Synth.Normalize(s, 0.9f), 0.003f);
        }

        /// <summary>A karnay-like war horn: a buzzy tone that swells and falls.</summary>
        static float[] Horn(float frequency, float seconds)
        {
            var output = new float[Synth.Samples(seconds)];
            var phase = 0.0;
            for (var i = 0; i < output.Length; i++)
            {
                var t = i / (double)Synth.SampleRate;
                var envelope = System.Math.Min(1.0, t * 6.0) * System.Math.Max(0.0, 1.0 - t / seconds);
                phase += 2.0 * System.Math.PI * frequency * (1.0 + 0.01 * System.Math.Sin(t * 30.0)) / Synth.SampleRate;
                var saw = (phase / System.Math.PI) % 2.0 - 1.0;
                output[i] = (float)((System.Math.Sin(phase) * 0.6 + saw * 0.4) * envelope);
            }
            return Synth.LowPass(output, 0.25f);
        }

        static float[] Arpeggio(int[] degrees, float spacing, float seconds, int seed)
        {
            var s = new float[Synth.Samples(seconds)];
            for (var i = 0; i < degrees.Length; i++)
            {
                var frequency = PersianScale.Frequency(PersianScale.TonicD4, degrees[i]);
                Synth.Mix(s, Synth.Pluck(frequency, seconds - i * spacing, 0.996f, seed + i), Synth.Samples(i * spacing), 0.6f);
            }
            return s;
        }
    }
}
