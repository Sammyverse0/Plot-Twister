using System.Collections;
using UnityEngine;

public enum AimStyle
{
    Mixed,
    Lead,
    Behind,
    Ahead,
    Side
}

public class HeavyThrowAttack : MonoBehaviour
{
    [SerializeField] private AcidPotion potionPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private string throwStateName = "Throw";
    [SerializeField] private int releaseFrame = 71;
    [SerializeField] private int clipFrames = 146;
    [SerializeField] private float fallbackDelay = 0f;
    [SerializeField] private float minRange = 6f;
    [SerializeField] private float maxRange = 18f;
    [SerializeField] private float cooldown = 6f;
    [SerializeField] private float horizontalSpeed = 9f;
    [SerializeField] private float minFlightTime = 1.2f;
    [SerializeField] private float aimError = 0.6f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("Variety")]
    [SerializeField] private AimStyle aimStyle = AimStyle.Mixed;
    [SerializeField] private float throwGap = 2f;
    [SerializeField] private float leadStrength = 1f;
    [SerializeField] private float behindDistance = 4f;
    [SerializeField] private float aheadDistance = 6f;
    [SerializeField] private float sideDistance = 4.5f;
    [SerializeField, Range(0f, 1f)] private float fanChance = 0.25f;
    [SerializeField] private float fanSpread = 4.5f;

    [Header("Landing Marker")]
    [SerializeField] private bool showMarker = true;
    [SerializeField] private Material markerMaterial;

    private static readonly int ThrowHash = Animator.StringToHash("Throw");
    private static readonly AimStyle[] MixedPool = { AimStyle.Lead, AimStyle.Behind, AimStyle.Ahead, AimStyle.Side };

    private static float _nextGlobalThrow;
    private static AimStyle _lastStyle = AimStyle.Mixed;

    private AlienFollower _follower;
    private Animator _animator;
    private Transform _player;
    private Vector3 _playerVelocity;
    private Vector3 _lastPlayerPos;
    private float _nextThrowTime;
    private bool _throwing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetShared()
    {
        _nextGlobalThrow = 0f;
        _lastStyle = AimStyle.Mixed;
    }

    private void Start()
    {
        _follower = GetComponent<AlienFollower>();
        _animator = GetComponentInChildren<Animator>();

        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health != null)
        {
            _player = health.transform;
            _lastPlayerPos = _player.position;
        }

        _nextThrowTime = Time.time + cooldown * 0.5f;
    }

    private void Update()
    {
        if (_player == null || _follower == null) return;

        TrackPlayer();

        if (_throwing) return;
        if (Time.time < _nextThrowTime || Time.time < _nextGlobalThrow) return;
        if (!_follower.IsAwake || _follower.IsCrawling) return;

        Vector3 flat = _player.position - transform.position;
        flat.y = 0f;
        float distance = flat.magnitude;

        if (distance < minRange || distance > maxRange) return;

        StartCoroutine(ThrowRoutine());
    }

    private void TrackPlayer()
    {
        Vector3 velocity = (_player.position - _lastPlayerPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        velocity.y = 0f;

        _playerVelocity = Vector3.Lerp(_playerVelocity, velocity, 8f * Time.deltaTime);
        _lastPlayerPos = _player.position;
    }

    private IEnumerator ThrowRoutine()
    {
        _throwing = true;
        _nextThrowTime = Time.time + cooldown;
        _nextGlobalThrow = Time.time + throwGap + Random.Range(0f, 0.8f);

        if (_animator != null) _animator.SetTrigger(ThrowHash);

        AcidPotion potion = SpawnHeldPotion();
        float releaseAt = (float)releaseFrame / clipFrames;
        bool entered = false;
        bool released = false;
        float elapsed = 0f;

        while (true)
        {
            if (!HoldAndFace())
            {
                if (potion != null) Destroy(potion.gameObject);
                _throwing = false;
                yield break;
            }

            elapsed += Time.deltaTime;

            if (_animator == null)
            {
                if (elapsed >= fallbackDelay) break;
                yield return null;
                continue;
            }

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(throwStateName) && _animator.IsInTransition(0))
                state = _animator.GetNextAnimatorStateInfo(0);

            if (state.IsName(throwStateName))
            {
                entered = true;

                if (!released && state.normalizedTime >= releaseAt)
                {
                    if (potion != null) ThrowPotion(potion);
                    released = true;
                }

                if (state.normalizedTime >= 1f) break;
            }
            else if (entered || elapsed > 1f)
            {
                break;
            }

            yield return null;
        }

        if (!released && potion != null)
        {
            if (!entered) Debug.LogWarning("HeavyThrowAttack: never found a state named " + throwStateName);
            ThrowPotion(potion);
        }

        _throwing = false;
    }

    private AcidPotion SpawnHeldPotion()
    {
        if (potionPrefab == null)
        {
            Debug.LogError("HeavyThrowAttack: no potion prefab assigned");
            return null;
        }

        Transform hold = throwPoint != null ? throwPoint : transform;
        AcidPotion potion = Instantiate(potionPrefab, hold.position, hold.rotation);
        potion.Hold(hold);
        return potion;
    }

    private void ThrowPotion(AcidPotion potion)
    {
        Vector3 origin = potion.transform.position;
        AimStyle style = aimStyle == AimStyle.Mixed ? NextMixedStyle() : aimStyle;
        Vector3 aim = PickAimPoint(origin, style);

        LaunchAt(potion, origin, aim);

        if (Random.value >= fanChance) return;

        Vector3 across = Vector3.Cross(Vector3.up, aim - origin).normalized;

        for (int side = -1; side <= 1; side += 2)
        {
            AcidPotion extra = Instantiate(potionPrefab, origin, potion.transform.rotation);
            LaunchAt(extra, origin, aim + across * (fanSpread * side));
        }
    }

    private static AimStyle NextMixedStyle()
    {
        AimStyle next;

        do next = MixedPool[Random.Range(0, MixedPool.Length)];
        while (next == _lastStyle);

        _lastStyle = next;
        return next;
    }

    private Vector3 PickAimPoint(Vector3 origin, AimStyle style)
    {
        Vector3 flatToPlayer = _player.position - origin;
        flatToPlayer.y = 0f;
        float flightGuess = Mathf.Max(minFlightTime, flatToPlayer.magnitude / horizontalSpeed);

        float speed = _playerVelocity.magnitude;
        Vector3 heading = speed > 0.5f ? _playerVelocity.normalized : FlatForward(_player);
        Vector3 point = _player.position;

        if (speed < 0.5f) style = AimStyle.Lead;

        switch (style)
        {
            case AimStyle.Lead:
                point += _playerVelocity * (flightGuess * leadStrength);
                break;

            case AimStyle.Behind:
                point -= heading * behindDistance;
                break;

            case AimStyle.Ahead:
                point += heading * Mathf.Max(aheadDistance, speed * flightGuess * 1.5f);
                break;

            case AimStyle.Side:
            {
                Vector3 right = Vector3.Cross(Vector3.up, heading);
                point += right * (sideDistance * (Random.value < 0.5f ? -1f : 1f));
                break;
            }
        }

        Vector2 scatter = Random.insideUnitCircle * aimError;
        point += new Vector3(scatter.x, 0f, scatter.y);

        return AcidPool.FindGround(point + Vector3.up * 3f, point);
    }

    private void LaunchAt(AcidPotion potion, Vector3 origin, Vector3 aim)
    {
        Vector3 flat = aim - origin;
        flat.y = 0f;

        float flight = Mathf.Max(minFlightTime, flat.magnitude / horizontalSpeed);
        Vector3 velocity = (aim - origin - 0.5f * Physics.gravity * flight * flight) / flight;

        if (showMarker) LandingMarker.Spawn(aim, potion.PoolRadius, flight, markerMaterial);

        potion.Launch(velocity, transform);
    }

    private static Vector3 FlatForward(Transform t)
    {
        Vector3 forward = t.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    private bool HoldAndFace()
    {
        if (_follower.IsDead) return false;

        _follower.HoldFor(0.15f);

        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                turnSpeed * Time.deltaTime);
        }

        return true;
    }
}