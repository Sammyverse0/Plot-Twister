using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AlienTier
{
    public GameObject[] alienPrefabs;
    public float spawnInterval = 4f;
    public int maxAlive = 5;
}

public class AlienSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Settings")]
    [SerializeField] private AlienTier[] tiers;
    [SerializeField] private float minSpawnDistanceFromPlayer = 8f;

    private readonly List<GameObject> aliens = new List<GameObject>();
    private int currentTier;
    private bool spawning;
    private float timer;

    public int AliveCount => aliens.Count;
    public int AliveBasics { get; private set; }
    public int AliveHeavies { get; private set; }

    public void SetTier(int tier)
    {
        currentTier = Mathf.Clamp(tier, 0, tiers.Length - 1);
        spawning = true;
        timer = 0f;
    }

    public void StopSpawning()
    {
        spawning = false;
    }

    private void Update()
    {
        if (!spawning) return;

        AlienTier tier = tiers[currentTier];
        timer += Time.deltaTime;

        if (timer < tier.spawnInterval) return;
        timer = 0f;

        aliens.RemoveAll(a => a == null);
        if (aliens.Count >= tier.maxAlive) return;
        if (tier.alienPrefabs.Length == 0) return;

        GameObject prefab = tier.alienPrefabs[Random.Range(0, tier.alienPrefabs.Length)];

        if (TryGetSpawnPosition(out Vector3 position))
        {
            SpawnEnemy(prefab, position);
        }
    }

    private bool TryGetSpawnPosition(out Vector3 result)
    {
        List<Vector3> valid = new List<Vector3>();

        foreach (Transform point in spawnPoints)
        {
            if (Vector3.Distance(point.position, player.position) >= minSpawnDistanceFromPlayer)
            {
                valid.Add(point.position);
            }
        }

        if (valid.Count == 0)
        {
            result = Vector3.zero;
            return false;
        }

        result = valid[Random.Range(0, valid.Count)];
        return true;
    }

    private void SpawnEnemy(GameObject prefab, Vector3 pos)
    {
        GameObject alien = Instantiate(prefab, pos, Quaternion.identity);
        aliens.Add(alien);

        if (!alien.TryGetComponent(out AlienFollower follower))
            follower = alien.AddComponent<AlienFollower>();

        follower.SetTarget(player);

        if (!alien.TryGetComponent(out EnemyHealth health))
        {
            Debug.LogWarning($"{alien.name} has no EnemyHealth, so it can't be counted.");
            return;
        }

        bool isHeavy = health.IsHeavy || prefab.name.Contains("Heavy");

        if (isHeavy)
        {
            AliveHeavies++;
        }
        else
        {
            AliveBasics++;
        }

        health.OnDeath += () => HandleAlienDeath(alien, isHeavy);
    }

    private void HandleAlienDeath(GameObject alien, bool isHeavy)
    {
        aliens.Remove(alien);

        if (isHeavy)
        {
            AliveHeavies--;
        }
        else
        {
            AliveBasics--;
        }
    }
}