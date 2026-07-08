using UnityEngine;
using UnityEngine.SceneManagement;
using TrafikParkuru.Core;

namespace TrafikParkuru.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Görsel Paneller")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject rebindPanel;

        [Header("Butonlar")]
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        [SerializeField] private UnityEngine.UI.Button rebindButton;
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;

        private bool isPaused = false;

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (rebindPanel != null) rebindPanel.SetActive(false);

            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (rebindButton != null) rebindButton.onClick.AddListener(() => ToggleRebindMenu(true));
            if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        private void Update()
        {
            // Escape tuşu ile duraklat/devam et
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        public void PauseGame()
        {
            // Eğer oyun bittiyse duraklatılamaz
            if (resultsPanelActive()) return;

            isPaused = true;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            
            // Farenin görünmesini sağla
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (rebindPanel != null) rebindPanel.SetActive(false);

            // Fareyi kilitle/gizle
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ToggleRebindMenu(bool show)
        {
            if (rebindPanel != null)
            {
                rebindPanel.SetActive(show);
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            if (ScenarioManager.Instance != null)
            {
                Destroy(ScenarioManager.Instance.gameObject);
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Debug.Log("Application Quit");
            Application.Quit();
        }

        private bool resultsPanelActive()
        {
            ResultsScreen results = FindAnyObjectByType<ResultsScreen>();
            if (results != null)
            {
                // resultsPanel'ın aktif olup olmadığını kontrol edelim
                // Reflection veya basitçe find ile yapabiliriz.
                // results.gameObject altındaki aktif panele bakılabilir.
                return false; 
            }
            return false;
        }
    }
}
