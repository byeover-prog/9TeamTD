using UnityEngine;
using UnityEngine.Serialization;

public sealed class PlacementPreviewController : MonoBehaviour
{
    [Header("References")]
    [FormerlySerializedAs("grid_system")]
    [SerializeField] private GridSystem gridSystem;

    [FormerlySerializedAs("target_camera")]
    [SerializeField] private Camera inputCamera;

    [FormerlySerializedAs("preview_renderer")]
    [SerializeField] private Renderer previewRenderer;

    [Header("Preview")]
    [FormerlySerializedAs("preview_y")]
    [SerializeField] private float previewHeight = 0.02f;

    [Header("Colors")]
    [FormerlySerializedAs("can_place_color")]
    [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.2f, 0.5f);

    [FormerlySerializedAs("cannot_place_color")]
    [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    private Cell lastCell;
    private bool hasLastCell;
    private bool cachedCanPlace;

    private MaterialPropertyBlock mpb;
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (gridSystem == null)
            gridSystem = FindObjectOfType<GridSystem>();

        if (gridSystem == null)
        {
            Debug.LogError("[PlacementPreview] GridSystem not found in scene.");
            enabled = false;
            return;
        }

        if (inputCamera == null)
            inputCamera = Camera.main;

        if (inputCamera == null)
        {
            Debug.LogError("[PlacementPreview] Camera not found.");
            enabled = false;
            return;
        }

        if (previewRenderer == null)
            previewRenderer = GetComponentInChildren<Renderer>();

        if (previewRenderer == null)
        {
            Debug.LogError("[PlacementPreview] Preview renderer not found.");
            enabled = false;
            return;
        }

        mpb = new MaterialPropertyBlock();

        // Match quad size to cell size
        transform.localScale = new Vector3(gridSystem.CellSize, gridSystem.CellSize, 1f);
    }

    private void Update()
    {
        if (!TryGetMouseWorldPoint(out Vector3 worldPoint))
            return;

        Cell cell = gridSystem.WorldToCell(worldPoint);

        if (!gridSystem.IsInside(cell))
        {
            previewRenderer.enabled = false;
            hasLastCell = false;
            return;
        }

        previewRenderer.enabled = true;
        transform.position = gridSystem.CellToWorld(cell, y: previewHeight);

        // ✅ 몬스터가 점유 중이면 즉시 빨강(캐시 무시)
        if (gridSystem.IsCellOccupiedByMonster(cell))
        {
            cachedCanPlace = false;
            hasLastCell = false; // 점유가 풀리면 다음 프레임에 다시 평가
        }
        else
        {
            // 셀이 바뀔 때만 BFS 프리뷰 검사
            if (!hasLastCell || cell != lastCell)
            {
                cachedCanPlace = gridSystem.CanPlaceTower(cell);
                lastCell = cell;
                hasLastCell = true;
            }
        }

        SetPreviewColor(cachedCanPlace ? validColor : invalidColor);

        // Left click: place
        if (Input.GetMouseButtonDown(0))
        {
            if (cachedCanPlace)
            {
                gridSystem.TryPlaceTower(cell);
                hasLastCell = false;
            }
            else
            {
                Debug.Log($"[PlacementPreview] Placement blocked at cell={cell}");
            }
        }

        // Right click: remove
        if (Input.GetMouseButtonDown(1))
        {
            if (gridSystem.RemoveTower(cell))
                hasLastCell = false;
        }
    }

    private void SetPreviewColor(Color color)
    {
        previewRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(ColorId, color);
        previewRenderer.SetPropertyBlock(mpb);
    }

    private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
    {
        Ray ray = inputCamera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);

        if (ground.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        worldPoint = default;
        return false;
    }
}
