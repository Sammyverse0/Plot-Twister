using UnityEngine;


public class ScreenPuzzleUI : MonoBehaviour
{
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Plot[] plots;
    [SerializeField] private RectTransform[] tileImages;

    public void ToggleUI()
    {
        Time.timeScale = uiPanel.activeSelf ? 1f : 0f;
        uiPanel.SetActive(!uiPanel.activeSelf);
        Cursor.lockState = uiPanel.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiPanel.activeSelf;
    }

    private void Update()
    {
        if (!uiPanel.activeSelf) return;

        for (int i = 0; i < plots.Length; i++)
        {
            tileImages[i].localRotation = Quaternion.Euler(0f, 0f, -plots[i].rotationState * 90f);
        }
    }
}