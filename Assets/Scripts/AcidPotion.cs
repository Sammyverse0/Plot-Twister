using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AcidPotion : MonoBehaviour
{
    [SerializeField] private AcidPool poolPrefab;
    [SerializeField] private float spinSpeed = 8f;
    [SerializeField] private float lifetime = 6f;

    private Rigidbody _body;
    private bool _landed;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector3 velocity, Collider[] ignore)
    {
        foreach (Collider mine in GetComponentsInChildren<Collider>())
            foreach (Collider other in ignore)
                Physics.IgnoreCollision(mine, other);

        _body.linearVelocity = velocity;
        _body.angularVelocity = Random.onUnitSphere * spinSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_landed) return;
        _landed = true;

        Vector3 point = collision.GetContact(0).point;

        if (Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out RaycastHit ground, 5f, ~0, QueryTriggerInteraction.Ignore))
            point = ground.point;

        Instantiate(poolPrefab, point + Vector3.up * 0.03f, Quaternion.identity);
        Destroy(gameObject);
    }
}