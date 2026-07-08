using UnityEngine;
using UnityEngine.InputSystem;

/// Geçici test aracı: Rigidbody + 4 WheelCollider tabanlı sürüş.
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Girdi")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Tekerlek Collider'ları")]
    [SerializeField] private WheelCollider wheelFL;
    [SerializeField] private WheelCollider wheelFR;
    [SerializeField] private WheelCollider wheelRL;
    [SerializeField] private WheelCollider wheelRR;

    [Header("Tekerlek Görselleri")]
    [SerializeField] private Transform visualFL;
    [SerializeField] private Transform visualFR;
    [SerializeField] private Transform visualRL;
    [SerializeField] private Transform visualRR;

    [Header("Sürüş Ayarları")]
    [SerializeField] private float motorTorque = 900f;
    [SerializeField] private float brakeTorque = 2500f;
    [SerializeField] private float idleBrakeTorque = 40f;
    [SerializeField] private float maxSteerAngle = 32f;
    [SerializeField] private float steerSpeed = 6f;
    [SerializeField] private float maxSpeedKmh = 80f;
    [SerializeField] private float maxReverseSpeedKmh = 25f;

    private Rigidbody rb;
    private InputActionMap drivingMap;
    private InputAction throttleAction;
    private InputAction brakeAction;
    private InputAction steerAction;
    private float currentSteer;
    private float lastCrashTime = -999f;
    private const float crashCooldown = 3f;

    // Hızlar m/s tutulur, gösterimde x3.6
    public float SpeedMs => rb != null ? rb.linearVelocity.magnitude : 0f;
    public float SpeedKmh => SpeedMs * 3.6f;
    public float SteerInput => currentSteer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.4f, 0.1f);

        AutoAssignWheels();

        if (inputActions != null)
        {
            drivingMap = inputActions.FindActionMap("Driving", true);
            throttleAction = drivingMap.FindAction("Throttle", true);
            brakeAction = drivingMap.FindAction("Brake", true);
            steerAction = drivingMap.FindAction("Steer", true);
        }
        else
        {
            Debug.LogError("CarController: InputActionAsset atanmamis!", this);
        }
    }

    private void OnEnable()
    {
        drivingMap?.Enable();
    }

    private void OnDisable()
    {
        drivingMap?.Disable();
    }

    private void AutoAssignWheels()
    {
        // Inspector'da atanmadiysa isimden bul
        if (wheelFL == null) wheelFL = FindWheel("WheelColliders/FL");
        if (wheelFR == null) wheelFR = FindWheel("WheelColliders/FR");
        if (wheelRL == null) wheelRL = FindWheel("WheelColliders/RL");
        if (wheelRR == null) wheelRR = FindWheel("WheelColliders/RR");
        if (visualFL == null) visualFL = transform.Find("RealCarVisual/WheelFrontL") ?? transform.Find("WheelVisuals/FL");
        if (visualFR == null) visualFR = transform.Find("RealCarVisual/WheelFrontR") ?? transform.Find("WheelVisuals/FR");
        if (visualRL == null) visualRL = transform.Find("RealCarVisual/WheelRearL") ?? transform.Find("WheelVisuals/RL");
        if (visualRR == null) visualRR = transform.Find("RealCarVisual/WheelRearR") ?? transform.Find("WheelVisuals/RR");
    }

    private WheelCollider FindWheel(string path)
    {
        Transform t = transform.Find(path);
        return t != null ? t.GetComponent<WheelCollider>() : null;
    }

    private void FixedUpdate()
    {
        if (wheelFL == null || wheelFR == null || wheelRL == null || wheelRR == null)
            return;

        float throttle = throttleAction != null ? throttleAction.ReadValue<float>() : 0f;
        float brake = brakeAction != null ? brakeAction.ReadValue<float>() : 0f;
        float steerTarget = steerAction != null ? steerAction.ReadValue<float>() : 0f;

        currentSteer = Mathf.MoveTowards(currentSteer, steerTarget, steerSpeed * Time.fixedDeltaTime);
        float steerAngle = currentSteer * maxSteerAngle;
        wheelFL.steerAngle = steerAngle;
        wheelFR.steerAngle = steerAngle;

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float motor = 0f;
        float brakeForce = 0f;

        if (throttle > 0.01f)
        {
            if (SpeedKmh < maxSpeedKmh || forwardSpeed < 0f)
                motor = throttle * motorTorque;
        }

        if (brake > 0.01f)
        {
            if (forwardSpeed > 0.5f)
            {
                // Ileri giderken S = fren
                brakeForce = brake * brakeTorque;
            }
            else
            {
                // Durunca S = geri vites
                if (SpeedKmh < maxReverseSpeedKmh)
                    motor = -brake * motorTorque * 0.7f;
            }
        }

        if (throttle <= 0.01f && brake <= 0.01f)
            brakeForce = idleBrakeTorque; // hafif motor freni

        wheelRL.motorTorque = motor;
        wheelRR.motorTorque = motor;

        wheelFL.brakeTorque = brakeForce;
        wheelFR.brakeTorque = brakeForce;
        wheelRL.brakeTorque = brakeForce;
        wheelRR.brakeTorque = brakeForce;
    }

    private void Update()
    {
        UpdateWheelVisual(wheelFL, visualFL);
        UpdateWheelVisual(wheelFR, visualFR);
        UpdateWheelVisual(wheelRL, visualRL);
        UpdateWheelVisual(wheelRR, visualRR);
    }

    private static void UpdateWheelVisual(WheelCollider col, Transform visual)
    {
        if (col == null || visual == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        if (visual.parent != null && visual.parent.name == "RealCarVisual")
        {
            visual.SetPositionAndRotation(pos, rot * Quaternion.Euler(270f, 0f, 0f));
        }
        else
        {
            visual.SetPositionAndRotation(pos, rot * Quaternion.Euler(0f, 0f, 90f));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (TrafikParkuru.Core.ScenarioManager.Instance == null) return;

        string otherName = collision.gameObject.name;
        if (otherName.Contains("NPC_Car") || otherName.Contains("ToyCar") || otherName.Contains("Pedestrian") || otherName.Contains("Cesium_Man"))
        {
            if (Time.time - lastCrashTime > crashCooldown)
            {
                lastCrashTime = Time.time;
                TrafikParkuru.Core.ScenarioManager.Instance.AddPenalty(-20, "Kaza Yaptınız: Diğer trafik unsurlarına çarptınız!");
            }
        }
    }
}
