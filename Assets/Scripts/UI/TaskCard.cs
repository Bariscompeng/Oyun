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

        private Dictionary<GameStage, TMP_Text> stageTexts;

        private void Start()
        {
            stageTexts = new Dictionary<GameStage, TMP_Text>
            {
                { GameStage.RedLight, redLightText },
                { GameStage.Crosswalk, crosswalkText },
                { GameStage.Turn, turnText },
                { GameStage.SpeedZone, speedZoneText }
            };

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
            if (stageTexts.ContainsKey(stage) && stageTexts[stage] != null)
            {
                string baseText = GetBaseTextForStage(stage);
                stageTexts[stage].text = $"<s>{baseText} (+{score} Puan)</s>";
                stageTexts[stage].color = completedColor;
            }
        }

        private void UpdateCardVisuals(GameStage activeStage)
        {
            foreach (var kvp in stageTexts)
            {
                if (kvp.Value == null) continue;

                if (kvp.Key == activeStage)
                {
                    kvp.Value.color = activeColor;
                    kvp.Value.fontStyle = FontStyles.Bold;
                    kvp.Value.text = $"> {GetBaseTextForStage(kvp.Key)}";
                }
                else if (kvp.Key < activeStage)
                {
                    // Zaten tamamlanmıs (üstü çizili durum OnStageCompleted ile yapılıyor)
                }
                else
                {
                    kvp.Value.color = pendingColor;
                    kvp.Value.fontStyle = FontStyles.Normal;
                    kvp.Value.text = GetBaseTextForStage(kvp.Key);
                }
            }
        }

        private string GetBaseTextForStage(GameStage stage)
        {
            switch (stage)
            {
                case GameStage.RedLight:
                    return "Kırmızı Işıkta Dur!";
                case GameStage.Crosswalk:
                    return "Yaya Geçidinde Yol Ver!";
                case GameStage.Turn:
                    return "Kavşakta Sağa Sinyal Ver!";
                case GameStage.SpeedZone:
                    return "Hız Sınırına Uy! (Maks 25 km/s)";
                default:
                    return "";
            }
        }
    }
}
