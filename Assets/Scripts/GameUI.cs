using UnityEngine;
using UnityEngine.InputSystem;

public class GameUI : MonoBehaviour
{
    [SerializeField] private PuzzleManager puzzleManager;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject restartPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject nextLevelButton;

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        puzzleManager.OnLevelCompleted += ShowLevelComplete;

        if (playerHealth != null)
            playerHealth.OnDeath += ShowRestart;

        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        puzzleManager.OnLevelCompleted -= ShowLevelComplete;

        if (playerHealth != null)
            playerHealth.OnDeath -= ShowRestart;

        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void Start()
    {
        TurnOffPanels();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (puzzleManager != null && puzzleManager.IsPlayingEnding)
            return;

        if (restartPanel != null && restartPanel.activeSelf)
            return;

        if (levelCompletePanel != null && levelCompletePanel.activeSelf)
            return;

        if (pausePanel != null && pausePanel.activeSelf)
            HidePanels();
        else
            ShowPanel(pausePanel);
    }

    public void OnResumeClicked()
    {
        HidePanels();
    }

    public void OnNextClicked()
    {
        HidePanels();
        puzzleManager.NextLevel();
    }

    public void OnRestartClicked()
    {
        HidePanels();
        puzzleManager.RestartLevel();
    }

    private void ShowLevelComplete(int level)
    {
        if (nextLevelButton != null)
            nextLevelButton.SetActive(puzzleManager.HasNextLevel);

        ShowPanel(levelCompletePanel);
    }

    private void ShowRestart()
    {
        ShowPanel(restartPanel);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void TurnOffPanels()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (restartPanel != null)
            restartPanel.SetActive(false);

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    private void HidePanels()
    {
        TurnOffPanels();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}