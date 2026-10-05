using TMPro;
using UnityEngine;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject bar;
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform delayedFill;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private float delayedSpeed = 0.5f;

    private EnemyHealth boss;
    private float health = 1f;
    private float delayedHealth = 1f;

    private void Awake()
    {
        bar.SetActive(false);
    }

    public void Show(EnemyHealth newBoss, string bossName)
    {
        boss = newBoss;
        boss.OnHealthChanged += UpdateBar;
        boss.OnDeath += Hide;

        if (nameText != null) nameText.text = bossName;

        health = 1f;
        delayedHealth = 1f;
        SetWidth(fill, health);
        SetWidth(delayedFill, delayedHealth);

        bar.SetActive(true);
    }

    private void Update()
    {
        if (bar.activeSelf && boss == null)
        {
            bar.SetActive(false);
            return;
        }

        if (!bar.activeSelf || delayedHealth <= health) return;

        delayedHealth = Mathf.MoveTowards(delayedHealth, health, delayedSpeed * Time.deltaTime);
        SetWidth(delayedFill, delayedHealth);
    }

    private void UpdateBar(float current, float max)
    {
        health = current / max;
        SetWidth(fill, health);
    }

    private void SetWidth(RectTransform rect, float amount)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Hide()
    {
        if (boss != null)
        {
            boss.OnHealthChanged -= UpdateBar;
            boss.OnDeath -= Hide;
        }

        bar.SetActive(false);
    }

    private void OnDestroy()
    {
        if (boss != null)
        {
            boss.OnHealthChanged -= UpdateBar;
            boss.OnDeath -= Hide;
        }
    }
}
