using System;
using System.Collections;
using Arash.Art;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Arash.Combat
{
    /// <summary>
    /// Real-time behaviour of an enemy archer (F-51, F-52): waits a cooldown, then aims and shoots
    /// on its own, independently of everyone else. A red "!" warns the player while it draws; a
    /// hit during the draw spoils the shot and stuns it briefly. Tishtrya's rain slows it.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class EnemyBrain : MonoBehaviour
    {
        public float minCooldown = 2.8f;
        public float maxCooldown = 4.6f;
        public float firstDelay = 0.8f;
        public float stunTime = 0.9f;

        /// <summary>Who to shoot at next; null waits.</summary>
        public Func<Combatant> ChooseTarget;

        Combatant self;
        EnemyArcherAI ai;
        SpriteRenderer alert;
        Coroutine loop;
        float stunnedUntil;
        float slowUntil;
        float slowFactor = 1f;
        SpriteRenderer slowMark;

        public bool IsRunning { get { return loop != null; } }

        void Awake()
        {
            self = GetComponent<Combatant>();
            ai = GetComponent<EnemyArcherAI>();
        }

        void OnEnable()
        {
            if (ai != null)
            {
                ai.DrawStarted += OnDrawStarted;
                ai.TurnFinished += OnTurnFinished;
            }
            self.Health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (ai != null)
            {
                ai.DrawStarted -= OnDrawStarted;
                ai.TurnFinished -= OnTurnFinished;
            }
            if (self != null)
                self.Health.Damaged -= OnDamaged;
            ShowAlert(false);
        }

        public void Run()
        {
            if (loop == null && isActiveAndEnabled)
                loop = StartCoroutine(Loop());
        }

        public void Stop()
        {
            if (loop != null)
                StopCoroutine(loop);
            loop = null;
            if (ai != null)
                ai.CancelTurn();
            ShowAlert(false);
        }

        /// <summary>Tishtrya's rain: draws and cooldowns take <paramref name="factor"/> times longer for a while.</summary>
        public void Slow(float factor, float seconds)
        {
            slowFactor = Mathf.Max(slowFactor, factor);
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
            if (ai != null)
                ai.SetSlow(slowFactor);
            if (slowMark == null)
            {
                slowMark = ArtLibrary.Renderer(transform, "Rain", ArtLibrary.Fx("rain"), 60, new Vector2(0f, 2.7f));
                slowMark.transform.localScale = Vector3.one * 1.2f;
            }
            slowMark.enabled = true;
        }

        void Update()
        {
            if (slowFactor > 1f && Time.time >= slowUntil)
            {
                slowFactor = 1f;
                if (ai != null)
                    ai.SetSlow(1f);
                if (slowMark != null)
                    slowMark.enabled = false;
            }
            if (alert != null && alert.enabled)
                alert.transform.localScale = Vector3.one * (1.1f + 0.15f * Mathf.Sin(Time.time * 18f));
        }

        IEnumerator Loop()
        {
            yield return Wait(firstDelay + Random.Range(0f, 0.8f));
            while (self.IsAlive)
            {
                while (Time.time < stunnedUntil)
                    yield return null;

                var target = ChooseTarget != null ? ChooseTarget() : null;
                if (target == null || !target.IsAlive || ai == null)
                {
                    yield return null;
                    continue;
                }

                ai.BeginTurn(self, target, arrow => { });
                while (ai.IsBusy && self.IsAlive)
                    yield return null;
                yield return Wait(Random.Range(minCooldown, maxCooldown));
            }
            loop = null;
        }

        IEnumerator Wait(float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime / slowFactor)
                yield return null;
        }

        void OnDrawStarted(EnemyArcherAI archer)
        {
            ShowAlert(true);
        }

        void OnTurnFinished(EnemyArcherAI archer)
        {
            ShowAlert(false);
        }

        void OnDamaged(DamageInfo info, bool killed)
        {
            if (killed)
            {
                Stop();
                return;
            }
            if (ai != null && ai.IsDrawing)
            {
                ai.Interrupt();
                stunnedUntil = Time.time + stunTime;
            }
        }

        void ShowAlert(bool show)
        {
            if (alert == null)
            {
                if (!show)
                    return;
                var height = self.HeadTarget.position.y - transform.position.y + 0.65f * transform.localScale.y;
                alert = ArtLibrary.Renderer(transform, "Alert", ArtLibrary.Prop("alert"), 70, new Vector2(0f, height / Mathf.Max(0.01f, transform.localScale.y)));
            }
            alert.enabled = show && self.IsAlive;
        }
    }
}
