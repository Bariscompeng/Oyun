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

            // 1. Segment 1: Ana Yol (Z < -8 ve X < 10)
            if (pos.z < -8f && pos.x < 10f)
            {
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
            // 2. Segment 2: Yan Yol 1 (X > 9 ve X < 45 ve Z < 10)
            else if (pos.x > 9f && pos.x < 45f && pos.z < 10f)
            {
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
            // 3. Segment 3: Hız Sınırı Yan Yol 2 (Z > 5 ve Z < 25 ve X > 45)
            else if (pos.z > 5f && pos.z < 25f && pos.x > 45f)
            {
                // Sağ şerit (Kuzey yönü): X [49.5, 54.85]
                if (pos.x < 49.5f)
                {
                    currentViolation = true;
                    reason = "Şerit İhlali: Karşı şeride geçtiniz!";
                }
                else if (pos.x > 54.85f)
                {
                    currentViolation = true;
                    reason = "Şerit İhlali: Kaldırıma/Yol dışına çıktınız!";
                }
            }
            // 4. Segment 4: 2. Yaya ve Bitiş Yan Yol 3 (Z > 25 ve X < 45)
            else if (pos.z > 25f && pos.x < 45f)
            {
                // Sağ şerit (Batı yönü): Z [29.5, 34.85]
                if (pos.z < 29.5f)
                {
                    currentViolation = true;
                    reason = "Şerit İhlali: Karşı şeride geçtiniz!";
                }
                else if (pos.z > 34.85f)
                {
                    currentViolation = true;
                    reason = "Şerit İhlali: Kaldırıma/Yol dışına çıktınız!";
                }
            }
            // Diğer tüm durumlar kavşak/dönüş geçiş bölgeleridir (Junction 1, 2, 3), ceza uygulanmaz.

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
