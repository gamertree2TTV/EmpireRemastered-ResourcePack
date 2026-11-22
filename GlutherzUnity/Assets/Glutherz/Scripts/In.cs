using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Glutherz
{
    public enum Act { Attack, Block, Heavy, Potion, Interact, Menu, Cancel, Jump, Sprint }

    /// <summary>Eingabe-Abstraktion: nutzt das neue Input System, wenn aktiv, sonst die klassische Eingabe.</summary>
    public static class In
    {
#if ENABLE_INPUT_SYSTEM
        static ButtonControl Ctl(Act a)
        {
            var k = Keyboard.current; var m = Mouse.current;
            switch (a)
            {
                case Act.Attack: return m != null ? m.leftButton : null;
                case Act.Block: return m != null ? m.rightButton : null;
            }
            if (k == null) return null;
            switch (a)
            {
                case Act.Heavy: return k.qKey;
                case Act.Potion: return k.rKey;
                case Act.Interact: return k.eKey;
                case Act.Menu: return k.tabKey;
                case Act.Cancel: return k.escapeKey;
                case Act.Jump: return k.spaceKey;
                case Act.Sprint: return k.leftShiftKey;
            }
            return null;
        }

        public static bool Down(Act a) { var c = Ctl(a); return c != null && c.wasPressedThisFrame; }
        public static bool Held(Act a) { var c = Ctl(a); return c != null && c.isPressed; }

        public static Vector2 Move()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        public static Vector2 Look()
        {
            var m = Mouse.current;
            return m == null ? Vector2.zero : m.delta.ReadValue() * 0.1f;
        }
#else
        static KeyCode Key(Act a)
        {
            switch (a)
            {
                case Act.Attack: return KeyCode.Mouse0;
                case Act.Block: return KeyCode.Mouse1;
                case Act.Heavy: return KeyCode.Q;
                case Act.Potion: return KeyCode.R;
                case Act.Interact: return KeyCode.E;
                case Act.Menu: return KeyCode.Tab;
                case Act.Cancel: return KeyCode.Escape;
                case Act.Jump: return KeyCode.Space;
                default: return KeyCode.LeftShift;
            }
        }

        public static bool Down(Act a) { return Input.GetKeyDown(Key(a)); }
        public static bool Held(Act a) { return Input.GetKey(Key(a)); }
        public static Vector2 Move() { return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); }
        public static Vector2 Look() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 2f; }
#endif
    }
}
