using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the Player. Looking at a Plot and pressing Interact twists it.
// Looking at the Screen object and pressing Interact toggles the puzzle UI.
public class PlotInteraction : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float range = 5f;
    [SerializeField] private LayerMask interactMask;
    [SerializeField] private InputActionReference interactAction;

    private void OnEnable() => interactAction.action.performed += OnInteract;
    private void OnDisable() => interactAction.action.performed -= OnInteract;

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, range, interactMask))
            return;

        Plot plot = hit.collider.GetComponentInParent<Plot>();
        if (plot != null)
        {
            plot.Twist();
            return;
        }

        ScreenPuzzleUI screen = hit.collider.GetComponentInParent<ScreenPuzzleUI>();
        if (screen != null)
        {
            screen.ToggleUI();
        }
    }
}