using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private Gun gun;

    [Header("Lines")]
    [SerializeField] private RectTransform top;
    [SerializeField] private RectTransform bottom;
    [SerializeField] private RectTransform left;
    [SerializeField] private RectTransform right;
    [SerializeField] private RectTransform dot;

    [Header("Look")]
    [SerializeField] private float lineLength = 8f;
    [SerializeField] private float thickness = 3f;
    [SerializeField] private float minGap = 5f;
    [SerializeField] private float dotSize = 3f;
    [SerializeField] private Color color = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private bool showDot = false;

    private RectTransform canvasRect;
    private Canvas canvas;
    private Camera cam;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = canvas.GetComponent<RectTransform>();
        cam = Camera.main;

        SetupLine(top, new Vector2(thickness, lineLength));
        SetupLine(bottom, new Vector2(thickness, lineLength));
        SetupLine(left, new Vector2(lineLength, thickness));
        SetupLine(right, new Vector2(lineLength, thickness));

        if (dot != null)
        {
            SetupLine(dot, new Vector2(dotSize, dotSize));
            dot.anchoredPosition = Vector2.zero;
            dot.gameObject.SetActive(showDot);
        }
    }

    private void SetupLine(RectTransform line, Vector2 size)
    {
        if (line == null) return;

        line.anchorMin = new Vector2(0.5f, 0.5f);
        line.anchorMax = new Vector2(0.5f, 0.5f);
        line.pivot = new Vector2(0.5f, 0.5f);
        line.sizeDelta = size;

        if (line.TryGetComponent(out Image image))
            image.color = color;
    }

    private void LateUpdate()
    {
        float thick = Mathf.Max(thickness, 1.5f / Mathf.Max(canvas.scaleFactor, 0.01f));
        if (top != null) top.sizeDelta = new Vector2(thick, lineLength);
        if (bottom != null) bottom.sizeDelta = new Vector2(thick, lineLength);
        if (left != null) left.sizeDelta = new Vector2(lineLength, thick);
        if (right != null) right.sizeDelta = new Vector2(lineLength, thick);

        float gap = minGap + SpreadToPixels();
        float offset = gap + lineLength / 2f;

        if (top != null) top.anchoredPosition = new Vector2(0f, offset);
        if (bottom != null) bottom.anchoredPosition = new Vector2(0f, -offset);
        if (left != null) left.anchoredPosition = new Vector2(-offset, 0f);
        if (right != null) right.anchoredPosition = new Vector2(offset, 0f);
    }

    private float SpreadToPixels()
    {
        if (gun == null || cam == null) return 0f;

        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float spread = gun.CurrentSpread * Mathf.Deg2Rad;

        return Mathf.Tan(spread) / Mathf.Tan(halfFov) * (canvasRect.rect.height / 2f);
    }
}
