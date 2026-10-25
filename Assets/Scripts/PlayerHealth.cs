using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invulnerableTime = 0.5f;
    [SerializeField] private bool freezeOnDeath = true;

    [Header("Sound")]
    [SerializeField] private AudioSource deathSource;
    [SerializeField] private AudioClip deathSound;

    [Header("UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthIcon;
    [SerializeField] private Image damageFlash;
    [SerializeField] private float flashAlpha = 0.4f;
    [SerializeField] private float flashFadeSpeed = 1.5f;

    [Header("Bar Colors")]
    [SerializeField] private Color fullColor = new Color(0.25f, 0.75f, 0.1f);
    [SerializeField] private Color midColor = new Color(0.9f, 0.7f, 0.05f);
    [SerializeField] private Color lowColor = new Color(0.8f, 0.1f, 0.05f);

    [Header("Icon Punch")]
    [SerializeField] private float iconPunch = 1.35f;
    [SerializeField] private float iconSettleSpeed = 1.5f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private float _nextDamageTime;
    private float _iconScale = 1f;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        RefreshBar();

        if (damageFlash != null)
        {
            damageFlash.raycastTarget = false;
            SetFlashAlpha(0f);
        }
    }

    private void Update()
    {
        if (damageFlash != null && damageFlash.color.a > 0f)
            SetFlashAlpha(Mathf.MoveTowards(damageFlash.color.a, 0f, flashFadeSpeed * Time.unscaledDeltaTime));

        if (healthIcon != null && _iconScale > 1f)
        {
            _iconScale = Mathf.MoveTowards(_iconScale, 1f, iconSettleSpeed * Time.unscaledDeltaTime);
            healthIcon.rectTransform.localScale = Vector3.one * _iconScale;
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (Time.time < _nextDamageTime) return;

        _nextDamageTime = Time.time + invulnerableTime;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        RefreshBar();
        _iconScale = iconPunch;

        if (damageFlash != null) SetFlashAlpha(flashAlpha);

        if (CurrentHealth <= 0f)
            Die();
    }

    public void ResetHealth()
    {
        IsDead = false;
        CurrentHealth = maxHealth;
        _nextDamageTime = 0f;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        RefreshBar();
        _iconScale = 1f;
        if (healthIcon != null) healthIcon.rectTransform.localScale = Vector3.one;

        if (damageFlash != null) SetFlashAlpha(0f);
        if (TryGetComponent(out FPSMovement movement)) movement.enabled = true;
    }

    private void RefreshBar()
    {
        if (healthBarFill == null) return;

        float t = CurrentHealth / maxHealth;
        healthBarFill.fillAmount = t;
        healthBarFill.color = t > 0.5f
            ? Color.Lerp(midColor, fullColor, (t - 0.5f) * 2f)
            : Color.Lerp(lowColor, midColor, t * 2f);
    }

    private void SetFlashAlpha(float a)
    {
        Color c = damageFlash.color;
        c.a = a;
        damageFlash.color = c;
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log("Player died, health instance " + GetInstanceID() + " frame " + Time.frameCount);

        if (deathSource != null && deathSound != null)
        {
            deathSource.Stop();
            deathSource.PlayOneShot(deathSound);
        }

        if (freezeOnDeath && TryGetComponent(out FPSMovement movement))
            movement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnDeath?.Invoke();
    }
}