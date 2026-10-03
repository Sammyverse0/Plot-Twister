using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 3f;
    public float damage = 25f;

    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = -transform.right * speed;
        Destroy(gameObject, lifeTime);
    }


    private void OnCollisionEnter(Collision collision)
    {
        // The collider may be on a child of the alien, so search up the hierarchy.
        EnemyHealth enemy = collision.collider.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
            enemy.TakeDamage(damage);

        Destroy(gameObject);
    }
}