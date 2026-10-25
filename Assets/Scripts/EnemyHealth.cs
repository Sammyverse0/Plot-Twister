using System;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool isHeavy;

    [Header("Damage")]
    [SerializeField] private bool headshotKills = true;
    [SerializeField] private float headshotMultiplier = 2f;
    [SerializeField] private float bodyMultiplier = 1f;
    [SerializeField] private float legMultiplier = 1f;

    [Header("Death")]
    [SerializeField] private float deathTime = 3f;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float hitEffectLifetime = 2f;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private Transform deathEffectPoint;
    [SerializeField] private Vector3 deathEffectOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float deathEffectLifetime = 4f;

    [Header("Floating Health Bar")]
    [SerializeField] private Transform healthBarRoot;
    [SerializeField] private Image healthBarFill;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => _dead;
    public bool IsHeavy => isHeavy;
    public GameObject HitEffect => hitEffect;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private bool _dead;
    private Camera _cam;
    private AlienFollower _follower;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        if (healthBarFill != null) healthBarFill.fillAmount = 1f;
        _follower = GetComponent<AlienFollower>();
    }

    private void Start()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (healthBarRoot != null && _cam != null)
            healthBarRoot.rotation = _cam.transform.rotation;
    }

    public void TakeDamage(float amount)
    {
        TakeHit(amount, BodyPart.Body);
    }

    public void TakeHit(float amount, BodyPart part)
    {
        TakeHit(amount, part, transform.position + Vector3.up, Vector3.up);
    }

    public void TakeHit(float amount, BodyPart part, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_dead) return;
        if (_follower != null && !_follower.IsAwake) return;

        SpawnHitEffect(hitPoint, hitNormal);

        if (_follower != null && _follower.IsCrawling)
        {
            KillOutright();
            return;
        }

        if (part == BodyPart.Head && headshotKills)
            amount = CurrentHealth;

        float multiplier = part switch
        {
            BodyPart.Head => headshotKills ? 1f : headshotMultiplier,
            BodyPart.Legs => legMultiplier,
            _ => bodyMultiplier
        };

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount * multiplier);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (healthBarFill != null) healthBarFill.fillAmount = CurrentHealth / maxHealth;

        if (CurrentHealth <= 0f)
        {
            Die();
            return;
        }

        if (_follower == null) return;

        if (part == BodyPart.Legs && _follower.CanCrawl)
            _follower.StartCrawling();
        else
            _follower.GetHit();
    }

    private void SpawnHitEffect(Vector3 point, Vector3 normal)
    {
        if (hitEffect == null) return;

        Quaternion rotation = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        GameObject fx = Instantiate(hitEffect, point, rotation);
        Destroy(fx, hitEffectLifetime);
    }

    private void SpawnDeathEffect()
    {
        if (deathEffect == null) return;

        Vector3 position = deathEffectPoint != null
            ? deathEffectPoint.position
            : transform.position + deathEffectOffset;

        GameObject fx = Instantiate(deathEffect, position, Quaternion.identity);
        Destroy(fx, deathEffectLifetime);
    }

    private void KillOutright()
    {
        CurrentHealth = 0f;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (healthBarFill != null) healthBarFill.fillAmount = 0f;

        Die();
    }

    private void Die()
    {
        _dead = true;
        OnDeath?.Invoke();

        SpawnDeathEffect();

        if (healthBarRoot != null) healthBarRoot.gameObject.SetActive(false);

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (_follower != null) _follower.Die();

        Destroy(gameObject, deathTime);
    }
}