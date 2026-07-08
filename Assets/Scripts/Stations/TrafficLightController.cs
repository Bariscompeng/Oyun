using UnityEngine;
using TrafikParkuru.Vehicle;

namespace TrafikParkuru.Stations
{
    public enum TrafficLightState
    {
        Red,
        RedYellow,
        Green
    }

    public class TrafficLightController : MonoBehaviour
    {
        [Header("Işık Görselleri (Renderer)")]
        [SerializeField] private Renderer redRenderer;
        [SerializeField] private Renderer yellowRenderer;
        [SerializeField] private Renderer greenRenderer;

        [Header("Materyal Ayarları")]
        [SerializeField] private Color redColor = Color.red;
        [SerializeField] private Color yellowColor = new Color(1f, 0.6f, 0f);
        [SerializeField] private Color greenColor = Color.green;
        [SerializeField] private float emissionIntensity = 3.0f;

        [Header("Zamanlama Eşikleri")]
        [SerializeField] private float requiredStopTime = 2.4f;
        [SerializeField] private float redYellowDuration = 1.3f;
        [SerializeField] private float speedThreshold = 0.3f; // 0.3 m/s (~1 km/h) altı durma sayılır

        private TrafficLightState currentState = TrafficLightState.Red;
        private bool isPlayerInZone = false;
        private CarController playerCar;
        private float stopTimer = 0f;
        private float stateTimer = 0f;

        public TrafficLightState CurrentState => currentState;

        private void Start()
        {
            SetLightState(TrafficLightState.Red);
        }

        private void Update()
        {
            if (currentState == TrafficLightState.Red)
            {
                if (isPlayerInZone && playerCar != null)
                {
                    // Oyuncunun hizi durma esiginin altinda mi?
                    if (playerCar.SpeedMs < speedThreshold)
                    {
                        stopTimer += Time.deltaTime;
                        if (stopTimer >= requiredStopTime)
                        {
                            SetLightState(TrafficLightState.RedYellow);
                        }
                    }
                    else
                    {
                        // Eger hareket ederse sayac sifirlanir
                        stopTimer = 0f;
                    }
                }
                else
                {
                    stopTimer = 0f;
                }
            }
            else if (currentState == TrafficLightState.RedYellow)
            {
                stateTimer += Time.deltaTime;
                if (stateTimer >= redYellowDuration)
                {
                    SetLightState(TrafficLightState.Green);
                }
            }
        }

        public void SetLightState(TrafficLightState state)
        {
            currentState = state;
            stateTimer = 0f;
            stopTimer = 0f;

            Debug.Log($"TrafficLightController: Işık durumu değişti -> {currentState}");

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Materyal renklerini ve emisyonlarini ayarla
            SetRendererEmission(redRenderer, redColor, currentState == TrafficLightState.Red || currentState == TrafficLightState.RedYellow);
            SetRendererEmission(yellowRenderer, yellowColor, currentState == TrafficLightState.RedYellow);
            SetRendererEmission(greenRenderer, greenColor, currentState == TrafficLightState.Green);
        }

        private void SetRendererEmission(Renderer rend, Color color, bool isActive)
        {
            if (rend == null) return;

            // Her degisiklikte yeni materyal ornegi olusmamasi icin MaterialPropertyBlock kullanilabilir,
            // ancak prototip kolayligi acisindan material.color ve keywordler de kullanilabilir.
            Material mat = rend.material;
            if (isActive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_BaseColor", color);
                mat.SetColor("_EmissionColor", color * emissionIntensity);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_BaseColor", color * 0.2f); // Koyu renk
                mat.SetColor("_EmissionColor", Color.black);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInZone = true;
                playerCar = other.GetComponentInParent<CarController>();
                if (playerCar == null) playerCar = other.GetComponent<CarController>();
                stopTimer = 0f;
                Debug.Log("TrafficLightController: Oyuncu bekleme alanına girdi.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInZone = false;
                playerCar = null;
                stopTimer = 0f;
                Debug.Log("TrafficLightController: Oyuncu bekleme alanından çıktı.");
            }
        }
    }
}
