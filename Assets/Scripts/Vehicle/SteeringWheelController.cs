using UnityEngine;

public class SteeringWheelController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private CarController carController;

    [Header("Ayarlar")]
    [SerializeField] private float maxRotationAngle = 360f; // Tam sag veya tam solda kac derece donecegi
    [SerializeField] private Vector3 rotationAxis = Vector3.forward; // Varsayılan Z ekseni
    [SerializeField] private float straightAngleOffset = 0f; // Direksiyonun düz durması için açı ofseti

    private Quaternion originalRotation;

    private void Awake()
    {
        if (carController == null)
        {
            carController = GetComponentInParent<CarController>();
        }
        originalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (carController == null) return;

        // Tekerleklerin direksiyon girdi degeri (-1 ile +1 arasinda)
        float steerInput = carController.SteerInput;

        // Belirlenen eksende döndür, düzleştirme ofsetini ekle
        transform.localRotation = originalRotation * Quaternion.AngleAxis(straightAngleOffset - steerInput * maxRotationAngle, rotationAxis);
    }
}
