using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrafikParkuru.Core;

namespace TrafikParkuru.UI
{
    public class ResultsScreen : MonoBehaviour
    {
        [Header("Görsel Paneller")]
        [SerializeField] private GameObject resultsPanel;

        [Header("Özet Metinleri")]
        [SerializeField] private TMP_Text totalScoreText;
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private TMP_Text timeText;

        [Header("İstasyon Döküm Metinleri")]
        [SerializeField] private TMP_Text redLightDetailText;
        [SerializeField] private TMP_Text crosswalkDetailText;
        [SerializeField] private TMP_Text turnDetailText;
        [SerializeField] private TMP_Text speedZoneDetailText;

        [Header("Skor Tablosu")]
        [SerializeField] private TMP_Text leaderboardText;

        [Header("Butonlar")]
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;

        private void Start()
        {
            if (resultsPanel != null) resultsPanel.SetActive(false);

            if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnGameFinished += ShowResults;
            }
        }

        private void OnDestroy()
        {
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnGameFinished -= ShowResults;
            }
        }

        public void ShowResults()
        {
            if (resultsPanel == null) return;

            // Zaman olcegini durdur (oyunu duraklat)
            Time.timeScale = 0f;
            
            // Farenin görünmesini sağla
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            resultsPanel.SetActive(true);

            int totalScore = ScenarioManager.Instance.GetTotalScore();
            float timeTaken = ScenarioManager.Instance.ElapsedTime;
            string rating = ScenarioManager.Instance.GetLetterRating();

            // Ozet bilgileri
            if (totalScoreText != null) totalScoreText.text = $"Toplam Puan: {totalScore} / 100";
            if (ratingText != null) ratingText.text = $"Harf Notu: {rating}";
            if (timeText != null) timeText.text = $"Süre: {timeTaken:F1} saniye";

            // Istasyon detaylari
            SetStageDetail(GameStage.RedLight, redLightDetailText, "Kırmızı Işık");
            SetStageDetail(GameStage.Crosswalk, crosswalkDetailText, "Yaya Geçidi");
            SetStageDetail(GameStage.Turn, turnDetailText, "Sağa Dönüş Sinyali");
            SetStageDetail(GameStage.SpeedZone, speedZoneDetailText, "Hız Sınırı");

            // Skor tablosunu güncelle ve göster
            SaveAndShowLeaderboard(totalScore, timeTaken);
        }

        private void SetStageDetail(GameStage stage, TMP_Text textComponent, string stageName)
        {
            if (textComponent == null) return;
            int score = ScenarioManager.Instance.GetStageScore(stage);
            string note = ScenarioManager.Instance.GetStageNote(stage);
            textComponent.text = $"<b>{stageName}:</b> {score}/25 Puan\n<size=85%>{note}</size>";
        }

        private void SaveAndShowLeaderboard(int newScore, float newTime)
        {
            // PlayerPrefs'ten mevcut skor listesini yukle
            List<ScoreEntry> scores = LoadScores();

            // Yeni skoru ekle
            scores.Add(new ScoreEntry { score = newScore, time = newTime, date = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm") });

            // Skorları sırala: En yuksek puan, esitse en kisa sure
            scores.Sort((a, b) =>
            {
                if (a.score != b.score) return b.score.CompareTo(a.score);
                return a.time.CompareTo(b.time);
            });

            // Sadece ilk 5 skoru tut
            if (scores.Count > 5)
            {
                scores.RemoveRange(5, scores.Count - 5);
            }

            // Kaydet
            SaveScores(scores);

            // UI'da göster
            if (leaderboardText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("<b>🏆 EN İYİ DERECELER (SKOR TABLOSU)</b>\n");
                for (int i = 0; i < scores.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. Puan: {scores[i].score} | Süre: {scores[i].time:F1} sn | {scores[i].date}");
                }
                leaderboardText.text = sb.ToString();
            }
        }

        [System.Serializable]
        private struct ScoreEntry
        {
            public int score;
            public float time;
            public string date;
        }

        private List<ScoreEntry> LoadScores()
        {
            List<ScoreEntry> list = new List<ScoreEntry>();
            for (int i = 0; i < 5; i++)
            {
                if (PlayerPrefs.HasKey($"Leaderboard_Score_{i}"))
                {
                    ScoreEntry entry = new ScoreEntry
                    {
                        score = PlayerPrefs.GetInt($"Leaderboard_Score_{i}"),
                        time = PlayerPrefs.GetFloat($"Leaderboard_Time_{i}"),
                        date = PlayerPrefs.GetString($"Leaderboard_Date_{i}")
                    };
                    list.Add(entry);
                }
            }
            return list;
        }

        private void SaveScores(List<ScoreEntry> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                PlayerPrefs.SetInt($"Leaderboard_Score_{i}", list[i].score);
                PlayerPrefs.SetFloat($"Leaderboard_Time_{i}", list[i].time);
                PlayerPrefs.SetString($"Leaderboard_Date_{i}", list[i].date);
            }
            PlayerPrefs.Save();
        }

        // Restart Butonu için
        public void RestartGame()
        {
            Time.timeScale = 1f;
            
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.ResetScenario();
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Çıkış Butonu için
        public void QuitGame()
        {
            Debug.Log("Application Quit");
            Application.Quit();
        }
    }
}
