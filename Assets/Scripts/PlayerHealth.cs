using System;
using UnityEngine;
using UnityEngine.UI;


public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invulnerableTime = 0.5f;
    [SerializeField] private bool freezeOnDeath = true;

    [Header("UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image damageFlash;
    [SerializeField] private float flashAlpha = 0.4f;
    [SerializeField] private float flashFadeSpeed = 1.5f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private float _nextDamageTime;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        if (healthBarFill != null) healthBarFill.fillAmount = 1f;
        if (damageFlash != null)
        {
            damageFlash.raycastTarget = false;
            SetFlashAlpha(0f);
        }
    }

    private void Update()
    {
        if (damageFlash != null && damageFlash.color.a > 0f)
            SetFlashAlpha(Mathf.MoveTowards(damageFlash.color.a, 0f, flashFadeSpeed * Time.deltaTime));
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (Time.time < _nextDamageTime) return;

        _nextDamageTime = Time.time + invulnerableTime;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (healthBarFill != null) healthBarFill.fillAmount = CurrentHealth / maxHealth;
        if (damageFlash != null) SetFlashAlpha(flashAlpha);

        if (CurrentHealth <= 0f)
            Die();
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
        Debug.Log("Player died");
        OnDeath?.Invoke();

        if (freezeOnDeath && TryGetComponent(out FPSMovement movement))
            movement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = IsDead ? 0f : 1f;
    }
}