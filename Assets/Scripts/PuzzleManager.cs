using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
public enum Direction { North, East, South, West }

[System.Serializable]
public class LevelData
{
    public int gridSize = 2;
    public GameObject[] alienPrefabs;
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public GameObject reinforcementPrefab;
    public float reinforcementInterval = 6f;
}

public class PuzzleManager : MonoBehaviour
{
    [Header("Level 1 Intro Panel")]
    [SerializeField] private GameObject level1IntroPanel;
    public const string SaveKey = "SavedLevel";
    [Header("Ending Cutscene")]
    [SerializeField] private VideoPlayer endingCutscenePlayer;
    [SerializeField] private GameObject endingCutscenePanel;
    [Header("Combat Music")]
    [SerializeField] private AudioSource combatMusicSource;
    [SerializeField] private AudioClip combatMusic;
    [Header("Levels")]
    [SerializeField] private LevelData[] levels;

    [Header("Grid")]
    [SerializeField] private Vector2 plotSpacing = new Vector2(2f, 2f);
    [SerializeField] private Transform plotParent;
    [SerializeField] private GameObject plotPrefab;

    [Header("UI")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private GridLayoutGroup tileGrid;
    [SerializeField] private RectTransform tileImagePrefab;
    [SerializeField] private Sprite straightSprite;
    [SerializeField] private Sprite elbowSprite;

    [Header("Win Condition")]
    [SerializeField] private Direction entryDirection = Direction.West;
    [SerializeField] private Direction exitDirection = Direction.East;

    [Header("Aliens")]
    [SerializeField] private Transform player;
    [SerializeField] private int maxAliveEnemies = 10;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public event System.Action<int> OnLevelStarted;
    public event System.Action<int> OnLevelCompleted;
    public event System.Action OnAllLevelsCompleted;

    private readonly List<Plot> _plots = new();
    private readonly List<RectTransform> _tileImages = new();
    private readonly List<GameObject> _aliens = new();
    private int gridSize;
    private int _currentLevel;
    private int _aliveAliens;
    private int _aliveHeavies;
    private float _nextReinforcement;
    private bool _solved;

    private PlayerHealth _playerHealth;
    private CharacterController _playerController;
    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private static readonly int[] StraightBase = { (int)Direction.East, (int)Direction.West };
    private static readonly int[] ElbowBase = { (int)Direction.East, (int)Direction.South };

    private void Start()
    {
        ResolvePlayer();

        if (player != null)
        {
            _startPosition = player.position;
            _startRotation = player.rotation;
            player.TryGetComponent(out _playerController);
            player.TryGetComponent(out _playerHealth);
        }

        if (endingCutscenePanel != null)
            endingCutscenePanel.SetActive(false);
        BuildLevel(Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 0), 0, levels.Length - 1));

        if (level1IntroPanel != null && _currentLevel == 0)
        {
            level1IntroPanel.SetActive(true);

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (level1IntroPanel != null)
        {
            level1IntroPanel.SetActive(false);
        }
    }

    public bool HasNextLevel => _currentLevel + 1 < levels.Length;

    private void ResolvePlayer()
    {
        if (player != null) return;

        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null) player = found.transform;
        else Debug.LogWarning("No player assigned and no object tagged 'Player' found.");
    }

    public void BuildLevel(int index)
    {
        ClearLevel();
        ResetPlayer();

        _currentLevel = index;
        PlayerPrefs.SetInt(SaveKey, index);
        PlayerPrefs.Save();
        gridSize = levels[index].gridSize;

        tileGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        tileGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        tileGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        tileGrid.constraintCount = gridSize;
        tileGrid.childAlignment = TextAnchor.MiddleCenter;

        RectTransform gridRect = tileGrid.GetComponent<RectTransform>();
        float cellWidth = (gridRect.rect.width - tileGrid.spacing.x * (gridSize - 1)) / gridSize;
        float cellHeight = (gridRect.rect.height - tileGrid.spacing.y * (gridSize - 1)) / gridSize;
        float cell = Mathf.Min(cellWidth, cellHeight);
        tileGrid.cellSize = new Vector2(cell, cell);

        Dictionary<Vector2Int, (PipeShape shape, int rotation)> solution = GenerateSolvablePath();

        if (debugLogs && solution != null)
        {
            foreach (var kvp in solution)
                Debug.Log($"[Solution] cell {kvp.Key}: shape={kvp.Value.shape}, needs rotationState={kvp.Value.rotation}");
        }

        for (int i = 0; i < gridSize * gridSize; i++)
        {
            int x = i % gridSize;
            int row = i / gridSize;
            int z = gridSize - 1 - row;
            Vector2Int gridPos = new Vector2Int(x, row);

            Vector3 worldPos = plotParent.position + new Vector3(x * plotSpacing.x, 0f, z * plotSpacing.y);
            Plot plot = Instantiate(plotPrefab, worldPos, Quaternion.identity, plotParent).GetComponent<Plot>();
            plot.manager = this;

            if (solution != null && solution.TryGetValue(gridPos, out var required))
                plot.shape = required.shape;
            else
                plot.shape = (PipeShape)Random.Range(0, 2);

            plot.SetInitialRotation(Random.Range(0, 4));
            _plots.Add(plot);

            RectTransform tile = Instantiate(tileImagePrefab, tileGrid.transform);
            tile.GetComponent<Image>().sprite = plot.shape == PipeShape.Straight ? straightSprite : elbowSprite;
            _tileImages.Add(tile);
        }

        if (solution != null)
        {
            var pathPlots = new List<Plot>();
            foreach (var pos in solution.Keys) pathPlots.Add(_plots[pos.y * gridSize + pos.x]);

            for (int guard = 0; guard < 50 && CheckSolved(false); guard++)
            {
                Plot p = pathPlots[Random.Range(0, pathPlots.Count)];
                p.SetInitialRotation((p.rotationState + 1) % 4);
            }
        }

        SyncTiles();
        OnLevelStarted?.Invoke(_currentLevel);
    }

    private void ClearLevel()
    {
        foreach (Plot plot in _plots)
            if (plot != null) Destroy(plot.gameObject);
        foreach (RectTransform tile in _tileImages)
            if (tile != null) Destroy(tile.gameObject);
        foreach (GameObject alien in _aliens)
            if (alien != null) Destroy(alien);

        _plots.Clear();
        _tileImages.Clear();
        _aliens.Clear();
        _aliveAliens = 0;
        _aliveHeavies = 0;
        _solved = false;
    }

    private void ResetPlayer()
    {
        if (player == null) return;

        if (_playerController != null) _playerController.enabled = false;
        player.SetPositionAndRotation(_startPosition, _startRotation);
        if (_playerController != null) _playerController.enabled = true;
    }

    private Dictionary<Vector2Int, (PipeShape, int)> GenerateSolvablePath()
    {
        Vector2Int exitCell = new Vector2Int(gridSize - 1, gridSize - 1);

        for (int attempt = 0; attempt < 200; attempt++)
        {
            var result = TryWalkPath(exitCell);
            if (result != null) return result;
        }

        Debug.LogWarning("Couldn't generate a solvable path after 200 attempts.");
        return null;
    }

    private Dictionary<Vector2Int, (PipeShape, int)> TryWalkPath(Vector2Int exitCell)
    {
        var path = new Dictionary<Vector2Int, (PipeShape, int)>();
        Vector2Int current = Vector2Int.zero;
        Direction incoming = entryDirection;
        HashSet<Vector2Int> visited = new() { current };

        while (true)
        {
            if (current == exitCell)
            {
                path[current] = GetShapeForOpenings(incoming, exitDirection);
                return path;
            }

            List<Direction> options = new()
            {
                Opposite(incoming),
                (Direction)(((int)incoming + 1) % 4),
                (Direction)(((int)incoming + 3) % 4)
            };

            for (int i = options.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (options[i], options[j]) = (options[j], options[i]);
            }

            bool moved = false;
            foreach (Direction outgoing in options)
            {
                Vector2Int next = current + Offset(outgoing);
                if (next.x < 0 || next.x >= gridSize || next.y < 0 || next.y >= gridSize) continue;
                if (visited.Contains(next)) continue;

                path[current] = GetShapeForOpenings(incoming, outgoing);
                visited.Add(next);
                incoming = Opposite(outgoing);
                current = next;
                moved = true;
                break;
            }

            if (!moved) return null;
        }
    }

    private (PipeShape, int) GetShapeForOpenings(Direction a, Direction b)
    {
        foreach (PipeShape shape in new[] { PipeShape.Straight, PipeShape.Elbow })
        {
            int[] baseDirs = shape == PipeShape.Straight ? StraightBase : ElbowBase;
            for (int r = 0; r < 4; r++)
            {
                int o1 = (baseDirs[0] + r) % 4;
                int o2 = (baseDirs[1] + r) % 4;
                if ((o1 == (int)a && o2 == (int)b) || (o1 == (int)b && o2 == (int)a))
                    return (shape, r);
            }
        }
        return (PipeShape.Straight, 0);
    }

    public void ToggleUI()
    {
        uiPanel.SetActive(!uiPanel.activeSelf);
        if (uiPanel.activeSelf) SyncTiles();
    }

    private void Update()
    {
        if (uiPanel.activeSelf) SyncTiles();
        HandleReinforcements();
    }

    private void HandleReinforcements()
    {
        LevelData level = levels[_currentLevel];

        if (_aliveHeavies <= 0 || level.reinforcementPrefab == null) return;
        if (Time.time < _nextReinforcement) return;

        _nextReinforcement = Time.time + level.reinforcementInterval;

        if (_aliveAliens < maxAliveEnemies)
            SpawnEnemy(level.reinforcementPrefab, RandomPlotPosition());
    }

    private void SyncTiles()
    {
        for (int i = 0; i < _plots.Count; i++)
            _tileImages[i].localRotation = Quaternion.Euler(0f, 0f, -_plots[i].rotationState * 90f);
    }

    public void OnPlotTwisted()
    {
        if (_solved) return;

        if (CheckSolved(debugLogs))
        {
            _solved = true;
            Debug.Log("Puzzle solved!");
            SpawnAliens();
        }
    }

    private Direction[] GetOpenings(Plot plot)
    {
        int[] baseDirs = plot.shape == PipeShape.Straight ? StraightBase : ElbowBase;
        return new[]
        {
            (Direction)((baseDirs[0] + plot.rotationState) % 4),
            (Direction)((baseDirs[1] + plot.rotationState) % 4)
        };
    }

    private static Direction Opposite(Direction d) => (Direction)(((int)d + 2) % 4);

    private static Vector2Int Offset(Direction d) => d switch
    {
        Direction.North => new Vector2Int(0, -1),
        Direction.East => new Vector2Int(1, 0),
        Direction.South => new Vector2Int(0, 1),
        Direction.West => new Vector2Int(-1, 0),
        _ => Vector2Int.zero
    };

    private bool CheckSolved(bool log)
    {
        Vector2Int pos = Vector2Int.zero;
        Vector2Int exitCell = new Vector2Int(gridSize - 1, gridSize - 1);
        Direction incoming = entryDirection;
        HashSet<Vector2Int> visited = new();

        while (true)
        {
            if (pos.x < 0 || pos.x >= gridSize || pos.y < 0 || pos.y >= gridSize)
            {
                if (log) Debug.Log($"[CheckSolved] FAILED: {pos} out of bounds");
                return false;
            }
            if (!visited.Add(pos))
            {
                if (log) Debug.Log($"[CheckSolved] FAILED: loop at {pos}");
                return false;
            }

            Plot plot = _plots[pos.y * gridSize + pos.x];
            Direction[] openings = GetOpenings(plot);

            if (openings[0] != incoming && openings[1] != incoming)
            {
                if (log) Debug.Log($"[CheckSolved] FAILED: {pos} ({plot.shape}, rot {plot.rotationState}) doesn't accept {incoming}");
                return false;
            }

            Direction outgoing = openings[0] == incoming ? openings[1] : openings[0];

            if (pos == exitCell && outgoing == exitDirection)
            {
                if (log) Debug.Log("[CheckSolved] SUCCESS");
                return true;
            }

            pos += Offset(outgoing);
            incoming = Opposite(outgoing);
        }
    }

    private void SpawnAliens()
    {
        PlayCombatMusic();
        uiPanel.SetActive(false);

        LevelData level = levels[_currentLevel];

        // Spawn boss if this level has one
        if (level.bossPrefab != null)
        {
            SpawnBoss(level);
        }

        // Spawn normal aliens as well
        GameObject[] prefabs = level.alienPrefabs;

        if (prefabs != null && prefabs.Length > 0)
        {
            for (int i = 0; i < _plots.Count; i++)
            {
                if (_aliveAliens >= maxAliveEnemies)
                    break;

                SpawnEnemy(
                    prefabs[i % prefabs.Length],
                    _plots[i].transform.position + Vector3.up
                );
            }
        }

        // Start combat music
        PlayCombatMusic();

        // Start reinforcement timer
        _nextReinforcement = Time.time + level.reinforcementInterval;

        if (_aliveAliens <= 0)
            CompleteLevel();
    }

    private Vector3 RandomPlotPosition()
    {
        return _plots[Random.Range(0, _plots.Count)].transform.position + Vector3.up;
    }

    private void SpawnEnemy(GameObject prefab, Vector3 pos)
    {
        GameObject alien = Instantiate(prefab, pos, Quaternion.identity);
        _aliens.Add(alien);

        if (!alien.TryGetComponent(out AlienFollower follower))
            follower = alien.AddComponent<AlienFollower>();

        follower.SetTarget(player);

        if (!alien.TryGetComponent(out EnemyHealth health))
        {
            Debug.LogWarning($"{alien.name} has no EnemyHealth, so it can't be counted for level completion.");
            return;
        }

        _aliveAliens++;
        health.OnDeath += HandleAlienDeath;

        if (health.IsHeavy)
        {
            _aliveHeavies++;
            health.OnDeath += () => _aliveHeavies--;
        }
    }

    public int SummonFromPlots(int count)
    {
        GameObject[] prefabs = levels[_currentLevel].alienPrefabs;
        if (prefabs == null || prefabs.Length == 0 || _plots.Count == 0) return 0;

        int spawned = 0;
        while (spawned < count && _aliveAliens < maxAliveEnemies)
        {
            SpawnEnemy(prefabs[Random.Range(0, prefabs.Length)], RandomPlotPosition());
            spawned++;
        }

        return spawned;
    }

    private void SpawnBoss(LevelData level)
    {
        Vector3 pos = level.bossSpawnPoint != null ? level.bossSpawnPoint.position : plotParent.position;
        Quaternion rot = level.bossSpawnPoint != null ? level.bossSpawnPoint.rotation : Quaternion.identity;

        GameObject boss = Instantiate(level.bossPrefab, pos, rot);
        _aliens.Add(boss);

        if (boss.TryGetComponent(out BossController controller))
        {
            controller.SetTarget(player);
            controller.SetPuzzle(this);
        }

        if (boss.TryGetComponent(out EnemyHealth health))
        {
            _aliveAliens++;
            health.OnDeath += () => HandleBossDeath(boss);
        }
    }

    private void HandleBossDeath(GameObject boss)
    {
        foreach (GameObject alien in _aliens)
            if (alien != null && alien != boss) Destroy(alien);

        _aliens.Clear();
        _aliens.Add(boss);
        _aliveAliens = 0;
        _aliveHeavies = 0;

        CompleteLevel();
    }

    private void HandleAlienDeath()
    {
        _aliveAliens--;
        if (_aliveAliens <= 0)
            CompleteLevel();
    }

    private void CompleteLevel()
    {
        if (_playerHealth != null && _playerHealth.IsDead)
            return;

        StopCombatMusic();

        OnLevelCompleted?.Invoke(_currentLevel);

        if (!HasNextLevel)
        {
            PlayerPrefs.DeleteKey(SaveKey);
            Debug.Log("All levels complete");

            PlayEndingCutscene();

            OnAllLevelsCompleted?.Invoke();
        }
    }

    public void NextLevel()
    {
        if (!HasNextLevel) return;

        BuildLevel(_currentLevel + 1);
    }

    public void RestartLevel()
    {
        if (_playerHealth != null) _playerHealth.ResetHealth();
        BuildLevel(_currentLevel);
    }

    private void PlayCombatMusic()
    {
        if (combatMusicSource == null || combatMusic == null)
            return;

        if (combatMusicSource.clip != combatMusic)
            combatMusicSource.clip = combatMusic;

        if (!combatMusicSource.isPlaying)
            combatMusicSource.Play();
    }

    private void StopCombatMusic()
    {
        if (combatMusicSource != null && combatMusicSource.isPlaying)
            combatMusicSource.Stop();
    }
    private void PlayEndingCutscene()
    {
        if (endingCutscenePlayer == null)
        {
            Debug.LogWarning("Ending cutscene VideoPlayer is not assigned.");
            return;
        }

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        if (endingCutscenePanel != null)
            endingCutscenePanel.SetActive(true);

        endingCutscenePlayer.Stop();
        endingCutscenePlayer.Play();
    }

    public void CloseLevel1Intro()
    {
        if (level1IntroPanel != null)
            level1IntroPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenLevel1Intro()
    {
        if (level1IntroPanel != null)
        {
            level1IntroPanel.SetActive(true);

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}