using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Arash.Combat
{
    public enum RealtimeKind
    {
        /// <summary>Stop marching raiders before they reach the palisade (F-23).</summary>
        Waves,
        /// <summary>Keep a companion alive; shoot enemy arrows out of the air (F-24).</summary>
        Escort,
        /// <summary>Hit every target with limited arrows without hurting anyone (F-25).</summary>
        Trial,
    }

    [Serializable]
    public class WaveSpawn
    {
        [Min(1)] public int count = 3;
        [Tooltip("Seconds between raiders in this wave.")]
        public float interval = 2.5f;
        [Tooltip("Seconds before the wave starts.")]
        public float startDelay = 2f;
        public float speed = 1.4f;
        public float health = 40f;
        [Tooltip("Mounted raiders are faster and tougher.")]
        public bool mounted;
    }

    /// <summary>
    /// Real-time battles: the player shoots freely with a short reload while the mode runs its own
    /// rules. The camera holds a wide view instead of following arrows.
    /// </summary>
    public class RealtimeBattle : MonoBehaviour, IBattleMode
    {
        [SerializeField] BattleCamera battleCamera;
        [SerializeField, Tooltip("Seconds between player shots.")]
        float reloadTime = 0.6f;
        [SerializeField] float cameraSize = 7.5f;
        [SerializeField] Vector2 cameraOffset = new Vector2(10f, 3.5f);
        [SerializeField] float endDelay = 1.2f;

        public event Action<bool> BattleEnded;
        /// <summary>Raised when lives, time, targets or arrows change (for the HUD).</summary>
        public event Action StatusChanged;

        public RealtimeKind Kind { get; private set; }
        public bool IsOver { get; private set; }
        public int ArrowsUsed { get; private set; }

        // Waves
        public int Lives { get; private set; }
        public int MaxLives { get; private set; }
        public int Wave { get; private set; }
        public int WaveCount { get { return waves != null ? waves.Count : 0; } }
        // Escort
        public float TimeLeft { get; private set; }
        // Trial
        public int TargetsLeft { get { return targets != null ? targets.Count(t => t != null && !t.IsHit) : 0; } }
        public int ArrowsLeft { get { return Mathf.Max(0, arrowLimit - ArrowsUsed); } }

        public float PlayerCondition
        {
            get
            {
                switch (Kind)
                {
                    case RealtimeKind.Waves: return MaxLives > 0 ? Lives / (float)MaxLives : 0f;
                    case RealtimeKind.Escort: return protectee != null ? protectee.Health.Fraction : 0f;
                    default: return arrowLimit > 0 ? ArrowsLeft / (float)arrowLimit : 0f;
                }
            }
        }

        Combatant player;
        AimController aim;
        bool started;

        List<WaveSpawn> waves;
        Func<WaveSpawn, Walker> spawnWalker;
        readonly List<Walker> walkers = new List<Walker>();
        bool allSpawned;

        Combatant protectee;
        List<Combatant> archers;
        float fireInterval;
        float surviveSeconds;

        List<TrialTarget> targets;
        int arrowLimit;
        readonly List<Arrow> flying = new List<Arrow>();

        public void ConfigureWaves(Combatant playerCombatant, AimController playerAim, List<WaveSpawn> waveList,
            int lives, Func<WaveSpawn, Walker> spawner)
        {
            Setup(RealtimeKind.Waves, playerCombatant, playerAim);
            waves = waveList;
            spawnWalker = spawner;
            Lives = MaxLives = Mathf.Max(1, lives);
        }

        public void ConfigureEscort(Combatant playerCombatant, AimController playerAim, Combatant companion,
            List<Combatant> enemyArchers, float secondsBetweenShots, float secondsToSurvive)
        {
            Setup(RealtimeKind.Escort, playerCombatant, playerAim);
            protectee = companion;
            archers = enemyArchers;
            fireInterval = Mathf.Max(1f, secondsBetweenShots);
            surviveSeconds = secondsToSurvive;
            TimeLeft = secondsToSurvive;
        }

        public void ConfigureTrial(Combatant playerCombatant, AimController playerAim, List<TrialTarget> trialTargets,
            int arrows, IEnumerable<Health> bystanders)
        {
            Setup(RealtimeKind.Trial, playerCombatant, playerAim);
            targets = trialTargets;
            arrowLimit = Mathf.Max(1, arrows);
            foreach (var target in targets)
                target.Hit += t => OnTargetHit();
            foreach (var bystander in bystanders)
                bystander.Damaged += (info, killed) => Finish(false); // hurting a villager fails the trial
        }

        void Setup(RealtimeKind kind, Combatant playerCombatant, AimController playerAim)
        {
            Kind = kind;
            player = playerCombatant;
            aim = playerAim;
        }

        void Start()
        {
            if (aim != null)
                aim.CancelTurn();
        }

        public void Begin()
        {
            if (started || player == null)
                return;
            started = true;

            if (battleCamera != null)
            {
                battleCamera.SetFraming(cameraSize, cameraOffset);
                battleCamera.SetHome(player.transform, true);
                battleCamera.ReturnHome();
            }

            player.Health.Died += info => Finish(false);
            StartCoroutine(PlayerLoop());

            switch (Kind)
            {
                case RealtimeKind.Waves:
                    StartCoroutine(WaveLoop());
                    break;
                case RealtimeKind.Escort:
                    if (protectee != null)
                        protectee.Health.Died += info => Finish(false);
                    foreach (var archer in archers)
                        StartCoroutine(ArcherLoop(archer));
                    StartCoroutine(EscortClock());
                    break;
            }
            Notify();
        }

        IEnumerator PlayerLoop()
        {
            while (!IsOver)
            {
                Arrow shot = null;
                aim.BeginTurn(player, null, arrow => shot = arrow);
                while (shot == null && !IsOver)
                    yield return null;
                if (IsOver)
                    yield break;

                ArrowsUsed++;
                flying.Add(shot);
                Notify();

                if (Kind == RealtimeKind.Trial && ArrowsLeft == 0)
                {
                    // Out of arrows: wait for them to land, then judge.
                    while (flying.Any(a => a != null && a.IsFlying))
                        yield return null;
                    if (!IsOver)
                        Finish(TargetsLeft == 0);
                    yield break;
                }

                yield return new WaitForSeconds(reloadTime);
            }
        }

        IEnumerator WaveLoop()
        {
            for (var w = 0; w < waves.Count; w++)
            {
                Wave = w + 1;
                Notify();
                var wave = waves[w];
                yield return new WaitForSeconds(wave.startDelay);
                for (var i = 0; i < wave.count && !IsOver; i++)
                {
                    var walker = spawnWalker(wave);
                    if (walker != null)
                    {
                        walkers.Add(walker);
                        walker.ReachedGoal += OnWalkerArrived;
                        var health = walker.GetComponent<Health>();
                        if (health != null)
                            health.Died += info => CheckWavesCleared();
                    }
                    yield return new WaitForSeconds(wave.interval);
                }
            }
            allSpawned = true;
            CheckWavesCleared();
        }

        void OnWalkerArrived(Walker walker)
        {
            if (IsOver)
                return;
            walkers.Remove(walker);
            Destroy(walker.gameObject);
            Lives--;
            Notify();
            if (Lives <= 0)
                Finish(false);
            else
                CheckWavesCleared();
        }

        void CheckWavesCleared()
        {
            if (IsOver || !allSpawned)
                return;
            var anyAlive = walkers.Any(w => w != null && !w.HasArrived && !w.GetComponent<Health>().IsDead);
            if (!anyAlive)
                Finish(true);
        }

        IEnumerator ArcherLoop(Combatant archer)
        {
            var ai = archer.GetComponent<EnemyArcherAI>();
            yield return new WaitForSeconds(Random.Range(1f, fireInterval));
            while (!IsOver && archer.IsAlive && ai != null)
            {
                // Mostly at the companion, sometimes at Arash.
                var target = protectee != null && protectee.IsAlive && Random.value < 0.75f ? protectee : player;
                ai.BeginTurn(archer, target, arrow => { });
                yield return new WaitForSeconds(fireInterval * Random.Range(0.8f, 1.25f));
            }
        }

        IEnumerator EscortClock()
        {
            while (!IsOver)
            {
                if (archers.All(a => a == null || !a.IsAlive))
                {
                    Finish(true);
                    yield break;
                }
                if (surviveSeconds > 0f)
                {
                    TimeLeft = Mathf.Max(0f, TimeLeft - Time.deltaTime);
                    if (TimeLeft <= 0f)
                    {
                        Finish(true);
                        yield break;
                    }
                }
                Notify();
                yield return null;
            }
        }

        void OnTargetHit()
        {
            Notify();
            if (TargetsLeft == 0)
                Finish(true);
        }

        void Finish(bool won)
        {
            if (IsOver)
                return;
            IsOver = true;
            if (aim != null)
                aim.CancelTurn();
            if (archers != null)
                foreach (var archer in archers)
                {
                    var ai = archer != null ? archer.GetComponent<EnemyArcherAI>() : null;
                    if (ai != null)
                        ai.CancelTurn();
                }
            foreach (var walker in walkers)
                if (walker != null)
                    walker.enabled = false;
            StartCoroutine(EndAfterDelay(won));
        }

        IEnumerator EndAfterDelay(bool won)
        {
            Notify();
            yield return new WaitForSeconds(endDelay);
            if (BattleEnded != null)
                BattleEnded(won);
        }

        void Notify()
        {
            if (StatusChanged != null)
                StatusChanged();
        }
    }
}
