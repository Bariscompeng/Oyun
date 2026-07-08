using UnityEngine;
using TrafikParkuru.Core;

namespace TrafikParkuru.Stations
{
    public class FinishTrigger : MonoBehaviour
    {
        private bool isCompleted = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isCompleted) return;

            if (other.CompareTag("Player"))
            {
                isCompleted = true;
                Debug.Log("FinishTrigger: Oyuncu bitiş çizgisine ulaştı.");
                ScenarioManager.Instance.CompleteStage(GameStage.Finish, 0, "Bitiş çizgisi geçildi.");
            }
        }
    }
}
