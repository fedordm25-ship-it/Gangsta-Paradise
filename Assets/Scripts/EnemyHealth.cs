using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float deathPush = 6f;      // сила толчка при смерти

    [Header("Реакция на попадание")]
    [SerializeField] Transform visual;          // что качать (пусто = сам объект)
    [SerializeField] float flinchAngle = 15f;   // на сколько градусов отклоняется
    [SerializeField] float flinchTime = 0.2f;   // за сколько секунд выпрямляется
    [SerializeField] float knockback = 0.15f;   // на сколько метров отталкивает

    float health;
    bool isDead;
    public bool IsDead => isDead;
    Quaternion baseRotation;
    Coroutine flinchRoutine;

    void Awake()
    {
        health = maxHealth;
        if (visual == null) visual = transform;
        baseRotation = visual.localRotation;
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        health -= amount;

        if (health <= 0f)
        {
            Die(hitPoint, hitDirection);
            return;
        }

        // Отталкивание по горизонтали
        Vector3 flatDir = new Vector3(hitDirection.x, 0f, hitDirection.z).normalized;
        transform.position += flatDir * knockback;

        // Отклонение назад (если уже качается — начинаем заново)
        if (flinchRoutine != null) StopCoroutine(flinchRoutine);
        flinchRoutine = StartCoroutine(Flinch(flatDir));
    }

    IEnumerator Flinch(Vector3 dir)
    {
        Vector3 axis = Vector3.Cross(Vector3.up, dir);   // ось, вокруг которой «запрокидывает»
        float t = 0f;
        while (t < flinchTime)
        {
            t += Time.deltaTime;
            float k = 1f - t / flinchTime;               // от 1 к 0: резко отклонился, плавно вернулся
            visual.localRotation = baseRotation;
            visual.rotation = Quaternion.AngleAxis(flinchAngle * k, axis) * visual.rotation;
            yield return null;                           // ждём следующий кадр
        }
        visual.localRotation = baseRotation;
    }

    void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        isDead = true;
        if (flinchRoutine != null) StopCoroutine(flinchRoutine);

        if (!TryGetComponent(out Rigidbody rb))
            rb = gameObject.AddComponent<Rigidbody>();
        rb.AddForceAtPosition(hitDirection * deathPush, hitPoint, ForceMode.Impulse);
        Destroy(gameObject, 3f);
    }
}