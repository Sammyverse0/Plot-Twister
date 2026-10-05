using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossController : MonoBehaviour
{
    [SerializeField] private string bossName = "Cat Alien";
    [SerializeField] private Transform target;

    [Header("Chasing")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float stopDistance = 3f;
    [SerializeField] private float introTime = 3f;
    [SerializeField] private float arenaRadius = 18f;

    [Header("Melee Attack")]
    [SerializeField] private float attackRange = 3.5f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackHitDelay = 0.5f;
    [SerializeField] private float attackTime = 1.4f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private AudioClip attackSound;

    [Header("Special Attacks")]
    [SerializeField] private float minAttackDelay = 3f;
    [SerializeField] private float maxAttackDelay = 6f;

    [Header("Hurricane Kick")]
    [SerializeField] private int minDashes = 2;
    [SerializeField] private int maxDashes = 3;
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashOvershoot = 4f;
    [SerializeField] private float maxDashDistance = 22f;
    [SerializeField] private float dashPause = 1f;
    [SerializeField] private float flingRadius = 2.5f;
    [SerializeField] private float flingDamage = 15f;
    [SerializeField] private float flingForce = 14f;
    [SerializeField] private float flingUp = 7f;
    [SerializeField] private AudioClip spinSound;
    [SerializeField] private AudioClip dashSound;

    [Header("Roar")]
    [SerializeField] private UfoMissile ufoPrefab;
    [SerializeField] private Transform ufoSpawnPoint;
    [SerializeField] private int ufoCount = 4;
    [SerializeField] private float ufoDelay = 0.6f;
    [SerializeField] private float ufoGap = 0.2f;
    [SerializeField] private float roarTime = 2.5f;
    [SerializeField] private int minSummons = 1;
    [SerializeField] private int maxSummons = 4;
    [SerializeField] private AudioClip roarSound;

    [Header("Ground Pound")]
    [SerializeField] private float poundWindup = 0.5f;
    [SerializeField] private float jumpTime = 1.1f;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float poundDamage = 40f;
    [SerializeField] private float poundRadius = 4f;
    [SerializeField] private float poundRecover = 1f;
    [SerializeField] private Shockwave shockwavePrefab;
    [SerializeField] private GameObject landingMarker;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip poundSound;

    [Header("Sound")]
    [SerializeField] private AudioSource voice;
    [SerializeField] private AudioSource loopSource;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int SpinHash = Animator.StringToHash("Spin");
    private static readonly int RoarHash = Animator.StringToHash("Roar");
    private static readonly int PoundHash = Animator.StringToHash("Pound");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private Animator anim;
    private EnemyHealth health;
    private PlayerHealth playerHealth;
    private FPSMovement playerMovement;
    private CameraShake cameraShake;
    private PuzzleManager puzzle;
    private Collider[] myColliders;

    private Vector3 arenaCenter;
    private float nextAttackTime;
    private int lastSpecial = -1;
    private bool dead;
    private GameObject marker;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        health = GetComponent<EnemyHealth>();
        myColliders = GetComponentsInChildren<Collider>();
        health.OnDeath += Die;
    }

    private void Start()
    {
        arenaCenter = transform.position;
        cameraShake = FindFirstObjectByType<CameraShake>();

        if (target == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) SetTarget(found.transform);
        }

        BossHealthBar bar = FindFirstObjectByType<BossHealthBar>();
        if (bar != null) bar.Show(health, bossName);

        StartCoroutine(Brain());
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target == null) return;

        playerHealth = target.GetComponent<PlayerHealth>();
        playerMovement = target.GetComponent<FPSMovement>();
    }

    public void SetPuzzle(PuzzleManager newPuzzle)
    {
        puzzle = newPuzzle;
    }

    private IEnumerator Brain()
    {
        yield return Roar(false);

        while (!dead)
        {
            float wait = Random.Range(minAttackDelay, maxAttackDelay);
            float time = 0f;

            while (time < wait)
            {
                if (PlayerInRange(attackRange) && Time.time >= nextAttackTime)
                {
                    yield return MeleeAttack();
                    time += attackTime;
                    continue;
                }

                Chase();
                time += Time.deltaTime;
                yield return null;
            }

            SetRunning(false);

            int special = PickSpecial();
            if (special == 0) yield return HurricaneKick();
            else if (special == 1) yield return Roar(true);
            else yield return GroundPound();
        }
    }

    private int PickSpecial()
    {
        int special = Random.Range(0, 3);
        if (special == lastSpecial) special = (special + Random.Range(1, 3)) % 3;

        lastSpecial = special;
        return special;
    }

    private void Chase()
    {
        if (target == null) return;

        bool far = !PlayerInRange(stopDistance);

        SetRunning(far);
        FaceTowards(target.position);

        if (far) MoveTowards(target.position, moveSpeed);
    }

    private IEnumerator MeleeAttack()
    {
        SetRunning(false);
        FaceTowards(target.position, true);

        if (anim != null) anim.SetTrigger(AttackHash);
        PlaySound(attackSound);

        yield return new WaitForSeconds(attackHitDelay);

        if (playerHealth != null && PlayerInRange(attackRange + 0.5f))
            playerHealth.TakeDamage(attackDamage);

        yield return new WaitForSeconds(Mathf.Max(0f, attackTime - attackHitDelay));

        nextAttackTime = Time.time + attackCooldown;
    }

    private IEnumerator HurricaneKick()
    {
        if (anim != null) anim.SetBool(SpinHash, true);
        PlayLoop(spinSound);

        int dashes = Random.Range(minDashes, maxDashes + 1);

        for (int i = 0; i < dashes && target != null; i++)
        {
            float pause = 0f;
            while (pause < dashPause)
            {
                FaceTowards(target.position);
                pause += Time.deltaTime;
                yield return null;
            }

            yield return Dash();
        }

        if (anim != null) anim.SetBool(SpinHash, false);
        StopLoop();
    }

    private IEnumerator Dash()
    {
        Vector3 toPlayer = Flat(target.position - transform.position);
        if (toPlayer.sqrMagnitude < 0.01f) yield break;

        Vector3 dir = toPlayer.normalized;
        float distance = Mathf.Min(toPlayer.magnitude + dashOvershoot, maxDashDistance);
        Vector3 end = ClampToArena(transform.position + dir * distance);
        distance = Flat(end - transform.position).magnitude;

        transform.rotation = Quaternion.LookRotation(dir);
        PlaySound(dashSound);

        bool flung = false;
        float travelled = 0f;

        while (travelled < distance)
        {
            float step = Mathf.Min(dashSpeed * Time.deltaTime, distance - travelled);
            Vector3 pos = transform.position + dir * step;
            pos.y = GroundHeight(pos);
            transform.position = pos;
            travelled += step;

            if (!flung) flung = TryFling();

            yield return null;
        }
    }

    private bool TryFling()
    {
        if (playerHealth == null || !PlayerInRange(flingRadius)) return false;

        Vector3 away = Flat(target.position - transform.position).normalized;

        playerHealth.TakeDamage(flingDamage);
        if (playerMovement != null)
            playerMovement.Knockback(away * flingForce + Vector3.up * flingUp);
        if (cameraShake != null) cameraShake.Shake(2f, 0.4f);

        return true;
    }

    private IEnumerator Roar(bool summon)
    {
        if (target != null) FaceTowards(target.position, true);

        if (anim != null) anim.SetTrigger(RoarHash);
        PlaySound(roarSound);
        if (cameraShake != null) cameraShake.Shake(1.2f, 1.2f);

        float time = 0f;

        if (summon && target != null)
        {
            yield return new WaitForSeconds(ufoDelay);
            time += ufoDelay;

            if (puzzle != null)
                puzzle.SummonFromPlots(Random.Range(minSummons, maxSummons + 1));

            if (ufoPrefab != null)
            {
                for (int i = 0; i < ufoCount; i++)
                {
                    SpawnUfo(i);
                    yield return new WaitForSeconds(ufoGap);
                    time += ufoGap;
                }
            }
        }

        float wait = summon ? roarTime : introTime;
        if (time < wait) yield return new WaitForSeconds(wait - time);
    }

    private void SpawnUfo(int index)
    {
        float side = (index % 2 == 0 ? 1f : -1f) * (0.6f + index * 0.4f);

        Vector3 basePos = ufoSpawnPoint != null
            ? ufoSpawnPoint.position
            : transform.position - transform.forward * 2f + Vector3.up * 3f;

        Vector3 spawnPos = basePos + transform.right * side;
        Vector3 startDir = -transform.forward + transform.right * (side * 0.6f) + Vector3.up * 1.2f;

        UfoMissile ufo = Instantiate(ufoPrefab, spawnPos, Quaternion.LookRotation(startDir));
        ufo.Launch(target, startDir);
    }

    private IEnumerator GroundPound()
    {
        Vector3 start = transform.position;
        Vector3 land = ClampToArena(target.position);
        land.y = GroundHeight(land);

        FaceTowards(land, true);
        if (anim != null) anim.SetTrigger(PoundHash);

        if (landingMarker != null)
            marker = Instantiate(landingMarker, land + Vector3.up * 0.05f, Quaternion.identity);

        yield return new WaitForSeconds(poundWindup);

        PlaySound(jumpSound);

        float time = 0f;
        while (time < jumpTime)
        {
            float t = time / jumpTime;
            Vector3 pos = Vector3.Lerp(start, land, t);
            pos.y = Mathf.Lerp(start.y, land.y, t) + Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = pos;

            time += Time.deltaTime;
            yield return null;
        }

        transform.position = land;
        Land(land);

        yield return new WaitForSeconds(poundRecover);
    }

    private void Land(Vector3 land)
    {
        if (marker != null) Destroy(marker);

        PlaySound(poundSound);
        if (cameraShake != null) cameraShake.Shake(3f, 0.6f);

        if (playerHealth != null)
        {
            Vector3 toPlayer = Flat(target.position - land);
            if (toPlayer.magnitude <= poundRadius)
            {
                playerHealth.TakeDamage(poundDamage);
                if (playerMovement != null)
                    playerMovement.Knockback(toPlayer.normalized * 10f + Vector3.up * 6f);
            }
        }

        if (shockwavePrefab != null)
            Instantiate(shockwavePrefab, land, Quaternion.identity);
    }

    private void Die()
    {
        dead = true;
        StopAllCoroutines();

        SetRunning(false);
        if (marker != null) Destroy(marker);
        StopLoop();

        if (anim != null)
        {
            anim.SetBool(SpinHash, false);
            if (HasParameter(DieHash)) anim.SetTrigger(DieHash);
        }
    }

    private bool PlayerInRange(float range)
    {
        return target != null && Flat(target.position - transform.position).magnitude <= range;
    }

    private Vector3 ClampToArena(Vector3 point)
    {
        Vector3 fromCenter = Flat(point - arenaCenter);
        if (fromCenter.magnitude <= arenaRadius) return point;

        Vector3 clamped = arenaCenter + fromCenter.normalized * arenaRadius;
        clamped.y = point.y;
        return clamped;
    }

    private void MoveTowards(Vector3 point, float speed)
    {
        Vector3 toPoint = Flat(point - transform.position);
        float step = speed * Time.deltaTime;

        Vector3 pos = toPoint.magnitude <= step ? point : transform.position + toPoint.normalized * step;
        pos.y = GroundHeight(pos);
        transform.position = pos;
    }

    private void FaceTowards(Vector3 point, bool instant = false)
    {
        Vector3 dir = Flat(point - transform.position);
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = instant ? look : Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    private float GroundHeight(Vector3 pos)
    {
        RaycastHit[] hits = Physics.RaycastAll(pos + Vector3.up * 10f, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MinValue;

        foreach (RaycastHit hit in hits)
        {
            if (IsMine(hit.collider)) continue;
            if (hit.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (hit.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (hit.point.y > best) best = hit.point.y;
        }

        return best == float.MinValue ? transform.position.y : best;
    }

    private bool IsMine(Collider col)
    {
        foreach (Collider mine in myColliders)
            if (mine == col) return true;
        return false;
    }

    private bool HasParameter(int hash)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }

    private void SetRunning(bool running)
    {
        if (anim != null) anim.SetBool(IsRunningHash, running);
    }

    private void PlaySound(AudioClip clip)
    {
        if (voice != null && clip != null) voice.PlayOneShot(clip);
    }

    private void PlayLoop(AudioClip clip)
    {
        if (loopSource == null || clip == null) return;

        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.Play();
    }

    private void StopLoop()
    {
        if (loopSource != null) loopSource.Stop();
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
