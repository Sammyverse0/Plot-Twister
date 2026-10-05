using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AlienFollower : MonoBehaviour
{
    public Transform target;

    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float crawlSpeed = 0.8f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private float standUpDuration = 2f;
    [SerializeField] private string riseStateName = "Stand up";
    [SerializeField] private float hitStunTime = 0.4f;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float separationWeight = 1.5f;

    private static readonly List<AlienFollower> All = new();
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsCrawlingHash = Animator.StringToHash("IsCrawling");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private NavMeshAgent _agent;
    private Animator _animator;
    private float _wakeTime;
    private bool _isRising = true;
    private float _stunnedUntil;
    private bool _isCrawling;
    private bool _isDead;

    public bool IsAwake => !_isDead && !_isRising;
    public bool IsDead => _isDead;
    public bool IsCrawling => _isCrawling;

    private void Awake()
    {
        TryGetComponent(out _agent);
        _animator = GetComponentInChildren<Animator>();
        _wakeTime = Time.time + standUpDuration;
        ApplyAgentSettings();
    }

    private void OnEnable() => All.Add(this);

    private void OnDisable() => All.Remove(this);

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ApplyAgentSettings();
    }

    private float CurrentSpeed => _isCrawling ? crawlSpeed : moveSpeed;

    private void ApplyAgentSettings()
    {
        if (_agent == null) return;
        _agent.speed = CurrentSpeed;
        _agent.stoppingDistance = stopDistance;
    }

    public void StartCrawling()
    {
        if (_isCrawling || !IsAwake) return;

        _isCrawling = true;
        if (_animator != null) _animator.SetBool(IsCrawlingHash, true);
        ApplyAgentSettings();
    }

    public void GetHit()
    {
        if (!IsAwake || _isCrawling) return;

        if (_animator != null) _animator.SetTrigger(HitHash);
        _stunnedUntil = Time.time + hitStunTime;
    }

    public void Die()
    {
        _isDead = true;

        if (_animator != null)
        {
            _animator.SetBool(IsCrawlingHash, false);
            _animator.SetTrigger(DieHash);
        }

        if (_agent != null) _agent.enabled = false;
    }

    private void CheckRising()
    {
        if (_animator == null)
        {
            _isRising = Time.time < _wakeTime;
            return;
        }

        if (_animator.IsInTransition(0)) return;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName(riseStateName) || state.normalizedTime >= 1f)
            _isRising = false;
    }

    private void Update()
    {
        if (_isRising) CheckRising();
        if (target == null || !IsAwake) return;

        bool stunned = Time.time < _stunnedUntil;

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = stunned;
        }

        if (stunned) return;

        if (_animator != null && !_animator.GetBool(IsWalkingHash))
            _animator.SetBool(IsWalkingHash, true);

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.SetDestination(target.position);
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        Vector3 move = Vector3.zero;
        if (toTarget.magnitude > stopDistance)
            move += toTarget.normalized;
        move += GetSeparation() * separationWeight;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.position += move * (CurrentSpeed * Time.deltaTime);

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(toTarget.normalized),
                turnSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetSeparation()
    {
        Vector3 push = Vector3.zero;

        foreach (AlienFollower other in All)
        {
            if (other == this || other._isDead) continue;

            Vector3 offset = transform.position - other.transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (distance >= separationRadius) continue;

            if (distance < 0.0001f)
            {
                Vector2 random = Random.insideUnitCircle.normalized;
                push += new Vector3(random.x, 0f, random.y);
            }
            else
            {
                push += offset / distance * (1f - distance / separationRadius);
            }
        }

        return push;
    }
}
