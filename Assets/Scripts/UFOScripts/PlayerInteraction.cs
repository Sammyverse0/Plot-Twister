using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private ObjectiveManager objectiveManager;

    [Header("Settings")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;

    private PressableButton currentButton;

    private bool Locked => objectiveManager != null && objectiveManager.InteractionLocked;

    private void OnEnable()
    {
        interactAction.action.Enable();
        interactAction.action.performed += OnInteract;
    }

    private void OnDisable()
    {
        interactAction.action.performed -= OnInteract;
    }

    private void Start()
    {
        if (objectiveManager == null)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>();

        HidePrompt();
    }

    private void Update()
    {
        PressableButton found = null;

        if (!Locked)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
            {
                found = hit.collider.GetComponentInParent<PressableButton>();
            }

            if (found != null && found.IsPressed)
            {
                found = null;
            }
        }

        if (found == currentButton) return;

        currentButton = found;

        if (currentButton != null)
        {
            ShowPrompt(currentButton.PromptMessage);
        }
        else
        {
            HidePrompt();
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (Locked || currentButton == null) return;

        currentButton.Press();
        currentButton = null;
        HidePrompt();
    }

    private void ShowPrompt(string message)
    {
        promptText.text = message;
        promptText.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        promptText.gameObject.SetActive(false);
    }
}