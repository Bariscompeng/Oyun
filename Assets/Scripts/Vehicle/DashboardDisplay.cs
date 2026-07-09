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
        [SerializeField] private TMP_Text signalText; // Örn: "««    »»"
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

                // Disable the default orange emissive gauges on the model's dashboard mesh
                Transform visual = car.transform.Find("RealCarVisual");
                if (visual != null)
                {
                    Transform dashMesh = visual.Find("InteriorSteeringDash");
                    if (dashMesh != null)
                    {
                        var mr = dashMesh.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            // Create an instance-specific material at runtime to keep source assets clean
                            Material mat = mr.material;
                            if (mat.HasProperty("emissiveFactor"))
                            {
                                mat.SetColor("emissiveFactor", Color.black);
                            }
                            if (mat.HasProperty("emissiveTexture"))
                            {
                                mat.SetTexture("emissiveTexture", null);
                            }
                        }
                    }
                }
            }

            // Apply modern styling defaults
            if (speedText != null)
            {
                speedText.alignment = TextAlignmentOptions.Center;
            }
            if (gearText != null)
            {
                gearText.alignment = TextAlignmentOptions.Center;
            }
            if (signalText != null)
            {
                signalText.alignment = TextAlignmentOptions.Center;
            }
        }

        private void Update()
        {
            if (car == null) return;

            if (speedText != null)
            {
                // Large bold speed value with a smaller grey km/h unit label below it
                speedText.text = $"<font-weight=bold>{Mathf.RoundToInt(car.SpeedKmh)}</font-weight>\n<size=40%><color=#aaaaaa>km/h</color></size>";
            }

            if (gearText != null && carRb != null)
            {
                float speedKmh = car.SpeedKmh;
                string gearLetter = "P";
                if (speedKmh >= 0.2f)
                {
                    float dot = Vector3.Dot(car.transform.forward, carRb.linearVelocity);
                    gearLetter = (dot < -0.1f) ? "R" : "D";
                }

                // Dynamic coloring for the gears
                string gearColor = "#ffffff";
                if (gearLetter == "P") gearColor = "#ff4444"; // Red for Park
                else if (gearLetter == "R") gearColor = "#ffaa00"; // Amber for Reverse
                else if (gearLetter == "D") gearColor = "#44ff44"; // Green for Drive

                gearText.text = $"<color={gearColor}>{gearLetter}</color>";
            }

            if (signalText != null && signalController != null)
            {
                SignalState state = signalController.ActiveSignal;
                bool flashState = (Time.time % 0.6f) < 0.3f;
                
                // Modern double guillemet arrowheads («« and »»)
                // Active indicators blink bright neon green, inactive are faint green-grey
                if (state == SignalState.Left && flashState)
                {
                    signalText.text = "<color=#10ff50>««</color>    <color=#152515>»»</color>";
                }
                else if (state == SignalState.Right && flashState)
                {
                    signalText.text = "<color=#152515>««</color>    <color=#10ff50>»»</color>";
                }
                else
                {
                    signalText.text = "<color=#152515>««    »»</color>";
                }
            }
        }
    }
}
