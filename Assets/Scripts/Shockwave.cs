using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Shockwave : MonoBehaviour
{
    [SerializeField] private float maxRadius = 14f;
    [SerializeField] private float growSpeed = 9f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float knockUp = 5f;
    [SerializeField] private float knockBack = 8f;
    [SerializeField] private float ringWidth = 0.6f;
    [SerializeField] private float jumpClearance = 0.4f;
    [SerializeField] private int points = 64;

    private LineRenderer line;
    private float radius;
    private bool hasHit;
    private PlayerHealth playerHealth;
    private FPSMovement playerMovement;
    private CharacterController playerBody;
    private float groundY;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = points;
        line.widthMultiplier = ringWidth;

        if (line.sharedMaterial == null)
            line.material = new Material(Shader.Find("Sprites/Default"));

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerMovement = playerHealth.GetComponent<FPSMovement>();
            playerBody = playerHealth.GetComponent<CharacterController>();
        }

        groundY = transform.position.y;
    }

    private void Update()
    {
        radius += growSpeed * Time.deltaTime;

        if (radius >= maxRadius)
        {
            Destroy(gameObject);
            return;
        }

        DrawRing();
        CheckPlayer();

        Color color = line.startColor;
        color.a = 1f - radius / maxRadius;
        line.startColor = color;
        line.endColor = color;
    }

    private void DrawRing()
    {
        Vector3 center = transform.position;

        for (int i = 0; i < points; i++)
        {
            float angle = i / (float)points * Mathf.PI * 2f;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            pos.y = groundY + 0.1f;
            line.SetPosition(i, pos);
        }
    }

    private void CheckPlayer()
    {
        if (hasHit || playerHealth == null || playerHealth.IsDead) return;

        Vector3 toPlayer = playerHealth.transform.position - transform.position;
        float feet = playerBody != null ? playerBody.bounds.min.y : playerHealth.transform.position.y;
        float feetHeight = feet - groundY;
        toPlayer.y = 0f;

        bool ringOnPlayer = Mathf.Abs(toPlayer.magnitude - radius) < ringWidth;
        bool jumpedOver = playerMovement != null && !playerMovement.IsGrounded && feetHeight > jumpClearance;

        if (!ringOnPlayer || jumpedOver) return;

        hasHit = true;
        playerHealth.TakeDamage(damage);

        if (playerMovement != null)
            playerMovement.Knockback(toPlayer.normalized * knockBack + Vector3.up * knockUp);
    }
}
