using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float touchDamage = 10f;
    [SerializeField] private float touchCooldown = 1.2f;

    [Header("Attacks")]
    [SerializeField] private float minAttackDelay = 3f;
    [SerializeField] private float maxAttackDelay = 6f;

    [Header("Hurricane Kick")]
    [SerializeField] private float spinTime = 5f;
    [SerializeField] private float spinSpeed = 10f;
    [SerializeField] private float roamRadius = 5f;
    [SerializeField] private float arenaRadius = 18f;
    [SerializeField] private float newPointEvery = 0.6f;
    [SerializeField] private float flingRadius = 2.5f;
    [SerializeField] private float flingDamage = 15f;
    [SerializeField] private float flingForce = 14f;
    [SerializeField] private float flingUp = 7f;
    [SerializeField] private GameObject tornadoEffect;
    [SerializeField] private AudioClip spinSound;

    [Header("Roar")]
    [SerializeField] private UfoMissile ufoPrefab;
    [SerializeField] private Transform ufoSpawnPoint;
    [SerializeField] private int ufoCount = 4;
    [SerializeField] private float ufoDelay = 0.6f;
    [SerializeField] private float ufoGap = 0.2f;
    [SerializeField] private float roarTime = 2.5f;
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
    [SerializeField] private AudioClip poundSound;

    [Header("Minions")]
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private float minionInterval = 20f;
    [SerializeField] private float minionChance = 0.6f;
    [SerializeField] private int minionsPerWave = 2;
    [SerializeField] private int maxMinions = 4;

    [Header("Sound")]
    [SerializeField] private AudioSource voice;
    [SerializeField] private AudioSource loopSource;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int SpinHash = Animator.StringToHash("Spin");
    private static readonly int RoarHash = Animator.StringToHash("Roar");
    private static readonly int PoundHash = Animator.StringToHash("Pound");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private Animator anim;
    private EnemyHealth health;
    private PlayerHealth playerHealth;
    private FPSMovement playerMovement;
    private CameraShake cameraShake;
    private Collider[] myColliders;
    private readonly List<GameObject> minions = new();

    private Vector3 arenaCenter;
    private float nextTouchTime;
    private float nextFlingTime;
    private float nextMinionTime;
    private int lastAttack = -1;
    private bool busy;
    private bool dead;
    private GameObject marker;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        health = GetComponent<EnemyHealth>();
        myColliders = GetComponentsInChildren<Collider>();
        health.OnDeath += Die;

        if (tornadoEffect != null) tornadoEffect.SetActive(false);
    }

    private void Start()
    {
        arenaCenter = transform.position;
        nextMinionTime = Time.time + introTime + minionInterval;
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

    private IEnumerator Brain()
    {
        yield return Roar(false);

        while (!dead)
        {
            float wait = Random.Range(minAttackDelay, maxAttackDelay);
            float time = 0f;

            while (time < wait)
            {
                Chase();
                time += Time.deltaTime;
                yield return null;
            }

            SetRunning(false);
            busy = true;

            int attack = PickAttack();
            if (attack == 0) yield return HurricaneKick();
            else if (attack == 1) yield return Roar(true);
            else yield return GroundPound();

            busy = false;
        }
    }

    private int PickAttack()
    {
        int attack = Random.Range(0, 3);
        if (attack == lastAttack) attack = (attack + Random.Range(1, 3)) % 3;

        lastAttack = attack;
        return attack;
    }

    private void Update()
    {
        if (dead || target == null) return;

        if (!busy && Time.time >= nextMinionTime)
        {
            nextMinionTime = Time.time + minionInterval;
            if (Random.value <= minionChance) SpawnMinions();
        }
    }

    private void Chase()
    {
        if (target == null) return;

        Vector3 toPlayer = Flat(target.position - transform.position);
        bool far = toPlayer.magnitude > stopDistance;

        SetRunning(far);
        FaceTowards(target.position);

        if (far)
            MoveTowards(target.position, moveSpeed);
        else
            TryTouchDamage();
    }

    private void TryTouchDamage()
    {
        if (playerHealth == null || Time.time < nextTouchTime) return;

        playerHealth.TakeDamage(touchDamage);
        nextTouchTime = Time.time + touchCooldown;
    }

    private IEnumerator HurricaneKick()
    {
        if (anim != null) anim.SetBool(SpinHash, true);
        if (tornadoEffect != null) tornadoEffect.SetActive(true);
        PlayLoop(spinSound);

        float time = 0f;
        float nextPoint = 0f;
        Vector3 point = transform.position;

        while (time < spinTime)
        {
            if (time >= nextPoint)
            {
                point = PickRoamPoint();
                nextPoint = time + newPointEvery;
            }

            MoveTowards(point, spinSpeed);
            TryFling();

            time += Time.deltaTime;
            yield return null;
        }

        if (anim != null) anim.SetBool(SpinHash, false);
        if (tornadoEffect != null) tornadoEffect.SetActive(false);
        StopLoop();
    }

    private Vector3 PickRoamPoint()
    {
        Vector2 random = Random.insideUnitCircle * roamRadius;
        Vector3 point = target.position + new Vector3(random.x, 0f, random.y);

        Vector3 fromCenter = Flat(point - arenaCenter);
        if (fromCenter.magnitude > arenaRadius)
            point = arenaCenter + fromCenter.normalized * arenaRadius;

        return point;
    }

    private void TryFling()
    {
        if (playerHealth == null || Time.time < nextFlingTime) return;

        Vector3 toPlayer = Flat(target.position - transform.position);
        if (toPlayer.magnitude > flingRadius) return;

        playerHealth.TakeDamage(flingDamage);

        if (playerMovement != null)
            playerMovement.Knockback(toPlayer.normalized * flingForce + Vector3.up * flingUp);

        if (cameraShake != null) cameraShake.Shake(2f, 0.4f);

        nextFlingTime = Time.time + 1f;
    }

    private IEnumerator Roar(bool summonUfos)
    {
        FaceTowards(target != null ? target.position : transform.position + transform.forward, true);

        if (anim != null) anim.SetTrigger(RoarHash);
        PlaySound(roarSound);
        if (cameraShake != null) cameraShake.Shake(1.2f, 1.2f);

        float time = 0f;

        if (summonUfos && ufoPrefab != null && target != null)
        {
            yield return new WaitForSeconds(ufoDelay);
            time += ufoDelay;

            for (int i = 0; i < ufoCount; i++)
            {
                SpawnUfo(i);
                yield return new WaitForSeconds(ufoGap);
                time += ufoGap;
            }
        }

        float wait = summonUfos ? roarTime : introTime;
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
        Vector3 land = target.position;
        land.y = GroundHeight(land);

        FaceTowards(land, true);
        if (anim != null) anim.SetTrigger(PoundHash);

        if (landingMarker != null)
            marker = Instantiate(landingMarker, land + Vector3.up * 0.05f, Quaternion.identity);

        yield return new WaitForSeconds(poundWindup);

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

    private void SpawnMinions()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;

        minions.RemoveAll(m => m == null);

        for (int i = 0; i < minionsPerWave && minions.Count < maxMinions; i++)
        {
            Vector2 random = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
            Vector3 pos = transform.position + new Vector3(random.x, 0f, random.y);
            pos.y = GroundHeight(pos);

            GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            GameObject minion = Instantiate(prefab, pos, Quaternion.identity);

            if (!minion.TryGetComponent(out AlienFollower follower))
                follower = minion.AddComponent<AlienFollower>();

            follower.SetTarget(target);
            minions.Add(minion);
        }
    }

    private void Die()
    {
        dead = true;
        StopAllCoroutines();

        SetRunning(false);
        if (tornadoEffect != null) tornadoEffect.SetActive(false);
        if (marker != null) Destroy(marker);
        StopLoop();

        if (anim != null)
        {
            anim.SetBool(SpinHash, false);
            if (HasParameter(DieHash)) anim.SetTrigger(DieHash);
        }

        foreach (GameObject minion in minions)
            if (minion != null) Destroy(minion);
        minions.Clear();
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
