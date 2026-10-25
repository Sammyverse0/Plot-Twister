using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class UFOUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject restartPanel;

    [Header("Win Cutscene")]
    [SerializeField] private PlayableDirector cutsceneDirector;
    [SerializeField] private VideoPlayer cutsceneVideo;
    [SerializeField] private GameObject videoScreen;
    [SerializeField] private GameObject[] hideOnWin;
    [SerializeField] private MonoBehaviour[] disableOnWin;

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    private bool isPlayingEnding;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
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
        if (playerHealth != null)
            playerHealth.OnDeath -= ShowRestart;

        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }

        if (cutsceneDirector != null)
            cutsceneDirector.stopped -= OnDirectorStopped;

        if (cutsceneVideo != null)
            cutsceneVideo.loopPointReached -= OnVideoFinished;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        TurnOffPanels();

        if (videoScreen != null)
            videoScreen.SetActive(false);
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        if (isPlayingEnding)
            return;

        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (restartPanel != null && restartPanel.activeSelf)
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

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuScene);
    }

    private void ShowRestart()
    {
        if (isPlayingEnding)
            return;

        if (playerHealth != null && !playerHealth.IsDead)
            return;

        ShowPanel(restartPanel);
    }

    public void PlayWinCutscene()
    {
        if (isPlayingEnding)
            return;

        if (playerHealth != null && playerHealth.IsDead)
            return;

        isPlayingEnding = true;

        TurnOffPanels();

        foreach (GameObject obj in hideOnWin)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        foreach (MonoBehaviour script in disableOnWin)
        {
            if (script != null)
                script.enabled = false;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cutsceneDirector != null)
        {
            cutsceneDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            cutsceneDirector.stopped += OnDirectorStopped;
            cutsceneDirector.Play();
        }
        else if (cutsceneVideo != null)
        {
            if (videoScreen != null)
                videoScreen.SetActive(true);

            cutsceneVideo.isLooping = false;
            cutsceneVideo.loopPointReached += OnVideoFinished;
            cutsceneVideo.Play();
        }
        else
        {
            MainMenu();
        }
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        MainMenu();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        MainMenu();
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
    }

    private void HidePanels()
    {
        TurnOffPanels();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}