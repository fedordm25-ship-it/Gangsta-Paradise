using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Общий ввод для обеих машин. Работает и со старым Input Manager, и с новым Input System.
// W/S или стрелки — газ и тормоз/задний ход, A/D — руль, Пробел — ручник, R — поставить машину на колёса.
public static class CarInput
{
    public static float Throttle
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return 0f;
            float v = 0f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
            return v;
#else
            float v = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            return v;
#endif
        }
    }

    public static float Steer
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return 0f;
            float v = 0f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v -= 1f;
            return v;
#else
            float v = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v -= 1f;
            return v;
#endif
        }
    }

    public static bool Handbrake
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }
    }

    // Движение мыши за кадр (одинаковый масштаб для старого и нового ввода)
    public static Vector2 MouseDelta
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m != null ? m.delta.ReadValue() * 0.1f : Vector2.zero;
#else
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
        }
    }

    // Вызывать только из Update
    public static bool ResetPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }
}
