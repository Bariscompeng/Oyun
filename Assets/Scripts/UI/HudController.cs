using TMPro;
using UnityEngine;
using TrafikParkuru.Vehicle;
using TrafikParkuru.Core;

/// Ekranın altında km/s göstergesi ve sinyal okları, ayrıca sağda puan ve süre göstergeleri.
public class HudController : MonoBehaviour
{
    [SerializeField] private CarController car;
    [SerializeField] private TMP_Text speedText;
    
    [Header("Sinyal Göstergeleri")]
    [SerializeField] private TMP_Text leftArrowText;
    [SerializeField] private TMP_Text rightArrowText;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.2f);

    [Header("Dashboard Göstergeleri")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;

    private SignalController signalController;

    private void Awake()
    {
        if (car == null) car = FindAnyObjectByType<CarController>();
        if (speedText == null) speedText = transform.Find("SpeedText")?.GetComponent<TMP_Text>();
        if (scoreText == null) scoreText = transform.Find("DashboardCard/ScoreText")?.GetComponent<TMP_Text>();
        if (timeText == null) timeText = transform.Find("DashboardCard/TimeText")?.GetComponent<TMP_Text>();

        if (car != null)
        {
            signalController = car.GetComponent<SignalController>();
        }
    }

    private void Update()
    {
        if (car == null) return;

        if (speedText != null)
        {
            speedText.text = $"{Mathf.RoundToInt(car.SpeedKmh)} km/s";
        }

        if (scoreText != null && ScenarioManager.Instance != null)
        {
            scoreText.text = $"Puan: {ScenarioManager.Instance.GetTotalScore()} / 100";
        }

        if (timeText != null && ScenarioManager.Instance != null)
        {
            timeText.text = $"Süre: {ScenarioManager.Instance.ElapsedTime:F1} sn";
        }

        if (signalController == null)
        {
            signalController = car.GetComponent<SignalController>();
        }

        UpdateSignals();
    }

    private void UpdateSignals()
    {
        if (signalController == null) return;

        SignalState state = signalController.ActiveSignal;
        bool flashState = (Time.time % 0.6f) < 0.3f; // 0.6 saniye periyotlu flaşör

        if (leftArrowText != null)
        {
            leftArrowText.color = (state == SignalState.Left && flashState) ? activeColor : inactiveColor;
        }

        if (rightArrowText != null)
        {
            rightArrowText.color = (state == SignalState.Right && flashState) ? activeColor : inactiveColor;
        }
    }

    public void SetFpsView(bool isFps)
    {
        if (speedText != null) speedText.gameObject.SetActive(!isFps);
        if (leftArrowText != null) leftArrowText.gameObject.SetActive(!isFps);
        if (rightArrowText != null) rightArrowText.gameObject.SetActive(!isFps);
    }
}

