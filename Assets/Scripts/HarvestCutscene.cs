using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestCutscene : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private GameObject cutsceneCamera;
    [SerializeField] private float orbitTime = 7f;
    [SerializeField] private float orbitAngle = 300f;
    [SerializeField] private float extraDistance = 6f;
    [SerializeField] private float height = 6f;
    [SerializeField] private float blendTime = 1.5f;

    [Header("Crops")]
    [SerializeField] private GameObject[] cropPrefabs;
    [SerializeField] private int rowsPerPlot = 5;
    [SerializeField] private int cropsPerRow = 5;
    [SerializeField] private float edgeMargin = 0.12f;
    [SerializeField] private float jitter = 0.25f;
    [SerializeField] private float maxRandomTurn = 25f;
    [SerializeField] private float groundOffset = 0f;
    [SerializeField] private float riseDepth = 1.5f;
    [SerializeField] private float riseTime = 1.2f;
    [SerializeField] private float riseSpread = 3f;
    [SerializeField] private Vector2 cropScale = new Vector2(0.8f, 1.2f);

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip harvestSound;
    [SerializeField] private float fadeOutTime = 1.5f;

    [Header("Player")]
    [SerializeField] private MonoBehaviour[] disableDuringCutscene;
    [SerializeField] private GameObject[] hideDuringCutscene;

    private readonly List<GameObject> crops = new();

    private void Awake()
    {
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
    }

    public IEnumerator Play(List<Plot> plots)
    {
        if (plots.Count == 0) yield break;

        SetPlayerControl(false);

        Vector3 center = Vector3.zero;
        foreach (Plot plot in plots) center += plot.transform.position;
        center /= plots.Count;

        float radius = 0f;
        foreach (Plot plot in plots)
            radius = Mathf.Max(radius, Vector3.Distance(plot.transform.position, center));
        radius += extraDistance;

        Camera cam = Camera.main;
        Vector3 fromCenter = cam != null ? cam.transform.position - center : Vector3.back;
        float startAngle = Mathf.Atan2(fromCenter.z, fromCenter.x) * Mathf.Rad2Deg;

        PlaceCamera(center, radius, startAngle);
        if (cutsceneCamera != null) cutsceneCamera.SetActive(true);

        GrowCrops(plots);

        if (audioSource != null && harvestSound != null)
        {
            audioSource.Stop();
            audioSource.volume = 1f;
            audioSource.clip = harvestSound;
            audioSource.Play();
        }

        float time = 0f;
        while (time < orbitTime)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / orbitTime);
            PlaceCamera(center, radius, startAngle + orbitAngle * t);
            yield return null;
        }

        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        StartCoroutine(FadeOut());
        yield return new WaitForSeconds(blendTime);

        SetPlayerControl(true);
    }

    private IEnumerator FadeOut()
    {
        if (audioSource == null || !audioSource.isPlaying) yield break;

        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < fadeOutTime)
        {
            time += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / fadeOutTime);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    private void PlaceCamera(Vector3 center, float radius, float angle)
    {
        if (cutsceneCamera == null) return;

        float rad = angle * Mathf.Deg2Rad;
        Vector3 pos = center + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius + Vector3.up * height;

        cutsceneCamera.transform.position = pos;
        cutsceneCamera.transform.rotation = Quaternion.LookRotation(center - pos);
    }

    private void GrowCrops(List<Plot> plots)
    {
        if (cropPrefabs == null || cropPrefabs.Length == 0) return;

        int total = plots.Count * rowsPerPlot * cropsPerRow;
        int index = 0;

        foreach (Plot plot in plots)
        {
            Bounds area = GetPlotArea(plot);
            GameObject prefab = cropPrefabs[Random.Range(0, cropPrefabs.Length)];
            float usable = 1f - edgeMargin * 2f;
            float stepX = area.size.x * usable / cropsPerRow;
            float stepZ = area.size.z * usable / rowsPerPlot;

            for (int row = 0; row < rowsPerPlot; row++)
            {
                for (int col = 0; col < cropsPerRow; col++)
                {
                    float x = area.min.x + area.size.x * edgeMargin + stepX * (col + 0.5f + Random.Range(-jitter, jitter));
                    float z = area.min.z + area.size.z * edgeMargin + stepZ * (row + 0.5f + Random.Range(-jitter, jitter));
                    Vector3 ground = new Vector3(x, plot.transform.position.y + groundOffset, z);

                    Quaternion turn = Quaternion.Euler(0f, Random.Range(-maxRandomTurn, maxRandomTurn), 0f);
                    GameObject crop = Instantiate(prefab, ground, turn * prefab.transform.rotation);
                    crops.Add(crop);

                    float delay = riseSpread * index / Mathf.Max(1, total);
                    StartCoroutine(Rise(crop.transform, ground, delay));
                    index++;
                }
            }
        }
    }

    private Bounds GetPlotArea(Plot plot)
    {
        Renderer[] renderers = plot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(plot.transform.position, new Vector3(2f, 0f, 2f));

        Bounds area = renderers[0].bounds;
        foreach (Renderer r in renderers)
            area.Encapsulate(r.bounds);

        return area;
    }

    private IEnumerator Rise(Transform crop, Vector3 ground, float delay)
    {
        Vector3 fullScale = crop.localScale * Random.Range(cropScale.x, cropScale.y);
        Vector3 start = ground - Vector3.up * riseDepth;

        crop.position = start;
        crop.localScale = fullScale * 0.2f;

        yield return new WaitForSeconds(delay);

        float time = 0f;
        while (time < riseTime && crop != null)
        {
            time += Time.deltaTime;
            float t = time / riseTime;
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float pop = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;

            crop.position = Vector3.Lerp(start, ground, ease);
            crop.localScale = fullScale * (Mathf.Lerp(0.2f, 1f, ease) * pop);
            yield return null;
        }

        if (crop != null)
        {
            crop.position = ground;
            crop.localScale = fullScale;
        }
    }

    public void ClearCrops()
    {
        foreach (GameObject crop in crops)
            if (crop != null) Destroy(crop);
        crops.Clear();
    }

    private void SetPlayerControl(bool on)
    {
        foreach (MonoBehaviour script in disableDuringCutscene)
            if (script != null) script.enabled = on;

        foreach (GameObject thing in hideDuringCutscene)
            if (thing != null) thing.SetActive(on);
    }
}
