using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafikParkuru.Core
{
    public enum GameStage
    {
        RedLight,
        Crosswalk,
        Crosswalk2,
        Turn,
        SpeedZone,
        Finish
    }

    [System.Serializable]
    public struct PenaltyData
    {
        public int amount;
        public string reason;
    }

    public class ScenarioManager : MonoBehaviour
    {
        public static ScenarioManager Instance { get; private set; }

        [Header("Parkur Durumu")]
        [SerializeField] private GameStage currentStage = GameStage.RedLight;
        
        private Dictionary<GameStage, int> stageScores = new Dictionary<GameStage, int>();
        private Dictionary<GameStage, string> stageNotes = new Dictionary<GameStage, string>();
        private List<PenaltyData> penalties = new List<PenaltyData>();
        
        private float startTime;
        private float endTime;
        private bool isGameFinished = false;

        // Olaylar (Events)
        public event Action<GameStage> OnStageStarted;
        public event Action<GameStage, int, string> OnStageCompleted;
        public event Action<int> OnScoreChanged;
        public event Action OnGameFinished;
        public event Action<PenaltyData> OnPenaltyAdded;

        public GameStage CurrentStage => currentStage;
        public float ElapsedTime => isGameFinished ? (endTime - startTime) : (Time.time - startTime);
        public List<PenaltyData> Penalties => penalties;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Skorlari sifirla
            foreach (GameStage stage in Enum.GetValues(typeof(GameStage)))
            {
                if (stage != GameStage.Finish)
                {
                    stageScores[stage] = 0;
                    stageNotes[stage] = "Henüz başlanmadı.";
                }
            }
        }

        private void Start()
        {
            startTime = Time.time;
            OnStageStarted?.Invoke(currentStage);
        }

        public void ResetScenario()
        {
            currentStage = GameStage.RedLight;
            stageScores.Clear();
            stageNotes.Clear();
            penalties.Clear();

            foreach (GameStage stage in Enum.GetValues(typeof(GameStage)))
            {
                if (stage != GameStage.Finish)
                {
                    stageScores[stage] = 0;
                    stageNotes[stage] = "Henüz başlanmadı.";
                }
            }

            startTime = Time.time;
            isGameFinished = false;
        }

        public void CompleteStage(GameStage stage, int score, string note)
        {
            if (stage != currentStage)
            {
                Debug.LogWarning($"ScenarioManager: Mevcut olmayan bir istasyon ({stage}) tamamlanmaya çalışıldı. Beklenen: {currentStage}");
                return;
            }

            stageScores[stage] = score;
            stageNotes[stage] = note;
            
            Debug.Log($"ScenarioManager: İstasyon Tamamlandı: {stage} - Puan: {score} - Not: {note}");
            
            OnStageCompleted?.Invoke(stage, score, note);
            OnScoreChanged?.Invoke(GetTotalScore());

            // Bir sonraki istasyona gec
            if (currentStage < GameStage.Finish)
            {
                currentStage++;
                OnStageStarted?.Invoke(currentStage);
            }
            else if (currentStage == GameStage.Finish)
            {
                FinishGame();
            }
        }

        public void AddPenalty(int amount, string reason)
        {
            var p = new PenaltyData { amount = amount, reason = reason };
            penalties.Add(p);
            Debug.LogWarning($"ScenarioManager: Ceza Alındı! Miktar: {amount} - Neden: {reason}");
            OnPenaltyAdded?.Invoke(p);
            OnScoreChanged?.Invoke(GetTotalScore());
        }

        public int GetStageScore(GameStage stage)
        {
            return stageScores.ContainsKey(stage) ? stageScores[stage] : 0;
        }

        public string GetStageNote(GameStage stage)
        {
            return stageNotes.ContainsKey(stage) ? stageNotes[stage] : "";
        }

        public int GetTotalScore()
        {
            int total = 0;
            foreach (var score in stageScores.Values)
            {
                total += score;
            }
            
            foreach (var penalty in penalties)
            {
                total += penalty.amount; // penalty.amount negatif veya pozitif (bonus) deger olabilir
            }

            return Mathf.Clamp(total, 0, 100); // 0-100 arasında sınırla
        }

        public string GetLetterRating()
        {
            int score = GetTotalScore();
            if (score >= 90) return "A";
            if (score >= 80) return "B";
            if (score >= 70) return "C";
            if (score >= 50) return "D";
            return "F";
        }

        private void FinishGame()
        {
            // Temiz sürüş bonusu kontrolü
            var laneDetector = FindAnyObjectByType<TrafikParkuru.Vehicle.LaneDetector>();
            if (laneDetector != null && laneDetector.ViolationCount == 0)
            {
                AddPenalty(10, "Temiz Sürüş Bonusu");
            }

            isGameFinished = true;
            endTime = Time.time;
            Debug.Log($"ScenarioManager: Sınav tamamlandı! Toplam Skor: {GetTotalScore()} - Süre: {ElapsedTime:F1} sn");
            OnGameFinished?.Invoke();
        }
    }
}
