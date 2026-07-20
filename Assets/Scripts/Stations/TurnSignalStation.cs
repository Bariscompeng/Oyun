using UnityEngine;
using TrafikParkuru.Core;
using TrafikParkuru.Vehicle;

namespace TrafikParkuru.Stations
{
    public class TurnSignalStation : MonoBehaviour
    {
        private SignalController playerSignals;
        private bool isPlayerInZone = false;
        private bool isCompleted = false;
        private float rightSignalAccumulatedTime = 0f;

        private void OnTriggerEnter(Collider other)
        {
            if (isCompleted) return;

            if (other.CompareTag("Player"))
            {
                isPlayerInZone = true;
                playerSignals = other.GetComponentInParent<SignalController>();
                if (playerSignals == null) playerSignals = other.GetComponent<SignalController>();
                
                rightSignalAccumulatedTime = 0f;
                Debug.Log("TurnSignalStation: Oyuncu sağa dönüş sinyali yaklaşma alanına girdi.");
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
            if (isCompleted || !isPlayerInZone || playerSignals == null) return;

            // Sag sinyal acik mi?
            if (playerSignals.ActiveSignal == SignalState.Right)
            {
                // Sinyalin acik oldugu sureyi biriktir
                rightSignalAccumulatedTime += Time.deltaTime;
            }

            // Dönüşün tamamlanıp tamamlanmadığını kontrol et (Z yönünden Doğuya/X yönüne dönüş, açı ~90 derece)
            float angle = playerSignals.transform.eulerAngles.y;
            if (angle < 0) angle += 360f;

            if (angle > 70f && angle < 110f && playerSignals.transform.position.x > 3.0f)
            {
                EvaluatePass();
            }
        }

        private void EvaluatePass()
        {
            isCompleted = true;

            int score = 0;
            string note = "";

            if (rightSignalAccumulatedTime >= 1.0f)
            {
                score = 20;
                note = "Kavşağa yaklaşırken sağ sinyalinizi kurallara uygun şekilde (en az 1 sn) açık tuttunuz.";
            }
            else if (rightSignalAccumulatedTime > 0.05f) // Tolerans payı
            {
                score = 10;
                note = "Sağ sinyal verdiniz fakat kavşaktan önce yeterli süre (1 sn'den az) açık tutulmadı.";
            }
            else
            {
                score = 0;
                note = "Kavşaktan sağa dönerken sağ sinyal vermediniz.";
            }

            ScenarioManager.Instance.CompleteStage(GameStage.Turn, score, note);
        }
    }
}
