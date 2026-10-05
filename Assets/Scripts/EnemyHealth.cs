using System;
using UnityEngine;
using UnityEngine.UI;


public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private bool headshotKills = true;
    [SerializeField] private float headshotMultiplier = 2f;
    [SerializeField] private float bodyMultiplier = 1f;
    [SerializeField] private float legMultiplier = 1f;

    [Header("Death")]
    [SerializeField] private float deathTime = 3f;

    [Header("Floating Health Bar")]
    [SerializeField] private Transform healthBarRoot;
    [SerializeField] private Image healthBarFill;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;

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
        if (_dead) return;
        if (_follower != null && !_follower.IsAwake) return;

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

        if (part == BodyPart.Legs)
            _follower.StartCrawling();
        else
            _follower.GetHit();
    }

    private void Die()
    {
        _dead = true;
        OnDeath?.Invoke();

        if (healthBarRoot != null) healthBarRoot.gameObject.SetActive(false);

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (_follower != null) _follower.Die();

        Destroy(gameObject, deathTime);
    }
}
