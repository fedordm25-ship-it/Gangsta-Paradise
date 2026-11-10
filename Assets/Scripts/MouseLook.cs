using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [SerializeField] float sensitivity = 0.1f;
    float yaw, pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;  // прячем и фиксируем курсор
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (Mouse.current == null) return;
        Vector2 delta = Mouse.current.delta.ReadValue() * sensitivity;
        yaw += delta.x;
        pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
    void OnGUI()
    {
        GUI.Label(new Rect(Screen.width / 2f - 4f, Screen.height / 2f - 10f, 20f, 20f), "+");
    }
}
