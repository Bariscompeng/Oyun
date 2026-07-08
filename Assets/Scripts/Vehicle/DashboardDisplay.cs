using UnityEngine;
using TMPro;
using TrafikParkuru.Vehicle;
using TrafikParkuru.Core;

namespace TrafikParkuru.Vehicle
{
    public class DashboardDisplay : MonoBehaviour
    {
        [Header("Referanslar")]
        [SerializeField] private CarController car;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text signalText; // Örn: "<   >"
        [SerializeField] private TMP_Text gearText;   // Örn: "D"

        private SignalController signalController;
        private Rigidbody carRb;

        private void Start()
        {
            if (car == null) car = GetComponentInParent<CarController>();
            if (car != null)
            {
                signalController = car.GetComponent<SignalController>();
                carRb = car.GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            if (car == null) return;

            if (speedText != null)
            {
                speedText.text = $"{Mathf.RoundToInt(car.SpeedKmh)}";
            }

            if (gearText != null && carRb != null)
            {
                float speedKmh = car.SpeedKmh;
                if (speedKmh < 0.2f)
                {
                    gearText.text = "P";
                }
                else
                {
                    float dot = Vector3.Dot(car.transform.forward, carRb.linearVelocity);
                    if (dot < -0.1f)
                    {
                        gearText.text = "R";
                    }
                    else
                    {
                        gearText.text = "D";
                    }
                }
            }

            if (signalText != null && signalController != null)
            {
                SignalState state = signalController.ActiveSignal;
                bool flashState = (Time.time % 0.6f) < 0.3f;
                if (state == SignalState.Left && flashState)
                {
                    signalText.text = "<color=green>&lt;</color>  <color=#222222>&gt;</color>";
                }
                else if (state == SignalState.Right && flashState)
                {
                    signalText.text = "<color=#222222>&lt;</color>  <color=green>&gt;</color>";
                }
                else
                {
                    signalText.text = "<color=#222222>&lt;  &gt;</color>";
                }
            }
        }
    }
}
