using UnityEngine;

public class PressableButton : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color pressedColor = Color.green;
    [SerializeField] private AudioSource pressSound;
    [SerializeField] private string promptMessage = "Press E to fix the wiring";

    private ObjectiveManager manager;

    public bool IsPressed { get; private set; }
    public string PromptMessage => promptMessage;

    private void Start()
    {
        manager = FindFirstObjectByType<ObjectiveManager>();
    }

    public void Press()
    {
        if (IsPressed) return;
        IsPressed = true;

        if (targetRenderer != null)
        {
            targetRenderer.material.color = pressedColor;
        }

        if (pressSound != null)
        {
            pressSound.Play();
        }

        manager.RegisterPress();
    }
}