using UnityEngine;
using UnityEngine.InputSystem;

namespace TrafikParkuru.Vehicle
{
    /// <summary>
    /// FPS kamerada sağa/sola bakma: ok tuşları + fare hareketi ile yatay döndürme.
    /// Direksiyon seti kullanımı için ok tuşları birincil kontrol.
    /// </summary>
    public class FpsLookAround : MonoBehaviour
    {
        [Header("Bakış Ayarları")]
        [SerializeField] private float keyboardSpeed = 25f;     // derece/saniye (daha yavaş ve pürüzsüz)
        [SerializeField] private float mouseSensitivity = 0.005f; // fare hassasiyeti düşürüldü
        [SerializeField] private float maxYaw = 90f;            // sağa/sola maks açı (90 derece idealdir)
        [SerializeField] private float returnSpeed = 3f;         // tuş bırakıldığında merkeze dönüş hızı
        [SerializeField] private float smoothTime = 0.25f;       // dönüş yumuşatma süresi (arttırıldı)

        [Header("Giriş")]
        [SerializeField] private InputActionAsset inputActions;

        private InputAction lookAction; // sol/sağ ok tuşları
        private float currentYaw = 0f;
        private float targetYaw = 0f;
        private float yawVelocity = 0f;
        private Quaternion baseRotation;
        private bool hasInput;

        private void Awake()
        {
            baseRotation = transform.localRotation;

            if (inputActions != null)
            {
                var map = inputActions.FindActionMap("Driving", true);
                lookAction = map.FindAction("Look", false);
            }
        }

        private void OnEnable()
        {
            lookAction?.Enable();
            currentYaw = 0f;
            targetYaw = 0f;
            yawVelocity = 0f;
        }

        private void OnDisable()
        {
            // Kamera eski konumuna dönsün
            transform.localRotation = baseRotation;
            currentYaw = 0f;
            targetYaw = 0f;
            yawVelocity = 0f;
        }

        private void LateUpdate()
        {
            float input = 0f;
            hasInput = false;

            // 1) Ok tuşları / gamepad stick (InputSystem)
            if (lookAction != null)
            {
                float val = lookAction.ReadValue<float>();
                if (Mathf.Abs(val) > 0.05f)
                {
                    input += val * keyboardSpeed * Time.deltaTime;
                    hasInput = true;
                }
            }

            // 2) Fare X hareketi (New Input System)
            if (Mouse.current != null)
            {
                float mouseX = Mouse.current.delta.x.ReadValue();
                if (Mathf.Abs(mouseX) > 0.5f)
                {
                    input += mouseX * mouseSensitivity;
                    hasInput = true;
                }
            }

            if (hasInput)
            {
                targetYaw = Mathf.Clamp(targetYaw + input, -maxYaw, maxYaw);
            }
            else
            {
                // Girdi yoksa yavaşça merkeze dön
                targetYaw = Mathf.MoveTowards(targetYaw, 0f, returnSpeed * Time.deltaTime * 60f);
            }

            // Dönüşü SmoothDamp ile yumuşat
            currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, smoothTime);

            transform.localRotation = baseRotation * Quaternion.Euler(0f, currentYaw, 0f);
        }
    }
}
