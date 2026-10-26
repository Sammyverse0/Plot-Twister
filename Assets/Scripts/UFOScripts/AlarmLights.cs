using UnityEngine;

public class AlarmLights : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectiveManager objectiveManager;

    [Header("Settings")]
    [SerializeField] private Color alarmColor = Color.red;
    [SerializeField] private float blinkInterval = 0.5f;
    [SerializeField, Range(0f, 1f)] private float offIntensityMultiplier = 0.1f;
    [SerializeField] private bool spotLightsOnly = true;
    [SerializeField] private int triggerOnPress = 1;

    private Light[] lights;
    private float[] baseIntensities;
    private bool alarmActive;
    private bool lightsOn = true;
    private float timer;

    private void Awake()
    {
        if (objectiveManager == null)
            objectiveManager = FindFirstObjectByType<ObjectiveManager>();
    }

    private void OnEnable()
    {
        if (objectiveManager != null)
            objectiveManager.ButtonPressed += OnButtonPressed;
    }

    private void OnDisable()
    {
        if (objectiveManager != null)
            objectiveManager.ButtonPressed -= OnButtonPressed;
    }

    private void Start()
    {
        CollectLights();
    }

    private void CollectLights()
    {
        Light[] all = FindObjectsByType<Light>(FindObjectsSortMode.None);
        System.Collections.Generic.List<Light> found = new System.Collections.Generic.List<Light>();

        foreach (Light light in all)
        {
            if (spotLightsOnly && light.type != LightType.Spot) continue;
            found.Add(light);
        }

        lights = found.ToArray();
        baseIntensities = new float[lights.Length];

        for (int i = 0; i < lights.Length; i++)
        {
            baseIntensities[i] = lights[i].intensity;
        }
    }

    private void OnButtonPressed(int pressedCount)
    {
        if (alarmActive || pressedCount < triggerOnPress) return;

        StartAlarm();
    }

    public void StartAlarm()
    {
        if (alarmActive) return;

        if (lights == null) CollectLights();

        alarmActive = true;
        lightsOn = true;
        timer = 0f;

        foreach (Light light in lights)
        {
            if (light != null)
            {
                light.color = alarmColor;
            }
        }
    }

    private void Update()
    {
        if (!alarmActive) return;

        timer += Time.deltaTime;
        if (timer < blinkInterval) return;

        timer = 0f;
        lightsOn = !lightsOn;
        ApplyIntensity();
    }

    private void ApplyIntensity()
    {
        float multiplier = lightsOn ? 1f : offIntensityMultiplier;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].intensity = baseIntensities[i] * multiplier;
        }
    }
}