using UnityEngine;

public enum BodyPart { Head, Body, Legs }

public class HitZone : MonoBehaviour
{
    public BodyPart part = BodyPart.Body;

    private EnemyHealth health;

    private void Awake()
    {
        health = GetComponentInParent<EnemyHealth>();
    }

    public void Hit(float damage)
    {
        if (health != null)
            health.TakeHit(damage, part);
    }
}
