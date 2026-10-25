using UnityEngine;

public class BlinkingMarker : MonoBehaviour
{
    [SerializeField] private float normalAlpha = 0.4f;
    [SerializeField] private float blinkTime = 1f;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int OldColor = Shader.PropertyToID("_Color");

    private Renderer[] parts;
    private Color[] colors;
    private MaterialPropertyBlock block;
    private float timer;

    private void Awake()
    {
        parts = GetComponentsInChildren<Renderer>();
        colors = new Color[parts.Length];
        block = new MaterialPropertyBlock();

        for (int i = 0; i < parts.Length; i++)
        {
            Material mat = parts[i].sharedMaterial;
            if (mat == null) colors[i] = Color.white;
            else if (mat.HasProperty(BaseColor)) colors[i] = mat.GetColor(BaseColor);
            else if (mat.HasProperty(OldColor)) colors[i] = mat.GetColor(OldColor);
            else colors[i] = Color.white;
        }

        SetAlpha(normalAlpha);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float wave = Mathf.Cos(timer / blinkTime * Mathf.PI * 2f) * 0.5f + 0.5f;
        SetAlpha(normalAlpha * wave);
    }

    private void SetAlpha(float alpha)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            Color color = colors[i];
            color.a = alpha;

            parts[i].GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            block.SetColor(OldColor, color);
            parts[i].SetPropertyBlock(block);
        }
    }
}
