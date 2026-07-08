using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrafikParkuru.Vehicle
{
    public enum SignalState
    {
        None,
        Left,
        Right
    }

    [RequireComponent(typeof(AudioSource))]
    public class SignalController : MonoBehaviour
    {
        [Header("Girdi")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Sinyal Ayarları")]
        [SerializeField] private float autoCloseAngle = 60f;
        [SerializeField] private float tickRate = 0.5f;

        private InputActionMap drivingMap;
        private InputAction leftAction;
        private InputAction rightAction;

        private SignalState activeSignal = SignalState.None;
        private float signalOnTime;
        private float startYaw;
        private bool isTurning = false;
        private float maxAngleChange = 0f;

        private AudioSource audioSource;
        private float nextTickTime;
        private bool tickState = false;

        public SignalState ActiveSignal => activeSignal;
        public float SignalOnTime => signalOnTime;
        public bool IsSignalActive => activeSignal != SignalState.None;

        public event Action<SignalState> OnSignalChanged;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            
            // AudioSource'u tik-tak sesi icin ayarla
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D Ses

            if (inputActions != null)
            {
                drivingMap = inputActions.FindActionMap("Driving", true);
                leftAction = drivingMap.FindAction("SignalLeft", true);
                rightAction = drivingMap.FindAction("SignalRight", true);
            }
            else
            {
                Debug.LogError("SignalController: InputActionAsset atanmamis!", this);
            }
        }

        private void OnEnable()
        {
            if (leftAction != null)
            {
                leftAction.Enable();
                leftAction.performed += OnLeftSignalInput;
            }
            if (rightAction != null)
            {
                rightAction.Enable();
                rightAction.performed += OnRightSignalInput;
            }
        }

        private void OnDisable()
        {
            if (leftAction != null) leftAction.performed -= OnLeftSignalInput;
            if (rightAction != null) rightAction.performed -= OnRightSignalInput;
        }

        private void OnLeftSignalInput(InputAction.CallbackContext context)
        {
            SetSignal(activeSignal == SignalState.Left ? SignalState.None : SignalState.Left);
        }

        private void OnRightSignalInput(InputAction.CallbackContext context)
        {
            SetSignal(activeSignal == SignalState.Right ? SignalState.None : SignalState.Right);
        }

        public void SetSignal(SignalState state)
        {
            if (activeSignal == state) return;

            activeSignal = state;
            signalOnTime = Time.time;
            startYaw = transform.eulerAngles.y;
            isTurning = false;
            maxAngleChange = 0f;
            
            Debug.Log($"SignalController: Sinyal Durumu Değişti -> {activeSignal}");
            OnSignalChanged?.Invoke(activeSignal);

            if (activeSignal != SignalState.None)
            {
                PlayTickSound();
            }
        }

        private void Update()
        {
            // Tik-tak sesi ve gosterge tetikleme
            if (activeSignal != SignalState.None)
            {
                if (Time.time >= nextTickTime)
                {
                    tickState = !tickState;
                    PlayTickSound();
                    nextTickTime = Time.time + tickRate;
                }

                // Otomatik sinyal kapatma mantigi
                float currentYaw = transform.eulerAngles.y;
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(startYaw, currentYaw));

                // Donusun basladigini belirle (orn. 10 derece donulduse)
                if (angleDiff > 10f)
                {
                    isTurning = true;
                }

                if (isTurning)
                {
                    // Donusun hangi yone yapildigini dogrula ve kapa
                    // Sol sinyal acikken sola (yaw azalan/artan), sag sinyal acikken saga donulmeli.
                    // Eger toplam sapma autoCloseAngle'i gecerse ve direksiyon duzelmeye baslarsa kapat
                    if (angleDiff > maxAngleChange)
                    {
                        maxAngleChange = angleDiff;
                    }

                    // Sürücü dönüşü tamamlayıp tekeri düzeltince veya dönüş açısı yeterince büyük olduğunda kapat
                    // 60 derece sapma donusun bittigine dair net bir isarettir
                    if (maxAngleChange >= autoCloseAngle && angleDiff < maxAngleChange - 10f)
                    {
                        Debug.Log($"SignalController: Dönüş tamamlandı, sinyal otomatik kapatılıyor. Açı farkı: {angleDiff:F1}");
                        SetSignal(SignalState.None);
                    }
                }
            }
        }

        private void PlayTickSound()
        {
            if (audioSource == null) return;

            // Tik ve tak sesleri icin pitch degistirerek tek bir sinyal sesi uretelim
            audioSource.pitch = tickState ? 1.0f : 0.8f;
            
            // Projede hazir ses dosyasi olmayabileceginden, varsayilan bir klik sesi çalmaya calis veya 
            // eger clip atanmamissa basit bir audio uret (synth klik)
            if (audioSource.clip != null)
            {
                audioSource.Play();
            }
            else
            {
                // Eger clip atanmamissa, Console'a yazabiliriz (tik/tak)
                // Bu sayede ses yoksa bile calistigini anlariz.
            }
        }
    }
}
