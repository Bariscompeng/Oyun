using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TrafikParkuru.Core;

namespace TrafikParkuru.UI
{
    public class TaskCard : MonoBehaviour
    {
        [Header("UI Elemanları")]
        [SerializeField] private TMP_Text redLightText;
        [SerializeField] private TMP_Text crosswalkText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text speedZoneText;

        [Header("Renk Ayarları")]
        [SerializeField] private Color activeColor = Color.yellow;
        [SerializeField] private Color completedColor = Color.green;
        [SerializeField] private Color pendingColor = Color.gray;

        private void Start()
        {
            // ScenarioManager olaylarına abone ol
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnStageStarted += OnStageStarted;
                ScenarioManager.Instance.OnStageCompleted += OnStageCompleted;
                ScenarioManager.Instance.OnGameFinished += OnGameFinished;
                
                // Başlangıç durumunu ayarla
                UpdateCardVisuals(ScenarioManager.Instance.CurrentStage);
            }
        }

        private void OnDestroy()
        {
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnStageStarted -= OnStageStarted;
                ScenarioManager.Instance.OnStageCompleted -= OnStageCompleted;
                ScenarioManager.Instance.OnGameFinished -= OnGameFinished;
            }
        }

        private void OnGameFinished()
        {
            UpdateCardVisuals(GameStage.Finish);
        }

        private void OnStageStarted(GameStage stage)
        {
            UpdateCardVisuals(stage);
        }

        private void OnStageCompleted(GameStage stage, int score, string note)
        {
            if (ScenarioManager.Instance != null)
            {
                UpdateCardVisuals(ScenarioManager.Instance.CurrentStage);
            }
        }

        private void UpdateCardVisuals(GameStage activeStage)
        {
            if (redLightText == null || crosswalkText == null || turnText == null || speedZoneText == null) return;
            if (ScenarioManager.Instance == null) return;

            // 1. Kırmızı Işık
            bool isRedLightCompleted = ScenarioManager.Instance.GetStageNote(GameStage.RedLight) != "Henüz başlanmadı.";
            if (isRedLightCompleted)
            {
                SetCompletedStyle(redLightText, "Kırmızı Işıkta Dur!", ScenarioManager.Instance.GetStageScore(GameStage.RedLight));
            }
            else if (activeStage == GameStage.RedLight)
            {
                SetActiveStyle(redLightText, "Kırmızı Işıkta Dur!");
            }
            else
            {
                SetPendingStyle(redLightText, "Kırmızı Işıkta Dur!");
            }

            // 2. Yaya Geçitleri
            bool isCrosswalkCompleted = ScenarioManager.Instance.GetStageNote(GameStage.Crosswalk) != "Henüz başlanmadı.";
            bool isCrosswalk2Completed = ScenarioManager.Instance.GetStageNote(GameStage.Crosswalk2) != "Henüz başlanmadı.";

            if (isCrosswalkCompleted && isCrosswalk2Completed)
            {
                int score1 = ScenarioManager.Instance.GetStageScore(GameStage.Crosswalk);
                int score2 = ScenarioManager.Instance.GetStageScore(GameStage.Crosswalk2);
                SetCompletedStyle(crosswalkText, "Yaya Geçitlerinde Yol Ver!", score1 + score2);
            }
            else if (activeStage == GameStage.Crosswalk2)
            {
                SetActiveStyle(crosswalkText, "2. Yaya Geçidinde Yol Ver!");
            }
            else if (activeStage == GameStage.Crosswalk)
            {
                SetActiveStyle(crosswalkText, "1. Yaya Geçidinde Yol Ver!");
            }
            else if (isCrosswalkCompleted)
            {
                // 1. yaya geçidi tamamlanmış ama henüz 2. yaya geçidine gelinmemiş (arada Turn ve SpeedZone var)
                int score1 = ScenarioManager.Instance.GetStageScore(GameStage.Crosswalk);
                SetCompletedStyle(crosswalkText, "1. Yaya Geçidinde Yol Ver!", score1);
            }
            else
            {
                SetPendingStyle(crosswalkText, "Yaya Geçitlerinde Yol Ver!");
            }

            // 3. Sağa Dönüş
            bool isTurnCompleted = ScenarioManager.Instance.GetStageNote(GameStage.Turn) != "Henüz başlanmadı.";
            if (isTurnCompleted)
            {
                SetCompletedStyle(turnText, "Kavşakta Sağa Sinyal Ver!", ScenarioManager.Instance.GetStageScore(GameStage.Turn));
            }
            else if (activeStage == GameStage.Turn)
            {
                SetActiveStyle(turnText, "Kavşakta Sağa Sinyal Ver!");
            }
            else
            {
                SetPendingStyle(turnText, "Kavşakta Sağa Sinyal Ver!");
            }

            // 4. Hız Sınırı
            bool isSpeedZoneCompleted = ScenarioManager.Instance.GetStageNote(GameStage.SpeedZone) != "Henüz başlanmadı.";
            if (isSpeedZoneCompleted)
            {
                SetCompletedStyle(speedZoneText, "Hız Sınırına Uy! (Maks 50 km/s)", ScenarioManager.Instance.GetStageScore(GameStage.SpeedZone));
            }
            else if (activeStage == GameStage.SpeedZone)
            {
                SetActiveStyle(speedZoneText, "Hız Sınırına Uy! (Maks 50 km/s)");
            }
            else
            {
                SetPendingStyle(speedZoneText, "Hız Sınırına Uy! (Maks 50 km/s)");
            }
        }

        private void SetActiveStyle(TMP_Text txt, string text)
        {
            txt.color = activeColor;
            txt.fontStyle = FontStyles.Bold;
            txt.text = $"<color=#FFD700>»</color> <b>{text}</b>";
        }

        private void SetCompletedStyle(TMP_Text txt, string text, int score)
        {
            txt.color = completedColor;
            txt.fontStyle = FontStyles.Normal;
            txt.text = $"<color=#00FF88>[x]</color> <color=#94A3B8><s>{text}</s> <color=#00FF88>+{score} P</color></color>";
        }

        private void SetPendingStyle(TMP_Text txt, string text)
        {
            txt.color = pendingColor;
            txt.fontStyle = FontStyles.Normal;
            txt.text = $"<color=#475569>[ ]</color> <color=#94A3B8>{text}</color>";
        }
    }
}
