using System.Collections.Generic;
using System.Threading.Tasks;
using Siavosh.Core;
using UnityEngine;

namespace Siavosh.Audio
{
    public enum MusicMood
    {
        None,
        Calm,
        Battle,
        Ride,
    }

    /// <summary>
    /// Plays the game's sound: synthesized effects, and three pieces of music in Dastgah-e Shur
    /// (calm tar, a battle with daf, a fast ride) composed in background threads at startup and
    /// crossfaded by mood. Respects the Sound and Music settings.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        const int VoiceCount = 10;
        const float MusicVolume = 0.42f;
        const float FadeSeconds = 1.5f;

        static AudioService instance;

        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly AudioSource[] voices = new AudioSource[VoiceCount];
        readonly Dictionary<MusicMood, Task<float[]>> tasks = new Dictionary<MusicMood, Task<float[]>>();
        readonly Dictionary<MusicMood, AudioClip> music = new Dictionary<MusicMood, AudioClip>();
        AudioSource[] musicSources;
        int nextVoice;
        int activeMusic;
        MusicMood mood = MusicMood.None;

        public static void Create(Transform parent)
        {
            if (instance != null)
                return;
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<AudioService>();
        }

        /// <summary>Plays a sound effect with a little random pitch so repeats don't sound identical.</summary>
        public static void Play(Sfx sfx, float volume = 1f)
        {
            if (instance != null)
                instance.PlayInternal(sfx, volume);
        }

        public static void SetMood(MusicMood mood)
        {
            if (instance != null)
                instance.mood = mood;
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

            tasks[MusicMood.Calm] = Task.Run(() => MusicComposer.Compose(1359, 150f, 16, false));
            tasks[MusicMood.Battle] = Task.Run(() => MusicComposer.Compose(622, 200f, 16, true));
            tasks[MusicMood.Ride] = Task.Run(() => MusicComposer.Compose(907, 260f, 16, true));
        }

        void Update()
        {
            foreach (var pair in new List<KeyValuePair<MusicMood, Task<float[]>>>(tasks))
            {
                if (!pair.Value.IsCompleted)
                    continue;
                tasks.Remove(pair.Key);
                if (pair.Value.Exception != null)
                    Debug.LogError("[Audio] Music generation failed: " + pair.Value.Exception.GetBaseException().Message);
                else
                    music[pair.Key] = ToClip("Music " + pair.Key, pair.Value.Result);
            }
            UpdateMusic();
        }

        void PlayInternal(Sfx sfx, float volume)
        {
            AudioClip clip;
            if (!Settings.Sound || !clips.TryGetValue(sfx, out clip))
                return;
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % VoiceCount;
            voice.pitch = Random.Range(0.94f, 1.06f);
            voice.PlayOneShot(clip, volume);
        }

        void UpdateMusic()
        {
            AudioClip wanted;
            music.TryGetValue(mood, out wanted);
            var active = musicSources[activeMusic];
            if (wanted != null && active.clip != wanted)
            {
                activeMusic = 1 - activeMusic;
                active = musicSources[activeMusic];
                active.clip = wanted;
                active.volume = 0f;
                active.Play();
            }

            var target = Settings.Music && wanted != null ? MusicVolume : 0f;
            var step = Time.unscaledDeltaTime / FadeSeconds * MusicVolume;
            for (var i = 0; i < musicSources.Length; i++)
            {
                var source = musicSources[i];
                var goal = i == activeMusic ? target : 0f;
                source.volume = Mathf.MoveTowards(source.volume, goal, step);
                if (i != activeMusic && source.isPlaying && source.volume <= 0f)
                    source.Stop();
            }
        }

        static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, Synth.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
