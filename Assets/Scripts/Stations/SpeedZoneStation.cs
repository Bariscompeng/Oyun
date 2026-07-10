using UnityEngine;
using TrafikParkuru.Core;
using TrafikParkuru.Vehicle;

namespace TrafikParkuru.Stations
{
    public class SpeedZoneStation : MonoBehaviour
    {
        private CarController playerCar;
        private bool isPlayerInZone = false;
        private bool isCompleted = false;
        private float maxSpeedInZoneKmh = 0f;

        private void OnTriggerEnter(Collider other)
        {
            if (isCompleted) return;

            if (other.CompareTag("Player"))
            {
                isPlayerInZone = true;
                playerCar = other.GetComponentInParent<CarController>();
                if (playerCar == null) playerCar = other.GetComponent<CarController>();
                
                maxSpeedInZoneKmh = 0f;
                Debug.Log("SpeedZoneStation: Oyuncu 25 km/s hız sınırı bölgesine girdi.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (isCompleted || !isPlayerInZone) return;

            if (other.CompareTag("Player"))
            {
                isPlayerInZone = false;
                EvaluatePass();
            }
        }

        private void Update()
        {
            if (isCompleted || !isPlayerInZone || playerCar == null) return;

            // Hızı km/s olarak ölç
            float currentSpeedKmh = playerCar.SpeedMs * 3.6f;
            if (currentSpeedKmh > maxSpeedInZoneKmh)
            {
                maxSpeedInZoneKmh = currentSpeedKmh;
            }
        }

        private void EvaluatePass()
        {
            isCompleted = true;

            int score = 0;
            string note = "";

            if (maxSpeedInZoneKmh <= 30.0f)
            {
                score = 20;
                note = $"Tebrikler! Hız sınırına uyarak bölgedeki maksimum hızınızı {maxSpeedInZoneKmh:F1} km/s'te tuttunuz.";
            }
            else if (maxSpeedInZoneKmh <= 40.0f)
            {
                score = 10;
                note = $"Hız sınırını hafif aştınız. Bölgedeki maksimum hızınız: {maxSpeedInZoneKmh:F1} km/s (Limit: 25 km/s).";
            }
            else
            {
                score = 0;
                note = $"Hız limitini ciddi şekilde ihlal ettiniz! Bölgedeki maksimum hızınız: {maxSpeedInZoneKmh:F1} km/s (Limit: 25 km/s).";
            }

            ScenarioManager.Instance.CompleteStage(GameStage.SpeedZone, score, note);
        }
    }
}
