using System.Collections;
using TMPro;
using UnityEngine;
using TrafikParkuru.Core;

namespace TrafikParkuru.UI
{
    public class ToastMessage : MonoBehaviour
    {
        [Header("UI Elemanları")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TMP_Text toastText;

        [Header("Renk Ayarları")]
        [SerializeField] private Color successColor = Color.green;
        [SerializeField] private Color penaltyColor = Color.red;

        [Header("Ayarlar")]
        [SerializeField] private float displayDuration = 3.5f;

        private Coroutine activeCoroutine;

        private void Start()
        {
            if (toastPanel != null) toastPanel.SetActive(false);

            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnStageCompleted += OnStageCompleted;
                ScenarioManager.Instance.OnPenaltyAdded += OnPenaltyAdded;
            }
        }

        private void OnDestroy()
        {
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnStageCompleted -= OnStageCompleted;
                ScenarioManager.Instance.OnPenaltyAdded -= OnPenaltyAdded;
            }
        }

        private void OnStageCompleted(GameStage stage, int score, string note)
        {
            if (stage == GameStage.Finish) return;

            string msg = $"İSTASYON TAMAMLANDI\nPuan: +{score}\n{note}";
            ShowToast(msg, successColor);
        }

        private void OnPenaltyAdded(PenaltyData penalty)
        {
            string msg = $"KURAL İHLALİ!\nCeza: {penalty.amount} Puan\nNeden: {penalty.reason}";
            ShowToast(msg, penaltyColor);
        }

        public void ShowToast(string message, Color color)
        {
            if (toastText == null || toastPanel == null) return;

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            activeCoroutine = StartCoroutine(ToastSequence(message, color));
        }

        private IEnumerator ToastSequence(string message, Color color)
        {
            toastText.text = message;
            toastText.color = color;
            toastPanel.SetActive(true);

            yield return new WaitForSeconds(displayDuration);

            toastPanel.SetActive(false);
            activeCoroutine = null;
        }
    }
}
