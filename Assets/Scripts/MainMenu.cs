using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    private const string CutsceneSeenKey = "CutsceneSeen";

    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject newGameButton;
    [SerializeField] private VideoPlayer cutscenePlayer;
    [SerializeField] private string gameSceneName = "SampleScene";

    private bool _cutscenePlaying;
    private float _cutsceneStartTime;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (newGameButton != null)
            newGameButton.SetActive(PlayerPrefs.GetInt(PuzzleManager.SaveKey, 0) > 0);

        if (cutscenePlayer != null)
        {
            cutscenePlayer.playOnAwake = false;
            cutscenePlayer.loopPointReached += _ => FinishCutscene();
        }

        ShowMain();
    }

    private void Update()
    {
        if (!_cutscenePlaying || Time.time - _cutsceneStartTime < 0.5f) return;

        bool pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (pressed) FinishCutscene();
    }

    public void StartGame()
    {
        bool seen = PlayerPrefs.GetInt(CutsceneSeenKey, 0) == 1
            || PlayerPrefs.GetInt(PuzzleManager.SaveKey, 0) > 0;

        if (seen) SceneManager.LoadScene(gameSceneName);
        else PlayCutscene();
    }

    public void NewGame()
    {
        PlayerPrefs.DeleteKey(PuzzleManager.SaveKey);
        PlayerPrefs.DeleteKey(CutsceneSeenKey);
        PlayCutscene();
    }

    private void PlayCutscene()
    {
        if (cutscenePlayer == null)
        {
            FinishCutscene();
            return;
        }

        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        Cursor.visible = false;

        _cutscenePlaying = true;
        _cutsceneStartTime = Time.time;
        cutscenePlayer.Play();
    }

    private void FinishCutscene()
    {
        _cutscenePlaying = false;
        PlayerPrefs.SetInt(CutsceneSeenKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ShowMain()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}