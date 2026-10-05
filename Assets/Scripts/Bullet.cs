using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 150f;
    public float lifeTime = 3f;
    public float damage = 25f;
    [SerializeField] private LayerMask hitMask = ~0;

    private Vector3 direction;

    public void Fire(Vector3 newDirection)
    {
        direction = newDirection.normalized;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f);
    }

    void Start()
    {
        if (direction == Vector3.zero)
            direction = -transform.right;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        float distance = speed * Time.deltaTime;

        if (CheckHit(distance))
        {
            Destroy(gameObject);
            return;
        }

        transform.position += direction * distance;
    }

    private bool CheckHit(float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, distance, hitMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;

            if (col.transform.IsChildOf(transform)) continue;
            if (col.GetComponentInParent<CharacterController>() != null) continue;

            HitZone zone = col.GetComponent<HitZone>();
            if (zone != null)
            {
                zone.Hit(damage);
                return true;
            }

            if (col.isTrigger) continue;

            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
                enemy.TakeDamage(damage);

            return true;
        }

        return false;
    }
}
