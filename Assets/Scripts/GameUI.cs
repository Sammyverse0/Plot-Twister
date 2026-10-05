using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private PuzzleManager puzzleManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject restartPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject nextLevelButton;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        puzzleManager.OnLevelCompleted += ShowLevelComplete;
        if (playerHealth != null) playerHealth.OnDeath += ShowRestart;
    }

    private void OnDisable()
    {
        puzzleManager.OnLevelCompleted -= ShowLevelComplete;
        if (playerHealth != null) playerHealth.OnDeath -= ShowRestart;
    }

    private void Start()
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
        if (nextLevelButton != null) nextLevelButton.SetActive(puzzleManager.HasNextLevel);
        ShowPanel(levelCompletePanel);
    }

    private void ShowRestart()
    {
        ShowPanel(restartPanel);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HidePanels()
    {
        if (restartPanel != null) restartPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
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