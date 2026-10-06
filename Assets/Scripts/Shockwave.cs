using UnityEngine;

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

    [Header("Model")]
    [SerializeField] private Transform ringModel;
    [SerializeField] private float modelRadius = 0.5f;

    private LineRenderer line;
    private float radius;
    private bool hasHit;
    private PlayerHealth playerHealth;
    private FPSMovement playerMovement;
    private CharacterController playerBody;
    private float groundY;
    private Renderer[] modelParts;
    private Color[] modelColors;
    private MaterialPropertyBlock block;
    private Vector3 modelScale;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        line = GetComponent<LineRenderer>();

        if (ringModel != null && line != null)
            line.enabled = false;

        if (line != null && line.enabled)
        {
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = points;
            line.widthMultiplier = ringWidth;

            if (line.sharedMaterial == null)
                line.material = new Material(Shader.Find("Sprites/Default"));
        }

        if (ringModel != null)
        {
            block = new MaterialPropertyBlock();
            modelParts = ringModel.GetComponentsInChildren<Renderer>();
            modelColors = new Color[modelParts.Length];

            for (int i = 0; i < modelParts.Length; i++)
            {
                Material mat = modelParts[i].sharedMaterial;
                modelColors[i] = mat != null && mat.HasProperty(BaseColor) ? mat.GetColor(BaseColor) : Color.white;
            }

            modelScale = ringModel.localScale;
            ringModel.localScale = new Vector3(0f, modelScale.y, 0f);
        }

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

        float fade = 1f - radius / maxRadius;

        if (ringModel != null) UpdateModel(fade);
        else if (line != null) DrawRing(fade);

        CheckPlayer();
    }

    private void UpdateModel(float fade)
    {
        float size = radius / modelRadius;
        ringModel.localScale = new Vector3(modelScale.x * size, modelScale.y, modelScale.z * size);

        for (int i = 0; i < modelParts.Length; i++)
        {
            Color color = modelColors[i];
            color.a *= fade;

            modelParts[i].GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            modelParts[i].SetPropertyBlock(block);
        }
    }

    private void DrawRing(float fade)
    {
        Color color = line.startColor;
        color.a = fade;
        line.startColor = color;
        line.endColor = color;

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
