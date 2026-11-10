using UnityEngine;
using UnityEngine.InputSystem;

public class SemiAutoGun : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] Camera cam;
    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] ParticleSystem bloodPrefab;
    [SerializeField] ParticleSystem moneyPrefab;

    [Header("Параметры оружия")]
    [SerializeField] float fireCooldown = 0.15f;  // минимум секунд между выстрелами
    [SerializeField] float inputBuffer = 0.12f;   // сколько секунд помним ранний клик
    [SerializeField] float damage = 25f;
    [SerializeField] float range = 100f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] bool automatic = false;      // true = стреляет, пока зажата кнопка// ~0 = все слои

    float nextFireTime;
    float bufferedUntil = -1f;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (automatic)
        {
            // Автомат: пока кнопка зажата — стреляем с темпом fireCooldown
            if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireCooldown;
            }
        }
        else
        {
            // Полуавтомат: один клик — один выстрел (с буфером)
            if (Mouse.current.leftButton.wasPressedThisFrame)
                bufferedUntil = Time.time + inputBuffer;

            if (Time.time >= nextFireTime && Time.time <= bufferedUntil)
            {
                Shoot();
                bufferedUntil = -1f;
                nextFireTime = Time.time + fireCooldown;
            }
        }
    }

    void Shoot()
    {
        if (muzzleFlash != null) muzzleFlash.Play();

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            if (hit.collider.TryGetComponent(out EnemyHealth enemy) && !enemy.IsDead)
            {
                enemy.TakeDamage(damage, hit.point, ray.direction);
                SpawnEffect(bloodPrefab, hit.point, ray.direction);
                SpawnEffect(moneyPrefab, hit.point, ray.direction);
            }
        }
    }

    void SpawnEffect(ParticleSystem prefab, Vector3 point, Vector3 direction)
    {
        if (prefab == null) return;
        ParticleSystem fx = Instantiate(prefab, point, Quaternion.LookRotation(direction));
        Destroy(fx.gameObject, 3f);
    }
}