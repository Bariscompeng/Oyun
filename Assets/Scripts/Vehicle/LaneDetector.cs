using UnityEngine;
using TrafikParkuru.Core;

namespace TrafikParkuru.Vehicle
{
    /// <summary>
    /// Oyuncunun şeritten çıkıp çıkmadığını koordinat tabanlı denetler.
    /// Şerit ihlali durumunda ceza puanı uygular.
    /// </summary>
    public class LaneDetector : MonoBehaviour
    {
        [Header("Şerit Limitleri (Toleranslı)")]
        [SerializeField] private float mainRoadMaxX = 4.85f;
        [SerializeField] private float mainRoadMinX = -0.5f; // Başlangıç X=0 ve çizgi toleransı için
        [SerializeField] private float sideRoadMaxZ = 0.5f;  // Çizgi toleransı için
        [SerializeField] private float sideRoadMinZ = -4.85f;
        
        [Header("Ceza Ayarları")]
        [SerializeField] private int penaltyPoints = -5;
        [SerializeField] private float penaltyCooldown = 3f;

        private float lastPenaltyTime = -999f;
        private bool isViolating = false;
        private int violationCount = 0;

        public int ViolationCount => violationCount;

        private void Update()
        {
            if (ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentStage == GameStage.Finish)
                return;

            Vector3 pos = transform.position;
            bool currentViolation = false;
            string reason = "";

            // Hangi yolda olduğumuzu belirle (Kavşak bölgesi Z: [-5, 5] & X <= 5 hariç)
            bool inIntersection = (pos.z >= -5f && pos.z <= 5f && pos.x <= 5f);

            if (!inIntersection)
            {
                if (pos.z < -5f)
                {
                    // Ana yol (Z < -5)
                    if (pos.x < mainRoadMinX)
                    {
                        currentViolation = true;
                        reason = "Şerit İhlali: Karşı şeride geçtiniz!";
                    }
                    else if (pos.x > mainRoadMaxX)
                    {
                        currentViolation = true;
                        reason = "Şerit İhlali: Kaldırıma/Yol dışına çıktınız!";
                    }
                }
                else if (pos.x > 5f)
                {
                    // Yan yol (X > 5)
                    if (pos.z > sideRoadMaxZ)
                    {
                        currentViolation = true;
                        reason = "Şerit İhlali: Karşı şeride geçtiniz!";
                    }
                    else if (pos.z < sideRoadMinZ)
                    {
                        currentViolation = true;
                        reason = "Şerit İhlali: Kaldırıma/Yol dışına çıktınız!";
                    }
                }
            }

            if (currentViolation)
            {
                if (!isViolating)
                {
                    isViolating = true;
                    // Cooldown kontrolü ile mükerrer cezaları engelle
                    if (Time.time - lastPenaltyTime > penaltyCooldown)
                    {
                        violationCount++;
                        lastPenaltyTime = Time.time;
                        ScenarioManager.Instance.AddPenalty(penaltyPoints, reason);
                    }
                }
            }
            else
            {
                isViolating = false;
            }
        }
    }
}
