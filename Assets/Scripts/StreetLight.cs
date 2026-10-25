using UnityEngine;

public class StreetLight : MonoBehaviour
{
    [SerializeField] private Light lamp;
    [SerializeField] private Renderer bulb;
    [SerializeField] private float baseIntensity = 4f;
    [SerializeField, ColorUsage(false, true)] private Color bulbColor = new Color(2.4f, 1.8f, 0.9f);

    [Header("Flicker")]
    [SerializeField] private float flickerRange = 25f;
    [SerializeField] private float cutsPerSecond = 5f;
    [SerializeField] private float flickerSpeed = 18f;
    [SerializeField] private float settleTime = 0.8f;

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private MaterialPropertyBlock _block;
    private float _disturbance;
    private float _cutUntil;
    private float _seed;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        _seed = Random.value * 100f;
        if (lamp == null) lamp = GetComponentInChildren<Light>();
    }

    private void Update()
    {
        bool disturbed = AlienFollower.AnyRisingNear(transform.position, flickerRange);

        float rate = disturbed ? 4f : 1f / Mathf.Max(settleTime, 0.01f);
        _disturbance = Mathf.MoveTowards(_disturbance, disturbed ? 1f : 0f, rate * Time.deltaTime);

        float level = 1f;

        if (_disturbance > 0f)
        {
            if (disturbed && Time.time >= _cutUntil && Random.value < cutsPerSecond * Time.deltaTime)
                _cutUntil = Time.time + Random.Range(0.04f, 0.22f);

            float wobble = Mathf.PerlinNoise(Time.time * flickerSpeed, _seed) * 0.7f + 0.3f;
            level = Mathf.Lerp(1f, wobble, _disturbance);

            if (Time.time < _cutUntil) level *= 0.04f;
        }

        Apply(level);
    }

    private void Apply(float level)
    {
        if (lamp != null) lamp.intensity = baseIntensity * level;

        if (bulb == null) return;

        bulb.GetPropertyBlock(_block);
        _block.SetColor(EmissionColor, bulbColor * level);
        bulb.SetPropertyBlock(_block);
    }
}