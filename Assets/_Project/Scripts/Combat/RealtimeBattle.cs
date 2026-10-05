using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arash.Combat
{
    public enum RealtimeKind
    {
        /// <summary>Waves of enemies; both sides shoot at once (F-51).</summary>
        Battle,
        /// <summary>A battle in which a companion behind Arash must also survive (F-24).</summary>
        Escort,
        /// <summary>Hit every target with limited arrows without hurting anyone (F-25).</summary>
        Trial,
    }

    public enum DefeatReason
    {
        None,
        Fallen,
        CompanionFallen,
        OutOfArrows,
        Bystander,
    }

    /// <summary>
    /// Runs a real-time battle (F-51). The player shoots whenever the bow is ready while the enemies
    /// of the current wave act on their own. A wave ends when all of its enemies are down; supplies
    /// then refill part of each quiver and the next wave comes after an optional story moment. The
    /// battle is lost if Arash (or the companion) falls, or if every arrow is spent.
    /// </summary>
    public class RealtimeBattle : MonoBehaviour, IBattleMode
    {
        [SerializeField] BattleCamera battleCamera;
        [SerializeField] float endDelay = 1.4f;
        [SerializeField, Tooltip("Seconds between waves, after the last enemy of a wave falls.")]
        float waveGap = 1.2f;

        public event Action<bool> BattleEnded;
        /// <summary>Raised when the wave, time, targets or arrows change (for the HUD).</summary>
        public event Action StatusChanged;
        /// <summary>Raised when supplies arrive after a wave, with the number of arrows added.</summary>
        public event Action<int> Resupplied;

        public RealtimeKind Kind { get; private set; }
        public bool IsOver { get; private set; }
        public bool IsRunning { get { return started && !IsOver; } }
        public int ArrowsUsed { get; private set; }
        public DefeatReason Defeat { get; private set; }

        public int Wave { get; private set; }
        public int WaveCount { get; private set; }
        public int TargetsLeft { get { return targets != null ? targets.Count(t => t != null && !t.IsHit) : 0; } }
        public int ArrowsLeft { get { return Mathf.Max(0, arrowLimit - ArrowsUsed); } }

        public float PlayerCondition
        {
            get
            {
                switch (Kind)
                {
                    case RealtimeKind.Trial:
                        return arrowLimit > 0 ? ArrowsLeft / (float)arrowLimit : 0f;
                    case RealtimeKind.Escort:
                        return Mathf.Min(player.Health.Fraction, protectee != null ? protectee.Health.Fraction : 0f);
                    default:
                        return player != null ? player.Health.Fraction : 0f;
                }
            }
        }

        /// <summary>Living enemies of the current wave.</summary>
        public IEnumerable<Combatant> LivingEnemies { get { return enemies.Where(e => e != null && e.IsAlive); } }

        Combatant player;
        AimController aim;
        PlayerArsenal arsenal;
        bool started;

        Func<int, List<Combatant>> spawnWave;
        Action<int, Action> beforeWave;
        readonly List<Combatant> enemies = new List<Combatant>();
        Combatant protectee;

        List<TrialTarget> targets;
        int arrowLimit;
        readonly List<Arrow> flying = new List<Arrow>();

        /// <summary>
        /// A battle of <paramref name="waves"/> waves. <paramref name="spawner"/> creates a wave's
        /// enemies; <paramref name="intro"/> may show a story moment before a wave and must call the
        /// continuation it is given. A <paramref name="companion"/> makes it an escort.
        /// </summary>
        public void ConfigureBattle(Combatant playerCombatant, AimController playerAim, PlayerArsenal playerArsenal,
            int waves, Func<int, List<Combatant>> spawner, Action<int, Action> intro, Combatant companion)
        {
            Kind = companion != null ? RealtimeKind.Escort : RealtimeKind.Battle;
            player = playerCombatant;
            aim = playerAim;
            arsenal = playerArsenal;
            WaveCount = Mathf.Max(1, waves);
            spawnWave = spawner;
            beforeWave = intro;
            protectee = companion;
        }

        public void ConfigureTrial(Combatant playerCombatant, AimController playerAim, List<TrialTarget> trialTargets,
            int arrows, IEnumerable<Health> bystanders)
        {
            Kind = RealtimeKind.Trial;
            player = playerCombatant;
            aim = playerAim;
            targets = trialTargets;
            arrowLimit = Mathf.Max(1, arrows);
            foreach (var target in targets)
                target.Hit += t => OnTargetHit();
            foreach (var bystander in bystanders)
                bystander.Damaged += (info, killed) => Finish(false, DefeatReason.Bystander);
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
                battleCamera.SetHome(player.transform, true);
                battleCamera.SetAutoFrame(FrameRect);
                battleCamera.ReturnHome();
            }

            player.Health.Died += info => Finish(false, DefeatReason.Fallen);
            if (protectee != null)
                protectee.Health.Died += info => Finish(false, DefeatReason.CompanionFallen);

            StartCoroutine(PlayerLoop());
            if (Kind != RealtimeKind.Trial)
                StartCoroutine(WaveLoop());
            Notify();
        }

        /// <summary>What the camera keeps in view: Arash, the companion and every living enemy.</summary>
        Rect? FrameRect()
        {
            if (player == null)
                return null;
            var min = (Vector2)player.transform.position;
            var max = min + Vector2.up * 2.4f;
            Action<Vector2> include = p =>
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            };
            if (protectee != null)
                include(protectee.transform.position);
            foreach (var enemy in LivingEnemies)
            {
                include(enemy.transform.position);
                include(enemy.HeadTarget.position + Vector3.up * 0.8f);
            }
            if (targets != null)
                foreach (var target in targets)
                    if (target != null)
                        include(target.transform.position + Vector3.up);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        IEnumerator PlayerLoop()
        {
            while (!IsOver)
            {
                Arrow shot = null;
                aim.BeginTurn(player, null, arrow => shot = arrow);
                while (shot == null && !IsOver)
                {
                    if (Kind != RealtimeKind.Trial && arsenal != null && arsenal.TotalAmmo == 0)
                        yield return OutOfArrows();
                    yield return null;
                }
                if (IsOver)
                    yield break;

                ArrowsUsed++;
                flying.Add(shot);
                flying.RemoveAll(a => a == null || !a.IsFlying);
                Notify();

                if (Kind == RealtimeKind.Trial && ArrowsLeft == 0)
                {
                    // Out of arrows: wait for them to land, then judge.
                    yield return WaitForArrows();
                    if (!IsOver)
                        Finish(TargetsLeft == 0, DefeatReason.OutOfArrows);
                    yield break;
                }

                yield return new WaitForSeconds(arsenal != null ? arsenal.ReloadTime : 0.6f);
            }
        }

        /// <summary>Every quiver is empty: the last arrows may still win the wave, or supplies may come.</summary>
        IEnumerator OutOfArrows()
        {
            yield return WaitForArrows();
            yield return new WaitForSeconds(0.6f);
            if (!IsOver && arsenal.TotalAmmo == 0 && LivingEnemies.Any())
                Finish(false, DefeatReason.OutOfArrows);
        }

        IEnumerator WaitForArrows()
        {
            while (flying.Any(a => a != null && a.IsFlying))
                yield return null;
        }

        IEnumerator WaveLoop()
        {
            for (var w = 0; w < WaveCount && !IsOver; w++)
            {
                Wave = w + 1;
                Notify();

                if (beforeWave != null)
                {
                    var ready = false;
                    beforeWave(w, () => ready = true);
                    while (!ready && !IsOver)
                        yield return null;
                }

                enemies.Clear();
                enemies.AddRange(spawnWave(w).Where(e => e != null));
                foreach (var enemy in enemies)
                    enemy.Health.Died += info => Notify();
                Notify();

                while (!IsOver && LivingEnemies.Any())
                    yield return null;
                if (IsOver)
                    yield break;

                if (w + 1 < WaveCount)
                {
                    yield return new WaitForSeconds(waveGap);
                    if (arsenal != null)
                    {
                        var added = arsenal.Refill();
                        if (added > 0 && Resupplied != null)
                            Resupplied(added);
                    }
                }
            }
            if (!IsOver)
                Finish(true, DefeatReason.None);
        }

        void OnTargetHit()
        {
            Notify();
            if (TargetsLeft == 0)
                Finish(true, DefeatReason.None);
        }

        void Finish(bool won, DefeatReason reason)
        {
            if (IsOver)
                return;
            IsOver = true;
            Defeat = won ? DefeatReason.None : reason;
            if (aim != null)
                aim.CancelTurn();
            foreach (var enemy in enemies.Where(e => e != null))
            {
                var brain = enemy.GetComponent<EnemyBrain>();
                if (brain != null)
                    brain.Stop();
                var raider = enemy.GetComponent<Raider>();
                if (raider != null)
                    raider.enabled = false;
                var shaman = enemy.GetComponent<ShamanCaster>();
                if (shaman != null)
                    shaman.enabled = false;
            }
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
