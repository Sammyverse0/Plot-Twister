using System;
using UnityEngine;
using UnityEngine.UI;


public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    [Header("Floating Health Bar")]
    [SerializeField] private Transform healthBarRoot;
    [SerializeField] private Image healthBarFill;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private bool _dead;
    private Camera _cam;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        if (healthBarFill != null) healthBarFill.fillAmount = 1f;
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
        if (_dead) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (healthBarFill != null) healthBarFill.fillAmount = CurrentHealth / maxHealth;

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