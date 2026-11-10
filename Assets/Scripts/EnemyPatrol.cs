using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] Vector3 axis = Vector3.right;  // направление движения (по умолчанию ось X)
    [SerializeField] float distance = 3f;           // на сколько метров уходит в каждую сторону
    [SerializeField] float speed = 2f;              // метров в секунду
    [SerializeField] bool randomStart = true;       // чтобы кубы не двигались синхронно

    EnemyHealth health;
    float offset;      // насколько сейчас сдвинут от середины
    int dir = 1;       // 1 = вправо, -1 = влево

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
        axis = axis.normalized;

        if (randomStart)
        {
            offset = Random.Range(-distance, distance);
            dir = Random.value < 0.5f ? 1 : -1;
            transform.position += axis * offset;
        }
    }

    void Update()
    {
        if (health != null && health.IsDead) return;   // мёртвые не ходят

        float step = speed * Time.deltaTime * dir;
        offset += step;

        // Дошёл до края — разворачиваемся
        if (offset > distance) dir = -1;
        if (offset < -distance) dir = 1;

        transform.position += axis * step;
    }
}