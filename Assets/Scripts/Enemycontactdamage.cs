using UnityEngine;


public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float maxVerticalGap = 3f;

    private PlayerHealth _playerHealth;
    private Collider _playerCollider;
    private float _nextAttackTime;

    private void Start()
    {
        _playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (_playerHealth == null)
        {
            Debug.LogWarning("EnemyContactDamage: no PlayerHealth found in the scene.");
            enabled = false;
            return;
        }
        _playerCollider = _playerHealth.GetComponent<Collider>();
    }

    private void Update()
    {
        if (_playerHealth.IsDead || Time.time < _nextAttackTime) return;

        Vector3 myPos = transform.position;
        Vector3 closest = _playerCollider.ClosestPoint(myPos);

        
        Vector3 flat = closest - myPos;
        flat.y = 0f;

        float verticalGap = Mathf.Abs(myPos.y - _playerCollider.bounds.center.y);

        if (flat.magnitude <= attackRange && verticalGap <= maxVerticalGap)
        {
            _playerHealth.TakeDamage(damage);
            _nextAttackTime = Time.time + attackCooldown;
        }
    }
}