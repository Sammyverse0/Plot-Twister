using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AcidPool : MonoBehaviour
{
    [SerializeField] private Material acidMaterial;
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private int blobCount = 8;
    [SerializeField] private float duration = 8f;
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float tickTime = 0.5f;
    [SerializeField] private float maxVerticalGap = 1.5f;

    private readonly List<Transform> _blobs = new();
    private readonly List<Vector3> _baseScales = new();

    private PlayerHealth _player;
    private float _endTime;
    private float _nextTick;

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerHealth>();
        _endTime = Time.time + duration;

        AddBlob(Vector2.zero, radius * 1.4f);

        for (int i = 0; i < blobCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius * 0.75f;
            float size = Random.Range(0.5f, 1f) * radius;
            AddBlob(offset, size);
        }

        Destroy(gameObject, duration);
    }

    private void AddBlob(Vector2 offset, float size)
    {
        GameObject blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(blob.GetComponent<Collider>());

        Transform t = blob.transform;
        t.SetParent(transform, false);
        t.localPosition = new Vector3(offset.x, 0f, offset.y);
        t.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Vector3 scale = new Vector3(size * Random.Range(0.8f, 1.3f), 0.04f, size);
        t.localScale = scale;

        Renderer rend = blob.GetComponent<Renderer>();
        rend.sharedMaterial = acidMaterial;
        rend.shadowCastingMode = ShadowCastingMode.Off;

        _blobs.Add(t);
        _baseScales.Add(scale);
    }

    private void Update()
    {
        float remaining = _endTime - Time.time;

        if (remaining < fadeTime)
        {
            float k = Mathf.Clamp01(remaining / fadeTime);
            for (int i = 0; i < _blobs.Count; i++)
                _blobs[i].localScale = _baseScales[i] * k;
        }

        if (_player == null || _player.IsDead || Time.time < _nextTick) return;

        Vector3 offset = _player.transform.position - transform.position;
        float gap = Mathf.Abs(offset.y);
        offset.y = 0f;

        if (offset.magnitude <= radius && gap <= maxVerticalGap)
        {
            _player.TakeDamage(damage);
            _nextTick = Time.time + tickTime;
        }
    }
}
