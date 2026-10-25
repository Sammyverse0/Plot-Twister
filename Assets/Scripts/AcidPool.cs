using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AcidPool : MonoBehaviour
{
    [SerializeField] private Material acidMaterial;
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private int blobCount = 10;
    [SerializeField] private float duration = 8f;
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float tickTime = 0.5f;
    [SerializeField] private float maxVerticalGap = 1f;
    [SerializeField] private float footPadding = 0.3f;
    

    private readonly List<Transform> _blobs = new();
    private readonly List<Vector3> _baseScales = new();
    
    private PlayerHealth _player;
    private Collider _playerCollider;
    private float _endTime;
    private float _nextTick;
    public float FootprintRadius => radius * 1.1f;

    public static Vector3 FindGround(Vector3 from, Vector3 fallback)
    {
        RaycastHit[] hits = Physics.RaycastAll(from + Vector3.up * 2f, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        Vector3 result = fallback;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            if (hit.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (hit.collider.GetComponentInParent<AcidPotion>() != null) continue;
            if (hit.distance >= best) continue;

            best = hit.distance;
            result = hit.point;
        }

        return result;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerHealth>();
        if (_player != null) _playerCollider = _player.GetComponentInChildren<Collider>();

        _endTime = Time.time + duration;

        if (acidMaterial == null)
            Debug.LogWarning("AcidPool: no acid material assigned on " + name);

        AddBlob(Vector2.zero, radius * 1.4f);

        for (int i = 0; i < blobCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius * 0.8f;
            float size = Random.Range(0.5f, 1f) * radius;
            AddBlob(offset, size);
        }

        Destroy(gameObject, duration);
    }

    private void AddBlob(Vector2 offset, float size)
    {
        Vector3 flatPos = transform.position + new Vector3(offset.x, 0f, offset.y);
        Vector3 ground = FindGround(flatPos + Vector3.up * 2f, flatPos);

        GameObject blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(blob.GetComponent<Collider>());

        Transform t = blob.transform;
        t.SetParent(transform, false);
        t.position = new Vector3(flatPos.x, ground.y + 0.06f, flatPos.z);
        t.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Vector3 scale = new Vector3(size * Random.Range(0.8f, 1.3f), 0.1f, size);
        t.localScale = scale;

        Renderer rend = blob.GetComponent<Renderer>();
        if (acidMaterial != null) rend.sharedMaterial = acidMaterial;
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

        if (PlayerTouchingAcid())
        {
            _player.TakeDamage(damage);
            _nextTick = Time.time + tickTime;
        }
    }

    private bool PlayerTouchingAcid()
    {
        Vector3 feet = _playerCollider != null
            ? new Vector3(_playerCollider.bounds.center.x, _playerCollider.bounds.min.y, _playerCollider.bounds.center.z)
            : _player.transform.position;

        foreach (Transform blob in _blobs)
        {
            Vector3 offset = feet - blob.position;
            float gap = Mathf.Abs(offset.y);
            offset.y = 0f;

            float reach = (blob.localScale.x + blob.localScale.z) * 0.25f + footPadding;

            if (offset.magnitude <= reach && gap <= maxVerticalGap)
                return true;
        }

        return false;
    }
}