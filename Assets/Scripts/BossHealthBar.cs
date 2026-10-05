using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject bar;
    [SerializeField] private Image fill;
    [SerializeField] private Image delayedFill;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private float delayedSpeed = 0.5f;

    private EnemyHealth boss;

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
        fill.fillAmount = 1f;
        if (delayedFill != null) delayedFill.fillAmount = 1f;

        bar.SetActive(true);
    }

    private void Update()
    {
        if (delayedFill == null || !bar.activeSelf) return;

        if (delayedFill.fillAmount > fill.fillAmount)
            delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount, fill.fillAmount, delayedSpeed * Time.deltaTime);
    }

    private void UpdateBar(float current, float max)
    {
        fill.fillAmount = current / max;
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
}
