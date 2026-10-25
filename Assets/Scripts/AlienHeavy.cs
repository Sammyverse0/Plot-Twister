using System.Collections;
using UnityEngine;

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

    private static readonly int ThrowHash = Animator.StringToHash("Throw");

    private AlienFollower _follower;
    private Animator _animator;
    private Transform _player;
    private float _nextThrowTime;
    private bool _throwing;

    private void Start()
    {
        _follower = GetComponent<AlienFollower>();
        _animator = GetComponentInChildren<Animator>();

        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health != null) _player = health.transform;

        _nextThrowTime = Time.time + cooldown * 0.5f;
    }

    private void Update()
    {
        if (_player == null || _follower == null || _throwing) return;
        if (Time.time < _nextThrowTime) return;
        if (!_follower.IsAwake || _follower.IsCrawling) return;

        Vector3 flat = _player.position - transform.position;
        flat.y = 0f;
        float distance = flat.magnitude;

        if (distance < minRange || distance > maxRange) return;

        StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        _throwing = true;
        _nextThrowTime = Time.time + cooldown;

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

        Vector2 scatter = Random.insideUnitCircle * aimError;
        Vector3 target = _player.position + new Vector3(scatter.x, 0f, scatter.y);

        Vector3 flat = target - origin;
        flat.y = 0f;

        float flight = Mathf.Max(minFlightTime, flat.magnitude / horizontalSpeed);
        Vector3 velocity = (target - origin - 0.5f * Physics.gravity * flight * flight) / flight;

        potion.Launch(velocity, transform);
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