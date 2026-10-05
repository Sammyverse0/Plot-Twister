using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [Tooltip("Max horizontal distance from the player's collider surface that counts as a hit.")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float maxVerticalGap = 3f;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private PlayerHealth _playerHealth;
    private Collider _playerCollider;
    private AlienFollower _follower;
    private Animator _animator;
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
        _follower = GetComponent<AlienFollower>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_playerHealth.IsDead || Time.time < _nextAttackTime) return;
        if (_follower != null && !_follower.IsAwake) return;

        Vector3 myPos = transform.position;
        Vector3 closest = _playerCollider.ClosestPoint(myPos);

        Vector3 flat = closest - myPos;
        flat.y = 0f;

        float verticalGap = Mathf.Abs(myPos.y - _playerCollider.bounds.center.y);

        if (flat.magnitude <= attackRange && verticalGap <= maxVerticalGap)
        {
            if (_animator != null) _animator.SetTrigger(AttackHash);
            _playerHealth.TakeDamage(damage);
            _nextAttackTime = Time.time + attackCooldown;
        }
    }
}