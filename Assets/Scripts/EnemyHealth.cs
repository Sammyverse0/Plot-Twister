using System;
using UnityEngine;

// Put this on the alien prefab. Bullets call TakeDamage() on it.
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;

    public event Action<float, float> OnHealthChanged; // (current, max) - handy for a health bar later
    public event Action OnDeath;

    private bool _dead;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (_dead) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        _dead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}