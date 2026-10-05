using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MinimapPanelAnimator : MonoBehaviour
{
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private float shrinkTime = 0.6f;
    [SerializeField] private float bigScale = 1f;
    [SerializeField] private float smallScale = 0.3f;
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);
    [SerializeField] private Key growKey = Key.M;

    private RectTransform panel;
    private RectTransform screen;
    private float amount;
    private bool isMinimap;

    private void Awake()
    {
        panel = GetComponent<RectTransform>();
        screen = transform.parent as RectTransform;

        Vector2 size = panel.rect.size;
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = size;
    }

    private void OnEnable()
    {
        isMinimap = false;
        amount = 0f;
        Apply();
        StartCoroutine(StartShrink());
    }

    private IEnumerator StartShrink()
    {
        yield return new WaitForSecondsRealtime(holdTime);
        isMinimap = true;
    }

    private void Update()
    {
        if (!isMinimap) return;

        bool holding = Keyboard.current != null && Keyboard.current[growKey].isPressed;
        float target = holding ? 0f : 1f;

        amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime / shrinkTime);
        Apply();
    }

    private void Apply()
    {
        float smooth = Mathf.SmoothStep(0f, 1f, amount);

        panel.anchoredPosition = Vector2.Lerp(Vector2.zero, GetCornerPos(), smooth);
        panel.localScale = Vector3.one * Mathf.Lerp(bigScale, smallScale, smooth);
    }

    private Vector2 GetCornerPos()
    {
        Vector2 halfScreen = screen.rect.size / 2f;
        Vector2 halfPanel = panel.rect.size * smallScale / 2f;

        float x = halfScreen.x - margin.x - halfPanel.x;
        float y = -halfScreen.y + margin.y + halfPanel.y;
        return new Vector2(x, y);
    }
}
