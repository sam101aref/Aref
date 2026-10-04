using System.Collections.Generic;
using Arash.Core;
using Arash.Levels;
using Arash.Localization;
using Arash.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Arash.Flight
{
    /// <summary>
    /// The final level (F-30): draws <see cref="FlightRules"/> with placeholder shapes. The sky turns
    /// from dawn to noon, mountains give way to plains, and the Oxus appears at the end.
    /// </summary>
    public class ArrowFlightController : MonoBehaviour
    {
        [SerializeField] Camera sceneCamera;
        [SerializeField] BattleHud hud;
        [SerializeField] Sprite square;
        [SerializeField] Sprite circle;
        [SerializeField] Material spriteMaterial;
        [SerializeField, Tooltip("Arrow position from the left of the view, in world units.")]
        float arrowScreenX = -6f;

        static readonly Color Dawn = new Color(0.98f, 0.6f, 0.45f);
        static readonly Color Morning = new Color(0.98f, 0.82f, 0.55f);
        static readonly Color Noon = new Color(0.45f, 0.7f, 0.95f);
        static readonly Color Rock = new Color(0.42f, 0.38f, 0.36f);
        static readonly Color Plains = new Color(0.62f, 0.6f, 0.3f);

        LevelDefinition level;
        FlightSettings settings;
        FlightState state;
        List<FlightObstacle> course;
        readonly Dictionary<int, Transform> visuals = new Dictionary<int, Transform>();
        readonly HashSet<int> consumed = new HashSet<int>();
        Transform arrow;
        SpriteRenderer ground;
        bool started;
        bool ended;

        void Awake()
        {
            var catalog = LevelCatalog.Load();
            level = SceneFlow.CurrentLevel;
            if (level == null && catalog != null)
                level = catalog.AllLevels().Find(l => l.mode == LevelMode.Flight);
            settings = level != null ? level.flight : new FlightSettings();
            course = FlightCourse.Generate(settings.seed, settings.length, settings.obstacleDensity);
            state = new FlightState { Y = 2f };

            arrow = DrawArrow();
            ground = Shape(null, square, Vector2.zero, new Vector2(80f, 4f), Rock, 2).GetComponent<SpriteRenderer>();
            // The Oxus waits at the end of the course.
            Shape(null, square, new Vector2(settings.length + 20f, FlightRules.MinY - 0.6f), new Vector2(50f, 1.4f),
                new Color(0.25f, 0.5f, 0.75f), 3);
        }

        void Start()
        {
            if (hud == null)
                return;
            hud.SetLevel(level != null ? level.titleKey : null, "flight.hint");
            hud.SetStatus(() => Loc.T("flight.status", Mathf.CeilToInt(Mathf.Max(0f, state.Energy)),
                Mathf.FloorToInt(100f * Mathf.Clamp01(state.X / settings.length))));
            hud.PlayDialogue(level != null ? level.introDialogue : null, () =>
            {
                if (level != null)
                    hud.ShowIntro(level.introKey);
                started = true;
            });
        }

        void Update()
        {
            if (!started || ended || GamePause.IsPaused)
                return;

            var holding = IsHolding();
            if (holding && hud != null)
                hud.HideHint();
            FlightRules.Step(state, holding, Time.deltaTime, settings.speed, settings.length);
            CheckCollisions();
            UpdateVisuals();
            if (hud != null)
                hud.RefreshStatus();

            if (state.Finished || state.Failed)
                End(state.Finished);
        }

        static bool IsHolding()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed)
                return false;
            var events = EventSystem.current;
            return events == null || !events.IsPointerOverGameObject();
        }

        void CheckCollisions()
        {
            for (var i = 0; i < course.Count; i++)
            {
                var obstacle = course[i];
                if (consumed.Contains(i) || Mathf.Abs(obstacle.X - state.X) > 8f)
                    continue;
                obstacle.Y = CurrentY(obstacle, i);
                if (!FlightRules.Overlaps(obstacle, state.X, state.Y))
                    continue;

                var hadInvulnerability = state.Invulnerable > 0f;
                if (FlightRules.Touch(state, obstacle))
                {
                    consumed.Add(i);
                    Transform visual;
                    if (visuals.TryGetValue(i, out visual))
                        Destroy(visual.gameObject);
                    visuals.Remove(i);
                }
                else if (!hadInvulnerability && hud != null)
                {
                    hud.ShowPopup(Loc.T("flight.hit"));
                }
            }
        }

        /// <summary>Eagles glide up and down; everything else stays put.</summary>
        float CurrentY(FlightObstacle obstacle, int index)
        {
            return obstacle.Kind == FlightObstacleKind.Eagle
                ? obstacle.Y + Mathf.Sin(Time.time * 1.6f + index) * 1.2f
                : obstacle.Y;
        }

        void UpdateVisuals()
        {
            var progress = Mathf.Clamp01(state.X / settings.length);
            var cameraX = state.X - arrowScreenX;
            sceneCamera.transform.position = new Vector3(cameraX, 1f, -10f);
            sceneCamera.backgroundColor = progress < 0.5f
                ? Color.Lerp(Dawn, Morning, progress * 2f)
                : Color.Lerp(Morning, Noon, (progress - 0.5f) * 2f);

            ground.transform.position = new Vector3(cameraX, FlightRules.MinY - 2f, 0f);
            ground.color = Color.Lerp(Rock, Plains, Mathf.Clamp01(progress * 1.6f));

            arrow.position = new Vector3(state.X, state.Y, 0f);
            arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(state.VerticalSpeed, settings.speed) * Mathf.Rad2Deg);
            var blink = state.Invulnerable > 0f && Mathf.Repeat(Time.time * 10f, 1f) < 0.5f;
            arrow.gameObject.SetActive(!blink);

            // Draw obstacles that are near the view, drop the ones behind it.
            for (var i = 0; i < course.Count; i++)
            {
                var obstacle = course[i];
                var near = obstacle.X > cameraX - 20f && obstacle.X < cameraX + 22f;
                Transform visual;
                var exists = visuals.TryGetValue(i, out visual);
                if (near && !exists && !consumed.Contains(i))
                    visuals[i] = visual = DrawObstacle(obstacle);
                else if (!near && exists)
                {
                    Destroy(visual.gameObject);
                    visuals.Remove(i);
                    continue;
                }
                if (visual != null && obstacle.Kind == FlightObstacleKind.Eagle)
                    visual.position = new Vector3(obstacle.X, CurrentY(obstacle, i), 0f);
            }
        }

        void End(bool won)
        {
            ended = true;
            var stars = level != null ? ProgressRules.Stars(won, state.Energy / FlightRules.MaxEnergy, state.Hits, level.stars) : (won ? 1 : 0);
            var coins = won && level != null ? stars * level.coinsPerStar : 0;
            if (won && level != null)
            {
                SaveSystem.Data.RecordWin(level.id, stars);
                SaveSystem.Data.coins += coins;
                SaveSystem.Save();
            }

            var catalog = LevelCatalog.Load();
            var ending = catalog != null ? catalog.endingCutscene : null;
            System.Action toMap = won ? SceneFlow.WithCutscene(ending, SceneFlow.ToMainMenu) : SceneFlow.ToWorldMap;
            if (hud == null)
                return;
            hud.SetStatus(null);
            hud.PlayDialogue(won && level != null ? level.outroDialogue : null, () => hud.ShowResult(won, stars, coins, null, toMap));
        }

        Transform DrawObstacle(FlightObstacle obstacle)
        {
            switch (obstacle.Kind)
            {
                case FlightObstacleKind.Cloud:
                    var cloud = Shape(null, circle, new Vector2(obstacle.X, obstacle.Y), Vector2.one * obstacle.Size * 2f, new Color(0.35f, 0.35f, 0.42f, 0.9f), 6);
                    Shape(cloud, circle, new Vector2(0.35f, 0.15f), Vector2.one * 0.7f, new Color(0.3f, 0.3f, 0.36f, 0.9f), 6);
                    return cloud;
                case FlightObstacleKind.Peak:
                    var side = obstacle.Size * 1.414f;
                    var peak = Shape(null, square, new Vector2(obstacle.X, FlightRules.MinY), new Vector2(side, side), Rock * 0.85f, 4);
                    peak.rotation = Quaternion.Euler(0f, 0f, 45f);
                    return peak;
                case FlightObstacleKind.Eagle:
                    var eagle = Shape(null, square, new Vector2(obstacle.X, obstacle.Y), new Vector2(1.4f, 0.25f), new Color(0.25f, 0.18f, 0.12f), 7);
                    Shape(eagle, circle, new Vector2(-0.5f, 0.05f), new Vector2(0.25f, 0.25f), new Color(0.9f, 0.85f, 0.75f), 8);
                    return eagle;
                default:
                    var farr = Shape(null, circle, new Vector2(obstacle.X, obstacle.Y), Vector2.one * obstacle.Size * 2f, new Color(1f, 0.85f, 0.35f), 7);
                    Shape(farr, circle, Vector2.zero, Vector2.one * 1.6f, new Color(1f, 0.9f, 0.5f, 0.35f), 6);
                    return farr;
            }
        }

        Transform DrawArrow()
        {
            var root = new GameObject("Great Arrow").transform;
            Shape(root, square, new Vector2(-0.9f, 0f), new Vector2(1.8f, 0.1f), new Color(0.9f, 0.72f, 0.32f), 20);
            Shape(root, square, new Vector2(-1.75f, 0f), new Vector2(0.35f, 0.26f), new Color(0.72f, 0.16f, 0.12f), 21);
            var head = Shape(root, square, Vector2.zero, new Vector2(0.26f, 0.26f), new Color(0.85f, 0.85f, 0.9f), 21);
            head.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Shape(root, circle, new Vector2(-0.6f, 0f), new Vector2(2.6f, 0.9f), new Color(1f, 0.85f, 0.4f, 0.25f), 19);
            return root;
        }

        Transform Shape(Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject("Shape");
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            if (spriteMaterial != null)
                renderer.sharedMaterial = spriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (sprite == square)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = size;
            }
            else
            {
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
            return go.transform;
        }
    }
}
