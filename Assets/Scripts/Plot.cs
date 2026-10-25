using System.Collections;
using UnityEngine;

public enum PipeShape { Straight, Elbow }
public class Plot : MonoBehaviour
{
    public PipeShape shape;
    [HideInInspector] public int rotationState;
    [HideInInspector] public PuzzleManager manager;
    [SerializeField] private float worldYawOffset = 0f;

    [Header("Soil")]
    [SerializeField] private Renderer[] soilRenderers;
    [ColorUsage(false, true)]
    [SerializeField] private Color dryTint = new Color(1.7f, 1.45f, 1.15f);
    [SerializeField] private float wetTime = 1f;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private Color[] wetColors;
    private MaterialPropertyBlock block;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (soilRenderers != null)
            soilRenderers = System.Array.FindAll(soilRenderers, r => r != null);

        if (soilRenderers == null || soilRenderers.Length == 0)
            soilRenderers = GetComponentsInChildren<Renderer>();
        wetColors = new Color[soilRenderers.Length];

        for (int i = 0; i < soilRenderers.Length; i++)
        {
            Material mat = soilRenderers[i].sharedMaterial;
            wetColors[i] = mat != null && mat.HasProperty(BaseColor) ? mat.GetColor(BaseColor) : Color.white;
        }

        SetSoil(0f);
    }

    public void Twist()
    {
        rotationState = (rotationState + 1) % 4;
        ApplyVisualRotation();
        manager?.OnPlotTwisted();
    }


    public void SetInitialRotation(int state)
    {
        rotationState = state;
        ApplyVisualRotation();
    }

    private void ApplyVisualRotation()
    {
        transform.localRotation = Quaternion.Euler(0f, rotationState * 90f + worldYawOffset, 0f);
    }

    public void Water(float delay)
    {
        StartCoroutine(WaterRoutine(delay));
    }

    private IEnumerator WaterRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        float time = 0f;
        while (time < wetTime)
        {
            time += Time.deltaTime;
            SetSoil(time / wetTime);
            yield return null;
        }

        SetSoil(1f);
    }

    private void SetSoil(float wet)
    {
        for (int i = 0; i < soilRenderers.Length; i++)
        {
            if (soilRenderers[i] == null) continue;

            soilRenderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColor, Color.Lerp(wetColors[i] * dryTint, wetColors[i], wet));
            soilRenderers[i].SetPropertyBlock(block);
        }
    }
}
