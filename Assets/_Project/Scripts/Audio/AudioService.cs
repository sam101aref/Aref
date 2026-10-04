using System.Collections.Generic;
using System.Threading.Tasks;
using Arash.Combat;
using Arash.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arash.Audio
{
    public enum MusicMood
    {
        None,
        Calm,
        Battle,
    }

    /// <summary>
    /// Plays the game's sound (F-35). Created automatically before the first scene and kept for the
    /// whole session. Sound effects follow gameplay events (bows, impacts, headshots); music is
    /// chosen by scene and crossfades between a calm and a battle track, both composed in a
    /// background thread at startup. Respects the Sound and Music settings.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        const int VoiceCount = 8;
        const float MusicVolume = 0.45f;
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
        MusicMood mood = MusicMood.None;
        int activeMusic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            if (instance != null)
                return;
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioService>();
        }

        /// <summary>Plays a sound effect with a little random pitch so repeats don't sound identical.</summary>
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

            calmTask = Task.Run(() => MusicComposer.Compose(1359, 150f, 16, false));
            battleTask = Task.Run(() => MusicComposer.Compose(622, 200f, 16, true));
        }

        void OnEnable()
        {
            Bow.AnyFired += OnFired;
            Arrow.AnyStuck += OnStuck;
            Health.AnyDamaged += OnDamaged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            Bow.AnyFired -= OnFired;
            Arrow.AnyStuck -= OnStuck;
            Health.AnyDamaged -= OnDamaged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
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

        void OnFired(Bow bow, Arrow arrow)
        {
            Play(Sfx.BowRelease, 0.8f);
            Play(Sfx.Whoosh, 0.4f);
        }

        void OnStuck(Arrow arrow, ArrowHit hit)
        {
            var zone = hit.Collider != null ? hit.Collider.GetComponent<HitZone>() : null;
            if (zone == null)
                Play(Sfx.HitWood, 0.6f);
            else if (zone.Zone == HitZoneType.Armor)
                Play(Sfx.HitMetal, 0.7f);
            else
                Play(Sfx.HitFlesh, 0.8f);
        }

        void OnDamaged(Health health, DamageInfo info, bool killed)
        {
            if (info.IsHeadshot)
                Play(Sfx.Headshot, 0.7f);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case SceneFlow.BattleScene:
                case SceneFlow.FinalFlightScene:
                    mood = MusicMood.Battle;
                    break;
                case SceneFlow.MainMenuScene:
                case SceneFlow.WorldMapScene:
                case SceneFlow.CutsceneScene:
                    mood = MusicMood.Calm;
                    break;
                default:
                    mood = MusicMood.None;
                    break;
            }
        }

        void UpdateMusic()
        {
            var wanted = mood == MusicMood.Battle ? battleClip : mood == MusicMood.Calm ? calmClip : null;
            var active = musicSources[activeMusic];
            if (wanted != null && active.clip != wanted)
            {
                // Crossfade: the other source takes the new track.
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
                var goal = i == activeMusic ? target : 0f;
                source.volume = Mathf.MoveTowards(source.volume, goal, step);
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
}
