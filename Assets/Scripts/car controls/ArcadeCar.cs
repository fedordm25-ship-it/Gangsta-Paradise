using UnityEngine;

// АРКАДНАЯ машина. Колёс как физических объектов нет: из четырёх точек вниз идут лучи,
// они держат кузов над землёй (подвеска), а сцепление и тяга считаются простыми формулами.
// Значения по умолчанию подобраны под BMW M5 F90.
[RequireComponent(typeof(Rigidbody))]
public class ArcadeCar : MonoBehaviour
{
    public enum DriveType { Rear, Front, All }

    [Header("Точки колёс: пустые объекты в центрах колёс")]
    public Transform frontLeft;
    public Transform frontRight;
    public Transform rearLeft;
    public Transform rearRight;

    [Header("Кузов")]
    public float mass = 1950f;
    [Tooltip("Сдвиг центра масс по высоте. Чем ниже, тем труднее перевернуться")]
    public float centerOfMassOffsetY = -0.4f;

    [Header("Двигатель и тормоза")]
    public DriveType drive = DriveType.All;
    [Tooltip("Ускорение с места, м/с². 10 даёт примерно 3,5 с до сотни")]
    public float acceleration = 10f;
    public float maxSpeedKmh = 305f;
    public float reverseSpeedKmh = 40f;
    [Tooltip("Замедление при торможении, м/с²")]
    public float brakeDeceleration = 14f;
    [Tooltip("Насколько быстро машина теряет скорость без газа")]
    public float coastDrag = 0.3f;

    [Header("Руль")]
    public float maxSteerAngle = 32f;
    [Tooltip("Угол поворота колёс на большой скорости")]
    public float highSpeedSteerAngle = 7f;
    [Tooltip("Скорость, на которой угол становится минимальным")]
    public float steerFadeSpeedKmh = 180f;
    [Tooltip("Как быстро поворачивается руль, градусов в секунду")]
    public float steerSpeed = 140f;

    [Header("Сцепление и занос")]
    [Tooltip("Сцепление передних колёс. Больше — резче входит в поворот")]
    public float frontGrip = 2.0f;
    [Tooltip("Сцепление задних колёс. Меньше переднего — зад легко срывается")]
    public float rearGrip = 2.1f;
    [Tooltip("Как резко шины сопротивляются боковому скольжению")]
    public float cornerStiffness = 20f;
    [Range(0f, 1f), Tooltip("Доля сцепления зада при зажатом ручнике")]
    public float handbrakeGrip = 0.35f;
    [Range(0f, 1f), Tooltip("Насколько газ в повороте срывает зад (0 — не срывает)")]
    public float throttleDrift = 0.3f;
    [Tooltip("Замедление от ручника, м/с²")]
    public float handbrakeDeceleration = 6f;
    [Tooltip("Насколько быстро машина перестаёт вращаться, когда руль отпущен (0 — выключено)")]
    public float straightenStrength = 5f;

    [Header("Подвеска")]
    public float wheelRadius = 0.34f;
    [Tooltip("Полный ход подвески, м")]
    public float suspensionTravel = 0.25f;
    [Tooltip("Жёсткость: 1 — машина стоит на середине хода подвески")]
    public float springMultiplier = 1f;
    [Range(0.1f, 1.5f), Tooltip("Гашение раскачки")]
    public float damping = 0.7f;
    public LayerMask groundMask = ~0;

    public float SpeedKmh { get; private set; }

    Rigidbody rb;
    float steerAngle;
    float throttleInput;
    float steerInput;
    bool handbrake;
    readonly RaycastHit[] hits = new RaycastHit[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Start()
    {
        rb.centerOfMass = rb.centerOfMass + new Vector3(0f, centerOfMassOffsetY, 0f);
    }

    void Update()
    {
        throttleInput = CarInput.Throttle;
        steerInput = CarInput.Steer;
        handbrake = CarInput.Handbrake;

        if (CarInput.ResetPressed)
        {
            transform.SetPositionAndRotation(
                transform.position + Vector3.up * 1f,
                Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        float speed = rb.linearVelocity.magnitude;
        SpeedKmh = speed * 3.6f;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // Руль: на скорости колёса поворачиваются на меньший угол
        float steerLimit = Mathf.Lerp(maxSteerAngle, highSpeedSteerAngle,
            Mathf.InverseLerp(0f, steerFadeSpeedKmh, SpeedKmh));
        steerAngle = Mathf.MoveTowards(steerAngle, steerInput * steerLimit, steerSpeed * Time.fixedDeltaTime);

        // Газ, тормоз или задний ход
        float driveInput = 0f;
        float brakeInput = 0f;
        if (throttleInput > 0f)
        {
            if (forwardSpeed < -1f) brakeInput = 1f; else driveInput = throttleInput;
        }
        else if (throttleInput < 0f)
        {
            if (forwardSpeed > 1f) brakeInput = 1f; else driveInput = throttleInput;
        }

        // Тяга падает по мере приближения к максимальной скорости
        float driveForce = 0f;
        if (driveInput > 0f)
        {
            float k = Mathf.Clamp01(1f - forwardSpeed / (maxSpeedKmh / 3.6f));
            driveForce = driveInput * acceleration * mass * k;
        }
        else if (driveInput < 0f)
        {
            float k = Mathf.Clamp01(1f - (-forwardSpeed) / (reverseSpeedKmh / 3.6f));
            driveForce = driveInput * acceleration * 0.5f * mass * k;
        }

        bool frontDriven = drive != DriveType.Rear;
        bool rearDriven = drive != DriveType.Front;
        int drivenWheels = (frontDriven ? 2 : 0) + (rearDriven ? 2 : 0);
        float forcePerDrivenWheel = driveForce / drivenWheels;

        // Сцепление зада: падает от ручника и от газа в повороте
        float rearGripNow = rearGrip;
        if (handbrake) rearGripNow *= handbrakeGrip;
        if (rearDriven)
            rearGripNow *= 1f - throttleDrift * Mathf.Abs(driveInput) * Mathf.Abs(steerInput);

        SimulateWheel(frontLeft, steerAngle, frontGrip, frontDriven ? forcePerDrivenWheel : 0f, brakeInput, false);
        SimulateWheel(frontRight, steerAngle, frontGrip, frontDriven ? forcePerDrivenWheel : 0f, brakeInput, false);
        SimulateWheel(rearLeft, 0f, rearGripNow, rearDriven ? forcePerDrivenWheel : 0f, brakeInput, handbrake);
        SimulateWheel(rearRight, 0f, rearGripNow, rearDriven ? forcePerDrivenWheel : 0f, brakeInput, handbrake);

        // Выравнивание: чем меньше повёрнут руль, тем сильнее гасим вращение машины вокруг вертикали.
        // На ручнике не вмешиваемся, чтобы занос оставался управляемым.
        if (straightenStrength > 0f && !handbrake)
        {
            float yawRate = Vector3.Dot(rb.angularVelocity, transform.up);
            float release = 1f - Mathf.Abs(steerInput);
            rb.AddTorque(-transform.up * yawRate * straightenStrength * release, ForceMode.Acceleration);
        }
    }

    void SimulateWheel(Transform point, float steer, float grip, float driveForce, float brakeInput, bool handbrakeOn)
    {
        if (point == null) return;

        Vector3 up = transform.up;
        Vector3 origin = point.position + up * (suspensionTravel * 0.5f);
        float maxDistance = suspensionTravel + wheelRadius;

        if (!FindGround(origin, -up, maxDistance, out RaycastHit hit)) return;

        float wheelMass = mass * 0.25f;

        // Подвеска: пружина и амортизатор
        float springRate = springMultiplier * mass * 9.81f / (2f * suspensionTravel);
        float damperRate = 2f * damping * Mathf.Sqrt(springRate * wheelMass);
        float compression = maxDistance - hit.distance;
        Vector3 pointVelocity = rb.GetPointVelocity(point.position);
        float upSpeed = Vector3.Dot(pointVelocity, up);
        float load = Mathf.Max(0f, compression * springRate - upSpeed * damperRate);
        rb.AddForceAtPosition(up * load, point.position);

        // Направления колеса вдоль поверхности
        Vector3 wheelForward = Quaternion.AngleAxis(steer, up) * transform.forward;
        wheelForward = Vector3.ProjectOnPlane(wheelForward, hit.normal).normalized;
        Vector3 wheelRight = Vector3.Cross(hit.normal, wheelForward);

        float sideSpeed = Vector3.Dot(pointVelocity, wheelRight);
        float longSpeed = Vector3.Dot(pointVelocity, wheelForward);

        // Боковое сцепление: гасит скольжение вбок, но не сильнее, чем позволяет нагрузка на колесо
        float maxSideForce = load * grip;
        float sideForce = Mathf.Clamp(-sideSpeed * cornerStiffness * wheelMass, -maxSideForce, maxSideForce);

        // Продольные силы: тяга, тормоз, ручник, накат
        float longForce = driveForce;

        float deceleration = brakeInput * brakeDeceleration;
        if (handbrakeOn) deceleration += handbrakeDeceleration;
        if (deceleration > 0f)
        {
            float stopForce = Mathf.Abs(longSpeed) * wheelMass / Time.fixedDeltaTime;
            float brakeForce = Mathf.Min(deceleration * wheelMass, stopForce);
            longForce -= Mathf.Sign(longSpeed) * brakeForce;
        }
        else if (Mathf.Approximately(driveForce, 0f))
        {
            longForce -= longSpeed * coastDrag * wheelMass;
        }

        rb.AddForceAtPosition(wheelRight * sideForce + wheelForward * longForce, point.position);
    }

    // Ищем ближайшую поверхность под колесом, пропуская коллайдеры самой машины
    bool FindGround(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit best)
    {
        best = default;
        int count = Physics.RaycastNonAlloc(origin, direction, hits, maxDistance, groundMask, QueryTriggerInteraction.Ignore);
        bool found = false;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider.attachedRigidbody == rb) continue;
            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                best = hits[i];
                found = true;
            }
        }
        return found;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(20, 20, 400, 40), Mathf.RoundToInt(SpeedKmh) + " км/ч", style);
    }
}
