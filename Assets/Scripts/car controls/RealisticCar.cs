using UnityEngine;

// РЕАЛИСТИЧНАЯ машина на Wheel Collider: двигатель с оборотами, 8-ступенчатый автомат,
// распределение тяги между осями, пробуксовка колёс, стабилизаторы, прижимная сила.
// Значения по умолчанию подобраны под BMW M5 F90.
[RequireComponent(typeof(Rigidbody))]
public class RealisticCar : MonoBehaviour
{
    [Header("Wheel Collider каждого колеса")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Кузов")]
    public float mass = 1950f;
    [Tooltip("Сдвиг центра масс по высоте. Чем ниже, тем труднее перевернуться")]
    public float centerOfMassOffsetY = -0.3f;
    [Tooltip("Сопротивление воздуха")]
    public float airDrag = 0.48f;
    [Tooltip("Прижимная сила, растёт с квадратом скорости")]
    public float downforce = 0.6f;

    [Header("Двигатель")]
    [Tooltip("Максимальный крутящий момент, Н·м")]
    public float maxEngineTorque = 750f;
    [Tooltip("Доля момента в зависимости от оборотов (0 — холостые, 1 — отсечка)")]
    public AnimationCurve torqueCurve = new AnimationCurve(
        new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0.8f));
    public float idleRpm = 800f;
    public float redlineRpm = 7000f;
    public float maxSpeedKmh = 305f;
    [Tooltip("Торможение двигателем при отпущенном газе, Н·м на колесо")]
    public float engineBraking = 80f;

    [Header("Коробка передач")]
    public float[] gearRatios = { 5.0f, 3.2f, 2.143f, 1.72f, 1.313f, 1.0f, 0.823f, 0.64f };
    public float reverseRatio = 3.478f;
    public float finalDrive = 3.15f;
    [Range(0.5f, 1f)] public float drivetrainEfficiency = 0.85f;
    public float shiftUpRpm = 6500f;
    public float shiftDownRpm = 3000f;
    public float shiftDelay = 0.4f;

    [Header("Привод")]
    [Range(0f, 1f), Tooltip("Доля тяги на задней оси: 1 — задний привод, 0.5 — поровну, 0 — передний")]
    public float rearTorqueBias = 0.7f;
    [Range(0f, 1f), Tooltip("Антипробуксовка: 0 — выключена, 1 — максимальная")]
    public float tractionControl = 0.6f;

    [Header("Тормоза")]
    [Tooltip("Тормозной момент на переднее колесо, Н·м")]
    public float brakeTorque = 3500f;
    [Range(0f, 1f), Tooltip("Доля тормозного момента на задних колёсах")]
    public float rearBrakeRatio = 0.6f;
    public float handbrakeTorque = 6000f;
    [Range(0f, 1f), Tooltip("Доля бокового сцепления зада при зажатом ручнике")]
    public float handbrakeGrip = 0.55f;

    [Header("Руль")]
    public float maxSteerAngle = 35f;
    public float highSpeedSteerAngle = 6f;
    public float steerFadeSpeedKmh = 200f;
    public float steerSpeed = 110f;

    [Header("Шины и подвеска")]
    public float frontGrip = 1.2f;
    public float rearGrip = 1.2f;
    public float suspensionDistance = 0.25f;
    public float suspensionSpring = 90000f;
    public float suspensionDamper = 6500f;
    [Tooltip("Стабилизатор поперечной устойчивости: уменьшает крен в поворотах")]
    public float antiRoll = 9000f;

    public float SpeedKmh { get; private set; }
    public float EngineRpm { get; private set; }
    public int Gear { get; private set; } = 1;   // -1 — задняя

    Rigidbody rb;
    float steerAngle;
    float throttleInput;
    float steerInput;
    bool handbrake;
    float shiftTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        SetupWheel(frontLeft, frontGrip);
        SetupWheel(frontRight, frontGrip);
        SetupWheel(rearLeft, rearGrip);
        SetupWheel(rearRight, rearGrip);

        EngineRpm = idleRpm;
    }

    void Start()
    {
        rb.centerOfMass = rb.centerOfMass + new Vector3(0f, centerOfMassOffsetY, 0f);
    }

    void SetupWheel(WheelCollider wheel, float grip)
    {
        if (wheel == null) return;

        wheel.mass = 25f;
        wheel.suspensionDistance = suspensionDistance;

        JointSpring spring = wheel.suspensionSpring;
        spring.spring = suspensionSpring;
        spring.damper = suspensionDamper;
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;

        WheelFrictionCurve forward = wheel.forwardFriction;
        forward.extremumSlip = 0.4f;
        forward.extremumValue = 1f;
        forward.asymptoteSlip = 0.8f;
        forward.asymptoteValue = 0.6f;
        forward.stiffness = grip;
        wheel.forwardFriction = forward;

        WheelFrictionCurve sideways = wheel.sidewaysFriction;
        sideways.extremumSlip = 0.2f;
        sideways.extremumValue = 1f;
        sideways.asymptoteSlip = 0.5f;
        sideways.asymptoteValue = 0.75f;
        sideways.stiffness = grip;
        wheel.sidewaysFriction = sideways;

        wheel.ConfigureVehicleSubsteps(5f, 12, 15);
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
        if (frontLeft == null || frontRight == null || rearLeft == null || rearRight == null) return;

        float dt = Time.fixedDeltaTime;
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        SpeedKmh = speed * 3.6f;
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);

        // Руль
        float steerLimit = Mathf.Lerp(maxSteerAngle, highSpeedSteerAngle,
            Mathf.InverseLerp(0f, steerFadeSpeedKmh, SpeedKmh));
        steerAngle = Mathf.MoveTowards(steerAngle, steerInput * steerLimit, steerSpeed * dt);
        frontLeft.steerAngle = steerAngle;
        frontRight.steerAngle = steerAngle;

        // Газ, тормоз или задний ход
        float throttle = 0f;
        float brake = 0f;
        if (throttleInput > 0f)
        {
            if (forwardSpeed < -1f) brake = throttleInput;
            else
            {
                if (Gear == -1) Gear = 1;
                throttle = throttleInput;
            }
        }
        else if (throttleInput < 0f)
        {
            if (forwardSpeed > 1f) brake = -throttleInput;
            else
            {
                Gear = -1;
                throttle = -throttleInput;
            }
        }

        // Обороты двигателя считаем от скорости вращения ведущих колёс
        bool reverse = Gear == -1;
        float ratio = (reverse ? reverseRatio : gearRatios[Gear - 1]) * finalDrive;
        float wheelRpm = DrivenWheelRpm();
        float targetRpm = Mathf.Max(idleRpm, Mathf.Abs(wheelRpm) * ratio);
        EngineRpm = Mathf.Lerp(EngineRpm, targetRpm, dt * 10f);

        // Автоматическое переключение передач
        shiftTimer -= dt;
        if (!reverse && shiftTimer <= 0f)
        {
            if (EngineRpm > shiftUpRpm && Gear < gearRatios.Length)
            {
                Gear++;
                shiftTimer = shiftDelay;
            }
            else if (EngineRpm < shiftDownRpm && Gear > 1)
            {
                Gear--;
                shiftTimer = shiftDelay;
            }
        }

        // Момент двигателя и момент на колёсах
        float engineTorque = throttle * maxEngineTorque * torqueCurve.Evaluate(EngineRpm / redlineRpm);
        if (EngineRpm >= redlineRpm) engineTorque = 0f;
        if (SpeedKmh >= maxSpeedKmh) engineTorque = 0f;

        float wheelTorque = engineTorque * ratio * drivetrainEfficiency * (reverse ? -1f : 1f);
        float frontTorque = wheelTorque * (1f - rearTorqueBias) * 0.5f;
        float rearTorque = wheelTorque * rearTorqueBias * 0.5f;

        frontLeft.motorTorque = LimitWheelSpin(frontLeft, frontTorque);
        frontRight.motorTorque = LimitWheelSpin(frontRight, frontTorque);
        rearLeft.motorTorque = LimitWheelSpin(rearLeft, rearTorque);
        rearRight.motorTorque = LimitWheelSpin(rearRight, rearTorque);

        // Тормоза
        float coast = throttle <= 0f && brake <= 0f ? engineBraking : 0f;
        float frontBrake = brake * brakeTorque + coast;
        float rearBrake = brake * brakeTorque * rearBrakeRatio + coast;
        if (handbrake) rearBrake += handbrakeTorque;

        frontLeft.brakeTorque = frontBrake;
        frontRight.brakeTorque = frontBrake;
        rearLeft.brakeTorque = rearBrake;
        rearRight.brakeTorque = rearBrake;

        // Ручник снижает боковое сцепление задних колёс
        SetSidewaysGrip(rearLeft, handbrake ? rearGrip * handbrakeGrip : rearGrip);
        SetSidewaysGrip(rearRight, handbrake ? rearGrip * handbrakeGrip : rearGrip);

        ApplyAntiRoll(frontLeft, frontRight);
        ApplyAntiRoll(rearLeft, rearRight);

        // Аэродинамика
        rb.AddForce(-velocity * speed * airDrag);
        rb.AddForce(-transform.up * downforce * speed * speed);
    }

    float DrivenWheelRpm()
    {
        float sum = 0f;
        int count = 0;
        if (rearTorqueBias > 0f)
        {
            sum += rearLeft.rpm + rearRight.rpm;
            count += 2;
        }
        if (rearTorqueBias < 1f)
        {
            sum += frontLeft.rpm + frontRight.rpm;
            count += 2;
        }
        return sum / count;
    }

    // Антипробуксовка: если колесо буксует сильнее порога, убираем с него часть тяги
    float LimitWheelSpin(WheelCollider wheel, float torque)
    {
        if (tractionControl <= 0f) return torque;
        if (!wheel.GetGroundHit(out WheelHit hit)) return torque;

        float slip = Mathf.Abs(hit.forwardSlip);
        const float slipLimit = 0.35f;
        if (slip <= slipLimit) return torque;

        return torque * Mathf.Clamp01(1f - (slip - slipLimit) * tractionControl * 5f);
    }

    void SetSidewaysGrip(WheelCollider wheel, float grip)
    {
        WheelFrictionCurve sideways = wheel.sidewaysFriction;
        if (Mathf.Approximately(sideways.stiffness, grip)) return;
        sideways.stiffness = grip;
        wheel.sidewaysFriction = sideways;
    }

    void ApplyAntiRoll(WheelCollider left, WheelCollider right)
    {
        float travelLeft = 1f;
        float travelRight = 1f;

        bool groundedLeft = left.GetGroundHit(out WheelHit hitLeft);
        if (groundedLeft)
            travelLeft = (-left.transform.InverseTransformPoint(hitLeft.point).y - left.radius) / left.suspensionDistance;

        bool groundedRight = right.GetGroundHit(out WheelHit hitRight);
        if (groundedRight)
            travelRight = (-right.transform.InverseTransformPoint(hitRight.point).y - right.radius) / right.suspensionDistance;

        float force = (travelLeft - travelRight) * antiRoll;

        if (groundedLeft) rb.AddForceAtPosition(left.transform.up * -force, left.transform.position);
        if (groundedRight) rb.AddForceAtPosition(right.transform.up * force, right.transform.position);
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        string gear = Gear == -1 ? "R" : Gear.ToString();
        GUI.Label(new Rect(20, 20, 600, 40),
            Mathf.RoundToInt(SpeedKmh) + " км/ч   передача " + gear + "   " + Mathf.RoundToInt(EngineRpm) + " об/мин", style);
    }
}
