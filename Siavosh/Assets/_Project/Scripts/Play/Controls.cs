using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Siavosh.Play
{
    public enum Act
    {
        Jump,
        Attack,
        Shield,
        Dodge,
        Bow,
        Farr,
        Interact,
        Duck,
    }

    /// <summary>An on-screen button that reports presses and releases from touch.</summary>
    public class PadButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Act Act;
        public bool Held;
        public int Downs;
        public int Ups;

        public void OnPointerDown(PointerEventData eventData)
        {
            Held = true;
            Downs++;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Held = false;
            Ups++;
        }

        void OnDisable()
        {
            if (Held)
                Ups++;
            Held = false;
        }
    }

    /// <summary>The on-screen stick: drag anywhere in its area; the knob follows the finger.</summary>
    public class PadStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public float Radius = 90f;
        public Vector2 Value;
        Vector2 origin = Vector2.zero;

        public void OnPointerDown(PointerEventData eventData)
        {
            origin = Vector2.zero; // the stick's centre
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var offset = Vector2.ClampMagnitude(Local(eventData.position) - origin, Radius);
            Value = offset / Radius;
            if (Knob != null)
                Knob.anchoredPosition = offset;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            if (Knob != null)
                Knob.anchoredPosition = Vector2.zero;
        }

        void OnDisable()
        {
            Value = Vector2.zero;
        }

        Vector2 Local(Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out local);
            return local;
        }
    }

    /// <summary>
    /// The player's input for this frame, merged from the touch controls and the keyboard (for the
    /// editor). <see cref="Poll"/> is called once per frame before the world updates; afterwards
    /// Pressed/Released say whether a button went down or up during the frame.
    /// </summary>
    public static class Controls
    {
        static PadStick stick;
        static readonly Dictionary<Act, PadButton> buttons = new Dictionary<Act, PadButton>();
        static readonly Dictionary<Act, bool> held = new Dictionary<Act, bool>();
        static readonly Dictionary<Act, bool> pressed = new Dictionary<Act, bool>();
        static readonly Dictionary<Act, bool> released = new Dictionary<Act, bool>();

        public static float MoveX { get; private set; }
        public static float AimY { get; private set; }

        public static void Register(PadStick padStick)
        {
            stick = padStick;
        }

        public static void Register(PadButton button)
        {
            buttons[button.Act] = button;
        }

        public static void Clear()
        {
            stick = null;
            buttons.Clear();
            held.Clear();
            pressed.Clear();
            released.Clear();
            MoveX = 0f;
            AimY = 0f;
        }

        public static void Poll()
        {
            var keyX = 0f;
            var keyY = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyX += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyY += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyY -= 1f;

            var s = stick != null ? stick.Value : Vector2.zero;
            MoveX = Mathf.Clamp(Mathf.Abs(s.x) > 0.25f ? s.x : keyX, -1f, 1f);
            AimY = Mathf.Clamp(Mathf.Abs(s.y) > 0.2f ? s.y : keyY, -1f, 1f);

            foreach (Act act in System.Enum.GetValues(typeof(Act)))
            {
                var key = Key(act);
                PadButton b;
                buttons.TryGetValue(act, out b);
                var touchHeld = b != null && b.Held;
                var touchDown = b != null && b.Downs > 0;
                var touchUp = b != null && b.Ups > 0;
                if (b != null)
                {
                    b.Downs = 0;
                    b.Ups = 0;
                }
                held[act] = touchHeld || Input.GetKey(key);
                pressed[act] = touchDown || Input.GetKeyDown(key);
                released[act] = touchUp || Input.GetKeyUp(key);
            }
        }

        public static bool Held(Act act)
        {
            bool v;
            return held.TryGetValue(act, out v) && v;
        }

        public static bool Pressed(Act act)
        {
            bool v;
            return pressed.TryGetValue(act, out v) && v;
        }

        public static bool Released(Act act)
        {
            bool v;
            return released.TryGetValue(act, out v) && v;
        }

        static KeyCode Key(Act act)
        {
            switch (act)
            {
                case Act.Jump: return KeyCode.Space;
                case Act.Attack: return KeyCode.J;
                case Act.Shield: return KeyCode.K;
                case Act.Dodge: return KeyCode.L;
                case Act.Bow: return KeyCode.I;
                case Act.Farr: return KeyCode.U;
                case Act.Interact: return KeyCode.E;
                default: return KeyCode.LeftShift;
            }
        }
    }
}
