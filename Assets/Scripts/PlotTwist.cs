using System.Collections;
using UnityEngine;

public class PlotSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float riseHeight = 1.5f;
    [SerializeField] private float riseDuration = 0.8f;
    [SerializeField] private float twistAngle = 60f;
    [SerializeField] private float wobbleAngle = 8f;
    [SerializeField] private int enemyCount = 3;
    [SerializeField] private float spawnInterval = 0.35f;
    [SerializeField] private float spawnRadius = 1.5f;
    [SerializeField] private bool returnToGround = true;

    private bool triggered;
    private Vector3 startPos;
    private Quaternion startRot;

    private void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag(playerTag)) return;
        triggered = true;
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        yield return Animate(0f, 1f);

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }

        if (returnToGround)
        {
            yield return Animate(1f, 0f);
        }
    }

    private IEnumerator Animate(float from, float to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / riseDuration;
            float eased = Mathf.SmoothStep(from, to, Mathf.Clamp01(t));
            float wobble = Mathf.Sin(t * Mathf.PI * 2f) * wobbleAngle;

            transform.position = startPos + Vector3.up * (riseHeight * eased);
            transform.rotation = startRot * Quaternion.Euler(0f, twistAngle * eased, wobble * eased);
            yield return null;
        }

        transform.position = startPos + Vector3.up * (riseHeight * to);
        transform.rotation = startRot * Quaternion.Euler(0f, twistAngle * to, 0f);
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = origin + new Vector3(offset.x, 0f, offset.y);

        Instantiate(prefab, pos, Quaternion.identity);
    }
}