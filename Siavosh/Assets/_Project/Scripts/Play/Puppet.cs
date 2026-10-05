using Siavosh.Core;
using UnityEngine;

namespace Siavosh.Play
{
    /// <summary>
    /// A paper-cutout figure built from its parts (body, front arm, two boots), like the shadow
    /// puppets of a storyteller's play. The arm swings about the shoulder; the boots step; the body
    /// bobs. Controllers set the public fields every frame and the puppet poses itself.
    /// </summary>
    public class HumanPuppet : MonoBehaviour
    {
        public bool FacingRight = true;
        public float Speed;          // ground speed for the walk cycle
        public bool Airborne;
        public float ArmAngle;       // degrees, positive raises the arm forward-up
        public float Lean;           // degrees, positive leans forward
        public float Crouch;         // 0..1
        public float Squash;         // landing squash 0..1
        public float Fallen;         // 0..1, lying down

        Transform pose;
        Transform shoulder;
        SpriteRenderer body, arm, bootB, bootF;
        string rig;
        float phase;
        float flash;
        Color flashColor;
        Vector3 bootBRest, bootFRest;

        public static HumanPuppet Create(Transform parent, string rig, int sortingOrder, string arm = "arm")
        {
            var go = new GameObject("Puppet " + rig);
            go.transform.SetParent(parent, false);
            var puppet = go.AddComponent<HumanPuppet>();
            puppet.Build(rig, sortingOrder, arm);
            return puppet;
        }

        void Build(string rigName, int order, string armVariant)
        {
            rig = rigName;
            pose = new GameObject("Pose").transform;
            pose.SetParent(transform, false);
            bootB = Part(pose, "bootB", order);
            bootF = Part(pose, "bootF", order + 1);
            body = Part(pose, "body", order + 2);
            var anchor = Art.Anchor(rig, "shoulder");
            shoulder = new GameObject("Shoulder").transform;
            shoulder.SetParent(pose, false);
            shoulder.localPosition = anchor;
            arm = Part(shoulder, armVariant, order + 3);
            arm.transform.localPosition -= (Vector3)anchor;
            bootBRest = bootB.transform.localPosition;
            bootFRest = bootF.transform.localPosition;
        }

        SpriteRenderer Part(Transform parent, string part, int order)
        {
            var name = "parts/" + rig + "_" + part;
            var go = new GameObject(part);
            go.transform.SetParent(parent, false);
            var info = Art.Info(name);
            if (info != null)
                go.transform.localPosition = new Vector3(info.x, info.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(name);
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Swaps the front arm (e.g. sword arm for bow arm).</summary>
        public void SetArm(string variant)
        {
            var name = "parts/" + rig + "_" + variant;
            var info = Art.Info(name);
            arm.sprite = Art.Get(name);
            if (info != null)
                arm.transform.localPosition = new Vector3(info.x, info.y, 0f) - shoulder.localPosition;
        }

        public void Flash(Color color, float seconds = 0.12f)
        {
            flash = seconds;
            flashColor = color;
        }

        public void SetAlpha(float alpha)
        {
            foreach (var sr in new[] { body, arm, bootB, bootF })
            {
                var c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }

        public void SetSorting(int order)
        {
            bootB.sortingOrder = order;
            bootF.sortingOrder = order + 1;
            body.sortingOrder = order + 2;
            arm.sortingOrder = order + 3;
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            transform.localScale = new Vector3(FacingRight ? 1f : -1f, 1f, 1f);

            var moving = Mathf.Abs(Speed) > 0.2f && !Airborne;
            if (moving)
                phase += dt * Mathf.Clamp(Mathf.Abs(Speed), 2f, 9f) * 2.1f;
            else
                phase = Mathf.MoveTowards(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, dt * 6f);

            var stride = moving ? 0.17f : 0f;
            var lift = moving ? 0.07f : 0f;
            var s = Mathf.Sin(phase);
            var c = Mathf.Cos(phase);
            if (Airborne)
            {
                bootF.transform.localPosition = bootFRest + new Vector3(0.12f, 0.12f, 0f);
                bootB.transform.localPosition = bootBRest + new Vector3(-0.1f, 0.18f, 0f);
            }
            else
            {
                bootF.transform.localPosition = bootFRest + new Vector3(s * stride, Mathf.Max(0f, c) * lift, 0f);
                bootB.transform.localPosition = bootBRest + new Vector3(-s * stride, Mathf.Max(0f, -c) * lift, 0f);
            }

            var bob = moving ? Mathf.Abs(c) * 0.05f : Mathf.Sin(Time.time * 2f) * 0.012f;
            var squashY = 1f - Squash * 0.12f - Crouch * 0.18f;
            pose.localScale = new Vector3(1f + Squash * 0.06f, squashY, 1f);
            pose.localPosition = new Vector3(0f, bob, 0f);
            pose.localRotation = Quaternion.Euler(0f, 0f, -Lean - Fallen * 80f);
            shoulder.localRotation = Quaternion.Euler(0f, 0f, ArmAngle + (moving ? -s * 6f : 0f));

            if (flash > 0f)
            {
                flash -= dt;
                var tint = flash > 0f ? flashColor : Color.white;
                foreach (var sr in new[] { body, arm, bootB, bootF })
                {
                    var a = sr.color.a;
                    tint.a = a;
                    sr.color = tint;
                }
            }
        }
    }

    /// <summary>A galloping horse (Shabrang or an onager) with swinging legs and an optional rider.</summary>
    public class HorsePuppet : MonoBehaviour
    {
        public float Speed = 9f;
        public bool Airborne;
        public float Duck;           // rider ducks 0..1
        public float Stumble;        // 0..1
        public float Fallen;         // 0..1, lying on its side
        public bool FacingRight = true;

        Transform pose;
        Transform riderPivot;
        readonly Transform[] legs = new Transform[4];
        readonly float[] legPhase = { 0f, 0.6f, 2.4f, 3.0f };
        SpriteRenderer[] renderers;
        float phase;
        float flash;

        static readonly string[] LegNames = { "legFF", "legHF", "legFN", "legHN" };

        public static HorsePuppet Create(Transform parent, string rig, int sortingOrder, string rider = null, float scale = 1f)
        {
            var go = new GameObject("Horse " + rig);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            var puppet = go.AddComponent<HorsePuppet>();
            puppet.Build(rig, sortingOrder, rider);
            return puppet;
        }

        void Build(string rig, int order, string rider)
        {
            pose = new GameObject("Pose").transform;
            pose.SetParent(transform, false);
            var list = new System.Collections.Generic.List<SpriteRenderer>();
            for (var i = 0; i < 4; i++)
            {
                var anchor = Art.Anchor(rig, LegNames[i]);
                legs[i] = new GameObject(LegNames[i] + " pivot").transform;
                legs[i].SetParent(pose, false);
                legs[i].localPosition = anchor;
                var leg = Part(legs[i], rig, LegNames[i], i < 2 ? order : order + 2);
                leg.transform.localPosition -= (Vector3)anchor;
                list.Add(leg);
            }
            list.Add(Part(pose, rig, "body", order + 1));
            if (rider != null)
            {
                var anchor = Art.Anchor(rig, "rider");
                riderPivot = new GameObject("Rider pivot").transform;
                riderPivot.SetParent(pose, false);
                riderPivot.localPosition = anchor;
                var r = Part(riderPivot, rig, rider, order + 3);
                r.transform.localPosition -= (Vector3)anchor;
                list.Add(r);
            }
            renderers = list.ToArray();
        }

        static SpriteRenderer Part(Transform parent, string rig, string part, int order)
        {
            var name = "parts/" + rig + "_" + part;
            var go = new GameObject(part);
            go.transform.SetParent(parent, false);
            var info = Art.Info(name);
            if (info != null)
                go.transform.localPosition = new Vector3(info.x, info.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(name);
            sr.sortingOrder = order;
            return sr;
        }

        public void Flash(float seconds = 0.15f)
        {
            flash = seconds;
        }

        public void SetAlpha(float alpha)
        {
            foreach (var sr in renderers)
            {
                var c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            var sx = Mathf.Abs(transform.localScale.y);
            transform.localScale = new Vector3(FacingRight ? sx : -sx, sx, 1f);
            if (Fallen <= 0f)
                phase += dt * Mathf.Max(Speed, 0f) * 1.6f;

            for (var i = 0; i < 4; i++)
            {
                float angle;
                var front = i == 0 || i == 2;
                if (Airborne)
                    angle = front ? 10f : -10f; // the leap: legs stretched out as in the paintings
                else if (Speed > 0.5f)
                    angle = Mathf.Sin(phase + legPhase[i]) * 26f + (front ? -16f : 16f);
                else
                    angle = front ? -55f : 50f; // standing: legs under the body
                legs[i].localRotation = Quaternion.Euler(0f, 0f, angle * (1f - Fallen));
            }

            var bob = Airborne ? 0f : Mathf.Abs(Mathf.Sin(phase)) * 0.08f;
            pose.localPosition = new Vector3(0f, bob - Fallen * 0.5f, 0f);
            pose.localRotation = Quaternion.Euler(0f, 0f, Airborne ? 6f : Stumble * -12f + Fallen * 70f);
            if (riderPivot != null)
                riderPivot.localRotation = Quaternion.Euler(0f, 0f, -Duck * 28f);

            if (flash > 0f)
            {
                flash -= dt;
                var on = flash > 0f && Mathf.Repeat(flash * 20f, 1f) > 0.5f;
                foreach (var sr in renderers)
                {
                    var c = on ? new Color(1f, 0.55f, 0.45f, sr.color.a) : new Color(1f, 1f, 1f, sr.color.a);
                    sr.color = c;
                }
            }
        }
    }
}
