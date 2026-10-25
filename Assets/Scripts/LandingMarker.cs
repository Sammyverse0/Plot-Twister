using UnityEngine;
using UnityEngine.Rendering;

public class LandingMarker : MonoBehaviour
{
    private const int Segments = 48;

    private static Material _fallbackMaterial;

    private LineRenderer _line;
    private float _duration;
    private float _age;

    public static void Spawn(Vector3 groundPoint, float radius, float duration, Material material)
    {
        GameObject go = new GameObject("LandingMarker");
        go.transform.SetPositionAndRotation(groundPoint + Vector3.up * 0.08f, Quaternion.Euler(90f, 0f, 0f));
        go.AddComponent<LandingMarker>().Build(radius, duration, material);
    }

    private static Material GetFallback()
    {
        if (_fallbackMaterial == null)
            _fallbackMaterial = new Material(Shader.Find("Sprites/Default"));

        return _fallbackMaterial;
    }

    private void Build(float radius, float duration, Material material)
    {
        _duration = Mathf.Max(0.1f, duration);

        _line = gameObject.AddComponent<LineRenderer>();
        _line.useWorldSpace = false;
        _line.loop = true;
        _line.alignment = LineAlignment.TransformZ;
        _line.widthMultiplier = 0.18f;
        _line.shadowCastingMode = ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.sharedMaterial = material != null ? material : GetFallback();

        _line.positionCount = Segments;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i / (float)Segments * Mathf.PI * 2f;
            _line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }

        Destroy(gameObject, _duration + 0.05f);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / _duration);

        transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, t);

        Color ring = new Color(0.22f, 1f, 0.08f, Mathf.Lerp(0.25f, 1f, t));
        _line.startColor = ring;
        _line.endColor = ring;
    }
}