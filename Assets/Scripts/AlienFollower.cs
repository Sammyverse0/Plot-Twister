using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum MoveStyle
{Chase,Ambush,Flank,Kite}

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

    [Header("Movement Style")]
    [SerializeField] private MoveStyle moveStyle = MoveStyle.Chase;

    [Header("Ambush")]
    [SerializeField] private float leadTime = 1.2f;
    [SerializeField] private float commitDistance = 5f;
    [SerializeField] private float weaveWidth = 2.5f;
    [SerializeField] private float weaveSpeed = 2.2f;

    [Header("Flank")]
    [SerializeField] private float flankRadius = 6f;
    [SerializeField] private float flankCommitDistance = 3.5f;
    [SerializeField, Range(0f, 1f)] private float skittishChance = 0.25f;
    [SerializeField] private float skittishRange = 5f;

    [Header("Kite")]
    [SerializeField] private float kiteDistance = 11f;
    [SerializeField] private float kiteTolerance = 2.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip riseSound;
    [SerializeField] private float riseVolume = 1f;

    [Header("Ground")]
    [SerializeField] private bool snapToGround;
    [SerializeField] private float groundOffset;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float separationWeight = 1.5f;

    [Header("Crawl")]
    [SerializeField] private bool canCrawl;

    private static readonly List<AlienFollower> All = new();
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsCrawlingHash = Animator.StringToHash("IsCrawling");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private NavMeshAgent _agent;
    private Animator _animator;
    private float _wakeTime;
    private bool _isRising = true;
    private AudioSource _riseSource;
    private float _stunnedUntil;
    private bool _isCrawling;
    private bool _isDead;

    private Vector3 _targetVelocity;
    private Vector3 _lastTargetPos;
    private bool _hasLastTargetPos;
    private float _arriveRadius;
    private float _weaveSeed;

    private bool _flankReady;
    private bool _flankCommitted;
    private float _flankBearing;
    private float _flankOffset;
    private bool _skittish;
    private float _retreatUntil;
    private float _skittishReady;

    private float _strafeSign = 1f;
    private float _nextStrafeFlip;

    public bool IsAwake => !_isDead && !_isRising;
    public bool IsRising => !_isDead && _isRising;
    public bool IsDead => _isDead;
    public bool IsCrawling => _isCrawling;
    public bool CanCrawl => canCrawl;

    private void Awake()
    {
        TryGetComponent(out _agent);
        _animator = GetComponentInChildren<Animator>();
        _wakeTime = Time.time + standUpDuration;

        _weaveSeed = Random.value * 10f;
        _flankOffset = RollFlankOffset();
        _skittish = Random.value < skittishChance;
        _strafeSign = Random.value < 0.5f ? -1f : 1f;
        _nextStrafeFlip = Time.time + Random.Range(2f, 4f);

        ApplyAgentSettings();
    }

    private void Start()
    {
        if (riseSound == null) return;

        _riseSource = gameObject.AddComponent<AudioSource>();
        _riseSource.clip = riseSound;
        _riseSource.volume = riseVolume;
        _riseSource.spatialBlend = 1f;
        _riseSource.minDistance = 3f;
        _riseSource.maxDistance = 40f;
        _riseSource.playOnAwake = false;
        _riseSource.Play();
    }

    private void OnEnable() => All.Add(this);

    private void OnDisable() => All.Remove(this);

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _hasLastTargetPos = false;
        ApplyAgentSettings();
    }

    public void HoldFor(float seconds)
    {
        _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
    }

    private float CurrentSpeed => _isCrawling ? crawlSpeed : moveSpeed;

    private void ApplyAgentSettings()
    {
        if (_agent == null) return;
        _agent.speed = CurrentSpeed;
        _agent.stoppingDistance = stopDistance;
    }

    private static float RollFlankOffset()
    {
        float roll = Random.value;
        if (roll < 0.4f) return Random.Range(65f, 115f);
        if (roll < 0.8f) return -Random.Range(65f, 115f);
        return 180f + Random.Range(-20f, 20f);
    }

    public void StartCrawling()
    {
        if (!canCrawl || _isCrawling || !IsAwake) return;
        if (_isCrawling || !IsAwake) return;

        _isCrawling = true;
        if (_animator != null) _animator.SetBool(IsCrawlingHash, true);
        ApplyAgentSettings();
    }
    public static bool AnyRisingNear(Vector3 point, float range)
    {
        foreach (AlienFollower alien in All)
        {
            if (!alien.IsRising) continue;

            bool close = range <= 0f || (alien.transform.position - point).sqrMagnitude <= range * range;
            if (close) return true;
        }

        return false;
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
        {
            _isRising = false;
            if (_riseSource != null) _riseSource.Stop();
        }
    }

    private void SnapToGround()
    {
        RaycastHit[] hits = Physics.RaycastAll(
            transform.position + Vector3.up, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        float groundY = transform.position.y;

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            if (hit.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (hit.distance >= best) continue;

            best = hit.distance;
            groundY = hit.point.y;
        }

        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, groundY + groundOffset, 10f * Time.deltaTime);
        transform.position = pos;
    }

    private void TrackTarget()
    {
        if (!_hasLastTargetPos)
        {
            _lastTargetPos = target.position;
            _hasLastTargetPos = true;
            return;
        }

        Vector3 velocity = (target.position - _lastTargetPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        velocity.y = 0f;

        _targetVelocity = Vector3.Lerp(_targetVelocity, velocity, 8f * Time.deltaTime);
        _lastTargetPos = target.position;
    }

    private Vector3 PickGoal()
    {
        _arriveRadius = stopDistance;
        Vector3 player = target.position;

        if (_isCrawling) return player;

        Vector3 flat = player - transform.position;
        flat.y = 0f;
        float distance = flat.magnitude;

        return moveStyle switch
        {
            MoveStyle.Ambush => AmbushGoal(player, distance),
            MoveStyle.Flank => FlankGoal(player, distance),
            MoveStyle.Kite => KiteGoal(player, distance),
            _ => player
        };
    }

    private Vector3 AmbushGoal(Vector3 player, float distance)
    {
        if (distance < commitDistance) return player;

        float lead = Mathf.Min(leadTime, distance / Mathf.Max(CurrentSpeed, 0.1f));
        Vector3 ahead = player + _targetVelocity * lead;

        Vector3 toAhead = ahead - transform.position;
        toAhead.y = 0f;
        if (toAhead.sqrMagnitude < 0.01f) return player;

        Vector3 side = Vector3.Cross(Vector3.up, toAhead.normalized);
        float fade = Mathf.Clamp01((distance - commitDistance) / 8f);
        float weave = Mathf.Sin(Time.time * weaveSpeed + _weaveSeed) * weaveWidth * fade;

        return ahead + side * weave;
    }

    private Vector3 FlankGoal(Vector3 player, float distance)
    {
        if (!_flankReady)
        {
            Vector3 fromPlayer = transform.position - player;
            _flankBearing = Mathf.Atan2(fromPlayer.x, fromPlayer.z) * Mathf.Rad2Deg;
            _flankReady = true;
        }

        if (_flankCommitted) return ChargeOrRetreat(player, distance);

        Vector3 slot = player + Quaternion.Euler(0f, _flankBearing + _flankOffset, 0f) * Vector3.forward * flankRadius;

        Vector3 toSlot = slot - transform.position;
        toSlot.y = 0f;

        if (distance < flankCommitDistance || toSlot.magnitude < 1.5f)
            _flankCommitted = true;

        _arriveRadius = 0.5f;
        return slot;
    }

    private Vector3 ChargeOrRetreat(Vector3 player, float distance)
    {
        if (!_skittish) return player;

        if (Time.time >= _skittishReady && distance < skittishRange)
        {
            _retreatUntil = Time.time + Random.Range(1.2f, 2f);
            _skittishReady = _retreatUntil + Random.Range(3f, 5f);
        }

        if (Time.time >= _retreatUntil) return player;

        Vector3 away = transform.position - player;
        away.y = 0f;

        _arriveRadius = 0.2f;
        return transform.position + away.normalized * 5f;
    }

    private Vector3 KiteGoal(Vector3 player, float distance)
    {
        Vector3 away = transform.position - player;
        away.y = 0f;
        away = away.sqrMagnitude > 0.001f ? away.normalized : -transform.forward;

        if (Time.time >= _nextStrafeFlip)
        {
            _strafeSign = -_strafeSign;
            _nextStrafeFlip = Time.time + Random.Range(2f, 4f);
        }

        _arriveRadius = 0.3f;

        if (distance > kiteDistance + kiteTolerance) return player + away * kiteDistance;
        if (distance < kiteDistance - kiteTolerance) return transform.position + away * 5f;

        Vector3 side = Vector3.Cross(Vector3.up, away) * _strafeSign;
        return transform.position + side * 5f;
    }

    private Vector3 FacingDirection(Vector3 move, Vector3 toTarget)
    {
        bool faceMovement = moveStyle != MoveStyle.Chase && !_isCrawling && move.sqrMagnitude > 0.01f;
        return faceMovement ? move : toTarget;
    }

    private void Update()
    {
        if (snapToGround && !_isDead && (_agent == null || !_agent.enabled || !_agent.isOnNavMesh))
            SnapToGround();

        if (_isRising) CheckRising();
        if (target == null) return;

        TrackTarget();
        if (!IsAwake) return;

        bool stunned = Time.time < _stunnedUntil;
        bool onNavMesh = _agent != null && _agent.enabled && _agent.isOnNavMesh;

        if (onNavMesh) _agent.isStopped = stunned;
        if (stunned) return;

        if (_animator != null && !_animator.GetBool(IsWalkingHash))
            _animator.SetBool(IsWalkingHash, true);

        Vector3 goal = PickGoal();

        if (onNavMesh)
        {
            _agent.SetDestination(goal);
            return;
        }

        Vector3 toGoal = goal - transform.position;
        toGoal.y = 0f;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        Vector3 move = Vector3.zero;
        if (toGoal.magnitude > _arriveRadius)
            move += toGoal.normalized;
        move += GetSeparation() * separationWeight;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.position += move * (CurrentSpeed * Time.deltaTime);

        Vector3 face = FacingDirection(move, toTarget);
        if (face.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(face.normalized),
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