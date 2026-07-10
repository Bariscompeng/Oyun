using UnityEngine;
using TrafikParkuru.Core;
using TrafikParkuru.Vehicle;

namespace TrafikParkuru.Stations
{
    public class CrosswalkStation : MonoBehaviour
    {
        [Header("İstasyon Sınırları")]
        [SerializeField] private float zebraZ = -50f;
        [SerializeField] private float entryZ = -62f;
        [SerializeField] private float exitZ = -42f;
        [SerializeField] private GameStage stageToComplete = GameStage.Crosswalk;
        [SerializeField] private bool useXAxis = false;

        public bool UseXAxis => useXAxis;

        private PedestrianWalker activePedestrian;
        private CarController playerCar;
        
        private bool isCompleted = false;
        private bool hasStoppedForPedestrian = false;
        private bool enteredZebraWhilePedestrianCrossing = false;
        private bool hitPedestrian = false;

        public void RegisterPedestrian(PedestrianWalker pedestrian)
        {
            activePedestrian = pedestrian;
        }

        public void OnPedestrianHit()
        {
            if (hitPedestrian) return;
            hitPedestrian = true;
            
            // Genel ceza uygula
            ScenarioManager.Instance.AddPenalty(-20, "Yaya geçidinde yayaya çarpıldı!");
            
            // Istasyonu 0 puan ile bitir
            CompleteStation(0, "Yaya geçidindeki yayaya çarptınız.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCompleted) return;

            if (other.CompareTag("Player"))
            {
                playerCar = other.GetComponentInParent<CarController>();
                if (playerCar == null) playerCar = other.GetComponent<CarController>();
                
                Debug.Log("CrosswalkStation: Oyuncu yaya geçidi bölgesine girdi.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (isCompleted) return;

            if (other.CompareTag("Player"))
            {
                // Bolgeden cikinca nihai degerlendirmeyi yap
                EvaluatePass();
            }
        }

        private void Update()
        {
            if (isCompleted || playerCar == null || hitPedestrian) return;

            float carVal = useXAxis ? playerCar.transform.position.x : playerCar.transform.position.z;
            float zebraVal = zebraZ;

            // Yaya yoldayken aracin durumunu izle
            if (activePedestrian != null && activePedestrian.IsOnCrosswalk)
            {
                // Oyuncu durma cizgisinden once durdu mu? (Zebra crossing Z: -60, durma cizgisi yaklasik Z: -63)
                bool isBeforeLine = useXAxis ? (carVal > zebraVal + 2.5f) : (carVal < zebraVal - 2.5f);
                if (isBeforeLine)
                {
                    if (playerCar.SpeedMs < 0.3f)
                    {
                        hasStoppedForPedestrian = true;
                        Debug.Log("CrosswalkStation: Oyuncu yaya için durdu, yol veriyor.");
                    }
                }
                
                // Oyuncu yaya yoldayken yaya gecidine girdi mi?
                if (carVal >= zebraVal - 2.5f && carVal <= zebraVal + 2.5f)
                {
                    if (playerCar.SpeedMs > 0.5f)
                    {
                        // Eger yaya hala yoldayken ve arac durmamisken gecide girerse kural ihlali
                        enteredZebraWhilePedestrianCrossing = true;
                        Debug.LogWarning("CrosswalkStation: Oyuncu yaya yoldayken yaya geçidine girdi!");
                    }
                }
            }
        }

        private void EvaluatePass()
        {
            if (isCompleted) return;

            int score = 20;
            string note = "Yaya geçidinden kurallara uygun şekilde geçtiniz.";

            if (hitPedestrian)
            {
                score = 0;
                note = "Yaya geçidindeki yayaya çarptınız.";
            }
            else if (enteredZebraWhilePedestrianCrossing)
            {
                score = 0;
                note = "Yaya yoldayken yaya geçidine girerek yaya önceliğini ihlal ettiniz.";
            }
            else if (activePedestrian != null && !hasStoppedForPedestrian)
            {
                // Yaya vardi ama oyuncu hic durmadi (fakat carpismadi da, yaya gecmisti ya da hizlica gecti)
                // Bu durumda yaya yoldayken tam gecide girmediyse bile tehlikeli yaklasim sayilabilir
                score = 10;
                note = "Yaya geçidine yaklaşırken hızınızı yeterince düşürmediniz/yol vermediniz.";
            }

            CompleteStation(score, note);
        }

        private void CompleteStation(int score, string note)
        {
            isCompleted = true;
            playerCar = null;
            ScenarioManager.Instance.CompleteStage(stageToComplete, score, note);
        }
    }
}
