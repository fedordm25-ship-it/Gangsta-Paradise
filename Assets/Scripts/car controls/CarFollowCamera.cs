using UnityEngine;

// Камера от третьего лица для машины. Повесить на Main Camera и перетащить машину в поле Target.
// Мышью можно осматриваться вокруг машины; если мышь не трогать, камера сама возвращается за багажник.
public class CarFollowCamera : MonoBehaviour
{
    public Transform target;

    [Header("Положение")]
    public float distance = 6.5f;
    [Tooltip("На какую высоту над машиной смотрит камера")]
    public float lookHeight = 1f;
    [Tooltip("Наклон камеры вниз по умолчанию, градусы")]
    public float defaultPitch = 12f;
    [Tooltip("Как быстро камера доворачивает за машиной")]
    public float turnSpeed = 5f;

    [Header("Обзор мышью")]
    public float mouseSensitivity = 2f;
    public float minPitch = -5f;
    public float maxPitch = 60f;
    [Tooltip("Через сколько секунд без движения мыши камера возвращается за машину")]
    public float recenterDelay = 2f;
    public float recenterSpeed = 3f;
    public bool lockCursor = true;

    float followYaw;     // куда смотрит машина (сглаженно)
    float orbitYaw;      // на сколько мышь повернула камеру вокруг машины
    float pitch;
    float idleTime;

    void Start()
    {
        if (target != null) followYaw = target.eulerAngles.y;
        pitch = defaultPitch;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector2 mouse = CarInput.MouseDelta * mouseSensitivity;

        if (mouse.sqrMagnitude > 0.0001f)
        {
            idleTime = 0f;
            orbitYaw += mouse.x;
            pitch = Mathf.Clamp(pitch - mouse.y, minPitch, maxPitch);
        }
        else
        {
            idleTime += Time.deltaTime;
            if (idleTime > recenterDelay)
            {
                orbitYaw = Mathf.LerpAngle(orbitYaw, 0f, recenterSpeed * Time.deltaTime);
                pitch = Mathf.Lerp(pitch, defaultPitch, recenterSpeed * Time.deltaTime);
            }
        }

        followYaw = Mathf.LerpAngle(followYaw, target.eulerAngles.y, turnSpeed * Time.deltaTime);

        Quaternion rotation = Quaternion.Euler(pitch, followYaw + orbitYaw, 0f);
        Vector3 pivot = target.position + Vector3.up * lookHeight;

        transform.position = pivot - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }
}
