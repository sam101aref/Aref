using System;
using System.Collections.Generic;
using System.Linq;
using Arash.Core;
using UnityEngine;

namespace Arash.Combat
{
    /// <summary>
    /// The player's farr meter (F-31) and special arrows (F-32). Good shots fill the meter; when it
    /// is full the player can arm the selected special arrow, which replaces the next shot:
    /// fire (burns), triple (three at once), piercing (through shields and bodies), Simurgh (seeks
    /// the nearest enemy) or haoma (heals Arash as it flies).
    /// </summary>
    public class PlayerArsenal : MonoBehaviour
    {
        const float TripleSpread = 4f;
        const int PierceBodies = 3;
        const float SimurghTurnRate = 120f;
        const float HaomaHeal = 0.35f;

        Bow bow;
        AimController aim;
        Health health;
        Arrow fireArrowPrefab;
        Func<IEnumerable<Combatant>> enemies;
        List<SpecialArrow> specials = new List<SpecialArrow>();
        int selected;

        public FarrMeter Farr { get; private set; }
        public bool Armed { get; private set; }
        public bool HasSpecials { get { return specials.Count > 0; } }
        public SpecialArrow Selected { get { return HasSpecials ? specials[selected] : SpecialArrow.None; } }

        public event Action Changed;

        public void Configure(Bow playerBow, AimController playerAim, Health playerHealth, Arrow fireArrow,
            Func<IEnumerable<Combatant>> enemyList, Loadout loadout)
        {
            bow = playerBow;
            aim = playerAim;
            health = playerHealth;
            fireArrowPrefab = fireArrow;
            enemies = enemyList;
            specials = loadout.Specials ?? new List<SpecialArrow>();
            Farr = new FarrMeter { GainMultiplier = loadout.FarrMultiplier };
            Farr.Changed += Notify;
            aim.FireOverride = Fire;
            if (bow != null)
                bow.Fired += OnFired;
        }

        void OnEnable()
        {
            Health.AnyDamaged += OnAnyDamaged;
        }

        void OnDisable()
        {
            Health.AnyDamaged -= OnAnyDamaged;
        }

        void OnDestroy()
        {
            if (bow != null)
                bow.Fired -= OnFired;
        }

        /// <summary>Arms the selected special arrow for the next shot (or disarms it).</summary>
        public void ToggleArmed()
        {
            if (!Farr.IsFull || !HasSpecials)
                return;
            Armed = !Armed;
            Notify();
        }

        public void CycleSelection()
        {
            if (specials.Count < 2)
                return;
            selected = (selected + 1) % specials.Count;
            Notify();
        }

        Arrow Fire(AimState shot)
        {
            if (!Armed || !Farr.TrySpend())
                return bow.Fire(shot);
            Armed = false;
            Notify();

            switch (Selected)
            {
                case SpecialArrow.Fire:
                    return fireArrowPrefab != null ? bow.FireWith(shot, fireArrowPrefab) : bow.Fire(shot);
                case SpecialArrow.Triple:
                    bow.Fire(new AimState(true, shot.Angle + TripleSpread, shot.Power, shot.Facing));
                    bow.Fire(new AimState(true, shot.Angle - TripleSpread, shot.Power, shot.Facing));
                    return bow.Fire(shot);
                case SpecialArrow.Piercing:
                    var piercing = bow.Fire(shot);
                    if (piercing != null)
                        piercing.MakePiercing(PierceBodies);
                    return piercing;
                case SpecialArrow.Simurgh:
                    var simurgh = bow.Fire(shot);
                    if (simurgh != null)
                        simurgh.MakeHoming(() => NearestEnemy(simurgh.transform.position), SimurghTurnRate);
                    return simurgh;
                case SpecialArrow.Haoma:
                    health.Heal(health.Max * HaomaHeal);
                    return bow.Fire(shot);
                default:
                    return bow.Fire(shot);
            }
        }

        Transform NearestEnemy(Vector2 from)
        {
            var target = enemies().Where(e => e != null && e.IsAlive)
                .OrderBy(e => ((Vector2)e.BodyTarget.position - from).sqrMagnitude)
                .FirstOrDefault();
            return target != null ? target.BodyTarget : null;
        }

        void OnAnyDamaged(Health target, DamageInfo info, bool killed)
        {
            if (Farr == null || target == health || info.Source != transform)
                return;
            Farr.Add(FarrRules.Gain(info.Zone, killed));
        }

        void OnFired(Arrow arrow)
        {
            arrow.Intercepted += a => Farr.Add(FarrRules.Intercept);
        }

        bool wasFull;

        void Notify()
        {
            if (Farr.IsFull && !wasFull)
                Audio.AudioService.Play(Audio.Sfx.FarrReady);
            wasFull = Farr.IsFull;
            if (Changed != null)
                Changed();
        }
    }
}
