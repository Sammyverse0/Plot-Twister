using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class UfoMissile : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 8f;
    [SerializeField] private float launchTime = 0.7f;
    [SerializeField] private float chaseSpeed = 16f;
    [SerializeField] private float turnSpeed = 140f;
    [SerializeField] private float maxTurnSpeed = 400f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float hitDistance = 1.2f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float spinSpeed = 540f;
    [SerializeField] private Transform model;
    [SerializeField] private GameObject explosion;

    private Transform target;
    private PlayerHealth playerHealth;
    private Vector3 velocity;
    private float age;

    private void Awake()
    {
        GetComponent<EnemyHealth>().OnDeath += Explode;
    }

    public void Launch(Transform newTarget, Vector3 startDirection)
    {
        target = newTarget;
        playerHealth = target.GetComponent<PlayerHealth>();
        velocity = startDirection.normalized * launchSpeed;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target == null) return;

        age += Time.deltaTime;

        Vector3 aimPoint = target.position + Vector3.up * 0.8f;
        Vector3 toTarget = aimPoint - transform.position;

        if (age < launchTime)
        {
            velocity += Vector3.up * (4f * Time.deltaTime);
        }
        else
        {
            float turn = Mathf.Lerp(turnSpeed, maxTurnSpeed, (age - launchTime) / 3f);
            float speed = Mathf.MoveTowards(velocity.magnitude, chaseSpeed, chaseSpeed * 2f * Time.deltaTime);

            Vector3 newDir = Vector3.RotateTowards(velocity.normalized, toTarget.normalized, turn * Mathf.Deg2Rad * Time.deltaTime, 0f);
            velocity = newDir * speed;
        }

        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(velocity);

        if (model != null)
            model.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

        if (toTarget.magnitude <= hitDistance)
        {
            if (playerHealth != null) playerHealth.TakeDamage(damage);
            Explode();
        }
    }

    private void Explode()
    {
        if (explosion != null)
            Destroy(Instantiate(explosion, transform.position, Quaternion.identity), 2f);

        Destroy(gameObject);
    }
}
