using System.Collections;
using UnityEngine;

public class HeavyThrowAttack : MonoBehaviour
{
    [SerializeField] private AcidPotion potionPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float minRange = 6f;
    [SerializeField] private float maxRange = 18f;
    [SerializeField] private float cooldown = 6f;
    [SerializeField] private float windUp = 0.6f;
    [SerializeField] private float recover = 1f;
    [SerializeField] private float flightTime = 1f;
    [SerializeField] private float aimError = 0.6f;

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
        _follower.HoldFor(windUp + recover);

        if (_animator != null) _animator.SetTrigger(ThrowHash);

        float elapsed = 0f;
        while (elapsed < windUp)
        {
            if (_follower.IsDead)
            {
                _throwing = false;
                yield break;
            }

            FacePlayer();
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_follower.IsDead) ReleasePotion();

        yield return new WaitForSeconds(recover);
        _throwing = false;
    }

    private void FacePlayer()
    {
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            10f * Time.deltaTime);
    }

    private void ReleasePotion()
    {
        Vector3 origin = throwPoint != null
            ? throwPoint.position
            : transform.position + Vector3.up * 2f + transform.forward;

        Vector2 scatter = Random.insideUnitCircle * aimError;
        Vector3 target = _player.position + new Vector3(scatter.x, 0f, scatter.y);

        Vector3 velocity = (target - origin - 0.5f * Physics.gravity * flightTime * flightTime) / flightTime;

        AcidPotion potion = Instantiate(potionPrefab, origin, Quaternion.identity);
        potion.Launch(velocity, GetComponentsInChildren<Collider>());
    }
}