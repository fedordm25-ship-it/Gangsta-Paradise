using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] Transform cam;              // камера (пусто = Main Camera)
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float gravity = -20f;

    [Header("Граница площадки")]
    [SerializeField] Vector3 arenaCenter = Vector3.zero;
    [SerializeField] float arenaRadius = 6f;

    CharacterController controller;
    float verticalSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null) cam = Camera.main.transform;
    }

    void Update()
    {
        Vector2 input = ReadInput();

        // Направления «вперёд» и «вправо» берём от камеры, но без наклона вверх-вниз
        Vector3 forward = cam.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = cam.right; right.y = 0f; right.Normalize();

        Vector3 move = forward * input.y + right * input.x;
        if (move.sqrMagnitude > 1f) move.Normalize();   // по диагонали не быстрее

        bool running = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float speed = running ? runSpeed : walkSpeed;

        // Гравитация: прижимаем к земле
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = move * speed;
        velocity.y = verticalSpeed;
        controller.Move(velocity * Time.deltaTime);

        KeepInsideArena();
    }

    Vector2 ReadInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (kb.wKey.isPressed) y += 1f;
        if (kb.sKey.isPressed) y -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.aKey.isPressed) x -= 1f;
        return new Vector2(x, y);
    }

    void KeepInsideArena()
    {
        Vector3 offset = transform.position - arenaCenter;
        offset.y = 0f;

        if (offset.magnitude > arenaRadius)
        {
            // Возвращаем игрока на край круга
            Vector3 edge = arenaCenter + offset.normalized * arenaRadius;
            Vector3 correction = edge - transform.position;
            correction.y = 0f;
            controller.Move(correction);
        }
    }

    // Рисует границу в окне Scene, чтобы её было видно при настройке
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius);
    }
}