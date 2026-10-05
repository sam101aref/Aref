using System.Collections;
using System.Linq;
using Arash.Art;
using Arash.Core;
using Arash.Levels;
using Arash.Localization;
using Arash.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arash.Story
{
    /// <summary>Drifts an object slowly (clouds, walkers, the flying arrow).</summary>
    public class ParallaxDrift : MonoBehaviour
    {
        public Vector2 velocity;
        public bool bob;

        float t;
        Vector3 origin;

        void Start()
        {
            origin = transform.position;
        }

        void Update()
        {
            t += Time.deltaTime;
            var p = origin + (Vector3)(velocity * t);
            if (bob)
                p.y += Mathf.Abs(Mathf.Sin(t * 7f)) * 0.08f;
            transform.position = p;
        }
    }

    /// <summary>
    /// Plays a <see cref="CutsceneDefinition"/> (F-20, F-60) in the Cutscene scene. Each panel is an
    /// illustrated scene built from the game's art: the biome backdrop, characters in poses and
    /// props, with a slow camera move and a caption (with the speaker's portrait when someone
    /// speaks). Panels change with a short fade; tap for the next one, Skip to leave.
    /// </summary>
    public class CutscenePlayer : ScreenBase
    {
        const float FadeTime = 0.35f;

        [SerializeField] Sprite square;
        [SerializeField] Sprite circle;
        [SerializeField] Material spriteMaterial;
        [SerializeField] Camera sceneCamera;

        CutsceneDefinition cutscene;
        int index;
        float panelTime;
        Transform world;
        bool finished;
        bool switching;
        CanvasGroup fade;
        CanvasGroup captionGroup;

        protected override void Start()
        {
            ArtLibrary.SpriteMaterial = spriteMaterial;
            cutscene = SceneFlow.PendingCutscene;
            if (cutscene == null)
            {
                var catalog = LevelCatalog.Load();
                cutscene = catalog != null ? catalog.AllCutscenes().FirstOrDefault() : null;
            }
            if (cutscene == null || cutscene.panels.Count == 0)
            {
                Finish();
                return;
            }
            ShowPanel(0);
            base.Start();
            StartCoroutine(FadeFrom(1f));
        }

        void Update()
        {
            if (finished || cutscene == null || GamePause.IsPaused)
                return;
            panelTime += Time.deltaTime;
            var panel = cutscene.panels[index];
            MoveCamera(panel, Mathf.Clamp01(panelTime / panel.duration));
            if (captionGroup != null)
                captionGroup.alpha = Mathf.Clamp01((panelTime - 0.2f) / 0.5f);
            if (panelTime >= panel.duration)
                Next();
        }

        void Next()
        {
            if (switching || finished)
                return;
            if (index + 1 >= cutscene.panels.Count)
                StartCoroutine(FadeOutAndFinish());
            else
                StartCoroutine(SwitchTo(index + 1));
        }

        IEnumerator SwitchTo(int panelIndex)
        {
            switching = true;
            yield return FadeTo(1f);
            ShowPanel(panelIndex);
            switching = false;
            yield return FadeFrom(1f);
        }

        IEnumerator FadeOutAndFinish()
        {
            switching = true;
            yield return FadeTo(1f);
            Finish();
        }

        IEnumerator FadeTo(float target)
        {
            if (fade == null)
                yield break;
            var start = fade.alpha;
            for (var t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                if (fade == null)
                    yield break;
                fade.alpha = Mathf.Lerp(start, target, t / FadeTime);
                yield return null;
            }
            if (fade != null)
                fade.alpha = target;
        }

        IEnumerator FadeFrom(float from)
        {
            if (fade != null)
                fade.alpha = from;
            yield return FadeTo(0f);
        }

        void Finish()
        {
            if (finished)
                return;
            finished = true;
            SceneFlow.FinishCutscene();
        }

        void ShowPanel(int panelIndex)
        {
            index = panelIndex;
            panelTime = 0f;
            DrawWorld(cutscene.panels[index]);
            MoveCamera(cutscene.panels[index], 0f);
            if (Canvas != null)
                Rebuild();
        }

        void MoveCamera(CutscenePanel panel, float k)
        {
            if (sceneCamera == null)
                return;
            var t = Mathf.SmoothStep(0f, 1f, k);
            var p = Vector2.Lerp(panel.cameraFrom, panel.cameraTo, t);
            sceneCamera.transform.position = new Vector3(p.x, p.y, -10f);
            sceneCamera.orthographicSize = Mathf.Lerp(panel.zoomFrom, panel.zoomTo, t);
        }

        protected override void Build(RectTransform canvas)
        {
            if (cutscene == null)
                return;
            var panel = cutscene.panels[index];

            var catcher = UIFactory.Panel(canvas, "Tap", Color.clear);
            UIFactory.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(Next);

            if (index == 0)
            {
                var title = UIFactory.Label(canvas, Loc.T(cutscene.titleKey), 64, UIFactory.Gold, TextAnchor.MiddleCenter, true);
                UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1600f, 100f));
            }

            var band = UIFactory.Panel(canvas, "Caption", new Color(0f, 0f, 0f, 0.62f));
            band.raycastTarget = false;
            UIFactory.Place(band.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 250f));
            captionGroup = band.gameObject.AddComponent<CanvasGroup>();
            captionGroup.blocksRaycasts = false;
            captionGroup.alpha = 0f;

            var speaking = panel.speaker != Speaker.Narrator;
            var textX = 0f;
            if (speaking)
            {
                var portrait = UIFactory.Circle(band.transform, DialogueView.SpeakerColor(panel.speaker), 190f);
                UIFactory.Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(60f, 20f), new Vector2(190f, 190f));
                DialogueView.Portrait(portrait.rectTransform, panel.speaker, 1.3f);
                var name = UIFactory.Label(band.transform, Loc.T(DialogueView.SpeakerKey(panel.speaker)), 40, UIFactory.Gold, TextAnchor.MiddleLeft, true);
                UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(290f, -18f), new Vector2(1400f, 56f));
                textX = 290f;
            }
            var width = speaking ? 1520f : 1700f;
            var caption = UIFactory.WrappedLabel(band.transform, Loc.Get(panel.captionKey), 42, UIFactory.Cream, width,
                speaking ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter);
            if (speaking)
                UIFactory.Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(textX, -78f), new Vector2(width, 160f));
            else
                UIFactory.Place(caption.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 220f));

            var skip = UIFactory.Button(canvas, Loc.T("ui.skip"), Finish, new Color(0f, 0f, 0f, 0.45f), 36);
            UIFactory.Place((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(220f, 80f));

            var progress = UIFactory.Label(canvas, Loc.Number(index + 1) + " / " + Loc.Number(cutscene.panels.Count),
                30, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleLeft);
            UIFactory.Place(progress.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(300f, 60f));

            var black = UIFactory.Panel(canvas, "Fade", Color.black);
            UIFactory.Stretch(black.rectTransform);
            black.raycastTarget = false;
            var previous = fade != null ? fade.alpha : 1f;
            fade = black.gameObject.AddComponent<CanvasGroup>();
            fade.blocksRaycasts = false;
            fade.alpha = previous;
        }

        // ------------------------------------------------------------------ illustration

        void DrawWorld(CutscenePanel panel)
        {
            if (world != null)
                Destroy(world.gameObject);
            world = new GameObject("Cutscene World").transform;

            var backdrop = Backdrop.Build(panel.biome, panel.time, sceneCamera, 0f, 0f, 70f);
            backdrop.SetParent(world, true);

            foreach (var prop in panel.props)
            {
                var sprite = ArtLibrary.Prop(prop.sprite);
                if (sprite == null)
                    continue;
                var renderer = ArtLibrary.Renderer(world, prop.sprite, sprite, prop.front ? 55 : 15, prop.position);
                renderer.transform.localScale = Vector3.one * prop.scale;
                renderer.flipX = prop.flip;
                if (panel.time == TimeOfDay.Night)
                    renderer.color = new Color(0.62f, 0.66f, 0.82f);
                else if (panel.time == TimeOfDay.Dusk)
                    renderer.color = new Color(1f, 0.86f, 0.78f);
                if (prop.drift != Vector2.zero)
                    renderer.gameObject.AddComponent<ParallaxDrift>().velocity = prop.drift;
            }

            foreach (var actor in panel.actors)
                DrawActor(actor, panel.time);
        }

        void DrawActor(CutsceneActor actor, TimeOfDay time)
        {
            var root = new GameObject(actor.look.ToString()).transform;
            root.SetParent(world, false);
            root.position = actor.position;
            root.localScale = Vector3.one * actor.scale;
            var skin = CharacterSkin.Build(root, actor.facingRight, actor.look);
            if (actor.look == CharacterLook.Arash)
            {
                var outfit = Armory.Find(SaveSystem.Data.equippedOutfit) ?? Armory.Find(Armory.DefaultOutfit);
                skin.SetColors(outfit.Tunic, outfit.Cape);
                skin.SetHat(null, outfit.Cap);
            }
            if (time == TimeOfDay.Night)
                skin.Tint(new Color(0.7f, 0.74f, 0.88f));
            else if (time == TimeOfDay.Dusk)
                skin.Tint(new Color(1f, 0.9f, 0.84f));

            var pivot = root.Find("AimPivot");
            var angle = -65f;
            switch (actor.pose)
            {
                case ActorPose.Aim: angle = 12f; break;
                case ActorPose.Raise: angle = 72f; break;
            }
            if (pivot != null)
                pivot.rotation = Quaternion.Euler(0f, 0f, actor.facingRight ? angle : 180f - angle);

            if (actor.pose == ActorPose.Kneel)
            {
                foreach (var name in new[] { "Torso", "Head", "AimPivot" })
                {
                    var part = root.Find(name);
                    if (part != null)
                        part.localPosition += Vector3.down * 0.38f;
                }
                var legs = root.Find("Legs");
                if (legs != null)
                {
                    legs.localScale = new Vector3(1f, 0.55f, 1f);
                    legs.localPosition = new Vector3(0f, 0.22f, 0f);
                }
            }
            else if (actor.pose == ActorPose.Fallen)
            {
                root.rotation = Quaternion.Euler(0f, 0f, actor.facingRight ? 88f : -88f);
                root.position = (Vector3)actor.position + Vector3.up * 0.3f;
            }

            if (actor.drift != Vector2.zero || actor.pose == ActorPose.Walk)
            {
                var drift = root.gameObject.AddComponent<ParallaxDrift>();
                drift.velocity = actor.drift;
                drift.bob = actor.pose == ActorPose.Walk;
            }
        }
    }
}
