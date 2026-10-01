using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Replaces manual per-level wiring: set gridSize per level and this spawns
// both the world plots and the matching UI tiles, keeping them in sync by index.
public class PuzzleManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int gridSize = 2; // 2 for level 1, 3 for level 2, etc.
    [SerializeField] private Vector2 plotSpacing = new Vector2(2f, 2f); // x = gap between columns, y = gap between rows
    [SerializeField] private Transform plotParent;   // empty object marking the grid's corner
    [SerializeField] private GameObject plotPrefab;

    [Header("UI")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private GridLayoutGroup tileGrid; // on the UI panel's content container
    [SerializeField] private RectTransform tileImagePrefab;
    [SerializeField] private Sprite straightSprite;
    [SerializeField] private Sprite elbowSprite;

    private readonly List<Plot> _plots = new();
    private readonly List<RectTransform> _tileImages = new();

    private void Start()
    {
        tileGrid.constraintCount = gridSize;

        // Fit the grid into the panel's existing size, however big gridSize is.
        RectTransform gridRect = tileGrid.GetComponent<RectTransform>();
        float cellWidth = (gridRect.rect.width - tileGrid.spacing.x * (gridSize - 1)) / gridSize;
        float cellHeight = (gridRect.rect.height - tileGrid.spacing.y * (gridSize - 1)) / gridSize;
        tileGrid.cellSize = new Vector2(cellWidth, cellHeight);

        for (int i = 0; i < gridSize * gridSize; i++)
        {
            int x = i % gridSize;
            int z = i / gridSize;

            Vector3 worldPos = plotParent.position + new Vector3(x * plotSpacing.x, 0f, z * plotSpacing.y);
            Plot plot = Instantiate(plotPrefab, worldPos, Quaternion.identity, plotParent).GetComponent<Plot>();
            plot.shape = (PipeShape)Random.Range(0, 2); // placeholder — replace with designed layouts later
            _plots.Add(plot);

            RectTransform tile = Instantiate(tileImagePrefab, tileGrid.transform);
            tile.GetComponent<Image>().sprite = plot.shape == PipeShape.Straight ? straightSprite : elbowSprite;
            _tileImages.Add(tile);
        }
    }

    public void ToggleUI() => uiPanel.SetActive(!uiPanel.activeSelf);

    private void Update()
    {
        if (!uiPanel.activeSelf) return;

        for (int i = 0; i < _plots.Count; i++)
        {
            _tileImages[i].localRotation = Quaternion.Euler(0f, 0f, -_plots[i].rotationState * 90f);
        }
    }
}