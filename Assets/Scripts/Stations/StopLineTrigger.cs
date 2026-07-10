using UnityEngine;
using TrafikParkuru.Core;

namespace TrafikParkuru.Stations
{
    public class StopLineTrigger : MonoBehaviour
    {
        [Header("Bağlı Trafik Işığı")]
        [SerializeField] private TrafficLightController lightController;

        private bool isCompleted = false;

        private void Awake()
        {
            if (lightController == null)
            {
                lightController = FindAnyObjectByType<TrafficLightController>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCompleted) return;

            if (other.CompareTag("Player"))
            {
                isCompleted = true;
                EvaluatePass();
            }
        }

        private void EvaluatePass()
        {
            if (lightController == null)
            {
                Debug.LogError("StopLineTrigger: TrafficLightController bulunamadı!", this);
                ScenarioManager.Instance.CompleteStage(GameStage.RedLight, 0, "Hata: Trafik ışığı kontrolcüsü bulunamadı.");
                return;
            }

            int score = 0;
            string note = "";

            switch (lightController.CurrentState)
            {
                case TrafficLightState.Green:
                    score = 20;
                    note = "Kurallara uygun şekilde bekleyip yeşil ışıkta geçtiniz.";
                    break;
                case TrafficLightState.RedYellow:
                    score = 10;
                    note = "Kırmızı ve sarı birlikte yanarken (yeşili tam beklemeden) geçtiniz.";
                    break;
                case TrafficLightState.Red:
                    score = 0;
                    note = "Kırmızı ışıkta durmayarak ihlal gerçekleştirdiniz.";
                    break;
            }

            // ScenarioManager'a durumu raporla
            ScenarioManager.Instance.CompleteStage(GameStage.RedLight, score, note);
        }
    }
}
