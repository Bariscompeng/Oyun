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
                Debug.Log("SpeedZoneStation: Oyuncu 50 km/s hız sınırı bölgesine girdi.");
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
            float currentSpeedKmh = playerCar.SpeedKmh; // use SpeedKmh directly or playerCar.SpeedMs * 3.6f
            if (currentSpeedKmh > maxSpeedInZoneKmh)
            {
                maxSpeedInZoneKmh = currentSpeedKmh;
            }

            // Hız sınır bölgesinin çıkışını kontrol et (Z >= 35.0f)
            if (playerCar.transform.position.z >= 35.0f)
            {
                EvaluatePass();
            }
        }

        private void EvaluatePass()
        {
            isCompleted = true;

            int score = 0;
            string note = "";

            if (maxSpeedInZoneKmh <= 55.0f)
            {
                score = 20;
                note = $"Tebrikler! Hız sınırına uyarak bölgedeki maksimum hızınızı {maxSpeedInZoneKmh:F1} km/s'te tuttunuz.";
            }
            else if (maxSpeedInZoneKmh <= 65.0f)
            {
                score = 10;
                note = $"Hız sınırını hafif aştınız. Bölgedeki maksimum hızınız: {maxSpeedInZoneKmh:F1} km/s (Limit: 50 km/s).";
            }
            else
            {
                score = 0;
                note = $"Hız limitini ciddi şekilde ihlal ettiniz! Bölgedeki maksimum hızınız: {maxSpeedInZoneKmh:F1} km/s (Limit: 50 km/s).";
            }

            ScenarioManager.Instance.CompleteStage(GameStage.SpeedZone, score, note);
        }
    }
}
