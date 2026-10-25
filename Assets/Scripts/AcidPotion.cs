using UnityEngine;

public class AcidPotion : MonoBehaviour
{
    [SerializeField] private AcidPool poolPrefab;
    [SerializeField] private float tumbleSpeed = 90f;
    [SerializeField] private float radius = 0.2f;
    [SerializeField] private float maxLifetime = 8f;

    private Transform _holdPoint;
    private Vector3 _origin;
    private Vector3 _velocity;
    private Vector3 _tumble;
    private Transform _thrower;
    private float _age;
    private bool _launched;
    private bool _landed;

    private void Awake()
    {
        if (TryGetComponent(out Rigidbody body)) body.isKinematic = true;

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    public void Hold(Transform point)
    {
        _holdPoint = point;
        transform.SetPositionAndRotation(point.position, point.rotation);
    }

    public void Launch(Vector3 velocity, Transform thrower)
    {
        _holdPoint = null;
        _launched = true;
        _origin = transform.position;
        _velocity = velocity;
        _thrower = thrower;
        _tumble = Random.onUnitSphere * tumbleSpeed;
    }

    private void LateUpdate()
    {
        if (_launched || _holdPoint == null) return;

        transform.SetPositionAndRotation(_holdPoint.position, _holdPoint.rotation);
    }

    private void Update()
    {
        if (!_launched || _landed) return;

        _age += Time.deltaTime;
        Vector3 next = _origin + _velocity * _age + 0.5f * Physics.gravity * _age * _age;

        if (HitSomething(next, out Vector3 point))
        {
            Land(point);
            return;
        }

        transform.position = next;
        transform.Rotate(_tumble * Time.deltaTime, Space.World);

        if (_age >= maxLifetime) Land(next);
    }

    private bool HitSomething(Vector3 next, out Vector3 point)
    {
        point = next;

        Vector3 delta = next - transform.position;
        float distance = delta.magnitude;
        if (distance < 0.0001f) return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position, radius, delta / distance, distance, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;
            if (_thrower != null && hit.transform.IsChildOf(_thrower)) continue;
            if (hit.distance >= best) continue;

            best = hit.distance;
            point = hit.distance <= 0f ? transform.position : hit.point;
            found = true;
        }

        return found;
    }

    private void Land(Vector3 point)
    {
        _landed = true;

        if (poolPrefab == null)
        {
            Debug.LogError("AcidPotion: no pool prefab assigned on " + name);
            Destroy(gameObject);
            return;
        }

        Vector3 ground = AcidPool.FindGround(point + Vector3.up, point);
        Instantiate(poolPrefab, ground, Quaternion.identity);
        Destroy(gameObject);
    }
}