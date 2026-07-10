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
            }
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

            // 1. Kırmızı Işık
            if (activeStage == GameStage.RedLight)
            {
                SetActiveStyle(redLightText, "Kırmızı Işıkta Dur!");
            }
            else
            {
                SetCompletedStyle(redLightText, "Kırmızı Işıkta Dur!", ScenarioManager.Instance ? ScenarioManager.Instance.GetStageScore(GameStage.RedLight) : 20);
            }

            // 2. Yaya Geçitleri
            if (activeStage == GameStage.Crosswalk)
            {
                SetActiveStyle(crosswalkText, "1. Yaya Geçidinde Yol Ver!");
            }
            else if (activeStage == GameStage.Crosswalk2)
            {
                SetActiveStyle(crosswalkText, "2. Yaya Geçidinde Yol Ver!");
            }
            else if (activeStage < GameStage.Crosswalk)
            {
                SetPendingStyle(crosswalkText, "Yaya Geçitlerinde Yol Ver!");
            }
            else
            {
                int score1 = ScenarioManager.Instance ? ScenarioManager.Instance.GetStageScore(GameStage.Crosswalk) : 20;
                int score2 = ScenarioManager.Instance ? ScenarioManager.Instance.GetStageScore(GameStage.Crosswalk2) : 20;
                SetCompletedStyle(crosswalkText, "Yaya Geçitlerinde Yol Ver!", score1 + score2);
            }

            // 3. Sağa Dönüş
            if (activeStage == GameStage.Turn)
            {
                SetActiveStyle(turnText, "Kavşakta Sağa Sinyal Ver!");
            }
            else if (activeStage < GameStage.Turn)
            {
                SetPendingStyle(turnText, "Kavşakta Sağa Sinyal Ver!");
            }
            else
            {
                SetCompletedStyle(turnText, "Kavşakta Sağa Sinyal Ver!", ScenarioManager.Instance ? ScenarioManager.Instance.GetStageScore(GameStage.Turn) : 20);
            }

            // 4. Hız Sınırı
            if (activeStage == GameStage.SpeedZone)
            {
                SetActiveStyle(speedZoneText, "Hız Sınırına Uy! (Maks 25 km/s)");
            }
            else if (activeStage < GameStage.SpeedZone)
            {
                SetPendingStyle(speedZoneText, "Hız Sınırına Uy! (Maks 25 km/s)");
            }
            else
            {
                SetCompletedStyle(speedZoneText, "Hız Sınırına Uy! (Maks 25 km/s)", ScenarioManager.Instance ? ScenarioManager.Instance.GetStageScore(GameStage.SpeedZone) : 20);
            }
        }

        private void SetActiveStyle(TMP_Text txt, string text)
        {
            txt.color = activeColor;
            txt.fontStyle = FontStyles.Bold;
            txt.text = $"<color=#EAB308>&gt;</color> <b>{text}</b>";
        }

        private void SetCompletedStyle(TMP_Text txt, string text, int score)
        {
            txt.color = completedColor;
            txt.fontStyle = FontStyles.Normal;
            txt.text = $"<color=#22C55E>[v]</color> <s>{text} (+{score} Puan)</s>";
        }

        private void SetPendingStyle(TMP_Text txt, string text)
        {
            txt.color = pendingColor;
            txt.fontStyle = FontStyles.Normal;
            txt.text = $"<color=#9CA3AF>[ ]</color> {text}";
        }
    }
}
