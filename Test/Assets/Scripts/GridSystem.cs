using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class GridSystem : MonoBehaviour
{
    private const string BaseTag = "Base";

    [Header("Grid")]
    [FormerlySerializedAs("grid_width")]
    [SerializeField, Min(2)] private int gridWidth = 30;

    [FormerlySerializedAs("grid_height")]
    [SerializeField, Min(2)] private int gridHeight = 20;

    [FormerlySerializedAs("cell_size")]
    [SerializeField, Min(0.1f)] private float cellSize = 1f;

    [Header("Base")]
    [FormerlySerializedAs("base_transform")]
    [SerializeField] private Transform baseTransform;

    [FormerlySerializedAs("base_cell")]
    [SerializeField] private Cell fallbackBaseCell = new Cell(15, 10);

    [Header("Towers")]
    [FormerlySerializedAs("tower_prefab")]
    [Tooltip("If null, a default Cube will be created.")]
    [SerializeField] private GameObject towerPrefab;

    [FormerlySerializedAs("tower_height")]
    [SerializeField, Min(0.1f)] private float towerHeight = 1f;

    [Header("Placement Rules")]
    [FormerlySerializedAs("no_build_border_thickness")]
    [Tooltip("Cells within this border thickness from the edge cannot be built on.")]
    [SerializeField, Min(0)] private int noBuildBorderThickness = 2;

    [SerializeField] private bool preventBuildNearMonster = true;
    [SerializeField, Min(0)] private int monsterNoBuildRadiusCells = 2;

    [FormerlySerializedAs("prevent_build_on_monster")]
    [SerializeField] private bool preventBuildOnMonster = true;

    [FormerlySerializedAs("monster_layer_mask")]
    [SerializeField] private LayerMask monsterLayerMask;

    [FormerlySerializedAs("monster_check_y")]
    [SerializeField, Min(0f)] private float monsterCheckY = 0.5f;

    [FormerlySerializedAs("monster_check_half_height")]
    [SerializeField, Min(0.01f)] private float monsterCheckHalfHeight = 1.0f;

    [Header("Debug")]
    [FormerlySerializedAs("draw_grid_gizmos")]
    [SerializeField] private bool drawGridGizmos = true;

    [FormerlySerializedAs("draw_blocked_cells")]
    [SerializeField] private bool drawBlockedCells = true;

    [FormerlySerializedAs("draw_unreachable_cells")]
    [SerializeField] private bool drawUnreachableCells = false;

    public int Width => gridWidth;
    public int Height => gridHeight;
    public float CellSize => cellSize;
    public Cell BaseCell => baseCell;

    private enum CellState : byte
    {
        Empty = 0,
        Blocked = 1,
        Base = 2,
    }

    private static readonly Vector2Int[] CardinalDirections =
    {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0),  // Left
    };

    private CellState[] cellStates;
    private int[] distanceToBase;         // Real distance field used by monsters
    private int[] previewDistanceToBase;  // Scratch distance field used by placement preview

    private Cell baseCell;

    private readonly Dictionary<int, GameObject> towerVisualByIndex = new Dictionary<int, GameObject>(256);
    private readonly List<Cell> edgeSpawnBuffer = new List<Cell>(128);
    private readonly Collider[] monsterOverlapBuffer = new Collider[8];

    private void Awake()
    {
        ResolveReferencesIfNeeded();
        ClampSettings();
        EnsureBuffers();

        ResolveBaseCell();
        ResetGridState();
        RebuildDistanceField(distanceToBase, assumedBlockedCell: null);

        SnapBaseTransformToCellCenter();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        ClampSettings();
        EnsureBuffers();

        ResolveBaseCellFromCurrentReferences();
        ResetGridState();
        RebuildDistanceField(distanceToBase, assumedBlockedCell: null);
    }

    public Cell WorldToCell(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / cellSize);
        int y = Mathf.FloorToInt(worldPosition.z / cellSize);
        return new Cell(x, y);
    }

    public Vector3 CellToWorld(Cell cell, float y = 0f)
    {
        float worldX = (cell.X + 0.5f) * cellSize;
        float worldZ = (cell.Y + 0.5f) * cellSize;
        return new Vector3(worldX, y, worldZ);
    }

    public bool IsInside(Cell cell)
    {
        return cell.X >= 0 && cell.X < gridWidth && cell.Y >= 0 && cell.Y < gridHeight;
    }

    public bool IsBuildable(Cell cell)
    {

        if (!IsInside(cell))
            return false;

        if (cell == baseCell)
            return false;

        if (IsInsideNoBuildBorder(cell))
            return false;

        if (GetCellState(cell) != CellState.Empty)
            return false;


        if (IsCellOccupiedByMonster(cell))
            return false;

        return true;
    }

    public bool CanPlaceTower(Cell cell)
    {
        RebuildDistanceField(previewDistanceToBase, assumedBlockedCell: cell);

        if (!HasAnyReachableEdgeSpawn(previewDistanceToBase, assumedBlockedCell: cell))
            return false;

        // ✅ 기존 몬스터들이 갇히는 배치면 프리뷰부터 빨강
        if (!AreAllExistingMonstersReachable(previewDistanceToBase))
            return false;

        return true;
    }

    public bool TryPlaceTower(Cell cell)
    {
        if (!IsBuildable(cell))
        {
            Debug.Log($"[GridSystem] TryPlaceTower failed (NotBuildable) cell={cell}");
            return false;
        }

        SetCellState(cell, CellState.Blocked);
        RebuildDistanceField(distanceToBase, assumedBlockedCell: null);

        if (!HasAnyReachableEdgeSpawn(distanceToBase, assumedBlockedCell: null))
        {
            SetCellState(cell, CellState.Empty);
            RebuildDistanceField(distanceToBase, assumedBlockedCell: null);

            Debug.Log($"[GridSystem] TryPlaceTower failed (WouldBlockAllSpawns) cell={cell}");
            return false;
        }
        if (!AreAllExistingMonstersReachable(distanceToBase))
        {
            // Rollback
            SetCellState(cell, CellState.Empty);
            RebuildDistanceField(distanceToBase, assumedBlockedCell: null);

            Debug.Log($"[GridSystem] TryPlaceTower failed (WouldTrapMonsters) cell={cell}");
            return false;
        }

        SpawnTowerVisual(cell);
        Debug.Log($"[GridSystem] TryPlaceTower success cell={cell}");
        return true;
    }

    public bool RemoveTower(Cell cell)
    {
        if (!IsInside(cell))
            return false;

        if (GetCellState(cell) != CellState.Blocked)
            return false;

        int index = ToIndex(cell);

        if (towerVisualByIndex.TryGetValue(index, out GameObject visual) && visual != null)
            Destroy(visual);

        towerVisualByIndex.Remove(index);

        SetCellState(cell, CellState.Empty);
        RebuildDistanceField(distanceToBase, assumedBlockedCell: null);

        Debug.Log($"[GridSystem] RemoveTower success cell={cell}");
        return true;
    }

    private bool AreAllExistingMonstersReachable(int[] distanceField)
    {
        // 배치 시에만 호출되므로 FindObjectsOfType 사용해도 부담이 적음
        MonsterAgent[] monsters = FindObjectsOfType<MonsterAgent>();

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterAgent m = monsters[i];
            if (m == null) continue;

            Cell monsterCell = WorldToCell(m.transform.position);

            // 그리드 밖이면 일단 스킵(스폰 직후 튕김 방지)
            if (!IsInside(monsterCell))
                continue;

            // 베이스까지 도달 불가면 "가두기"가 된 상태
            if (GetDistance(distanceField, monsterCell) == -1)
                return false;
        }

        return true;
    }
    public bool IsCellOccupiedByMonster(Cell cell)
    {
        if (!preventBuildOnMonster && !preventBuildNearMonster)
            return false;

        if (!Application.isPlaying)
            return false;

        if (!IsInside(cell))
            return false;

        Vector3 center = CellToWorld(cell, y: monsterCheckY);

        float halfX = cellSize * 0.45f;
        float halfZ = cellSize * 0.45f;

        if (preventBuildNearMonster && monsterNoBuildRadiusCells > 0)
        {
            float expand = cellSize * monsterNoBuildRadiusCells;
            halfX += expand;
            halfZ += expand;
        }

        Vector3 halfExtents = new Vector3(halfX, monsterCheckHalfHeight, halfZ);

        int hitCount = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            monsterOverlapBuffer,
            Quaternion.identity,
            monsterLayerMask,
            QueryTriggerInteraction.Ignore
        );

        return hitCount > 0;
    }

    public bool TryGetRandomSpawnCell(out Cell spawnCell)
    {
        CollectReachableEdgeCells(edgeSpawnBuffer);

        if (edgeSpawnBuffer.Count == 0)
        {
            spawnCell = default;
            Debug.LogWarning("[GridSystem] No reachable edge spawn cells!");
            return false;
        }

        spawnCell = edgeSpawnBuffer[Random.Range(0, edgeSpawnBuffer.Count)];
        return true;
    }

    public bool TryGetNextStep(Cell currentCell, Vector2Int lastDir, out Cell nextCell, out Vector2Int nextDir)
    {
        nextCell = currentCell;
        nextDir = Vector2Int.zero;

        if (!IsInside(currentCell))
            return false;

        int currentDistance = GetDistance(distanceToBase, currentCell);
        if (currentDistance <= 0)
            return false; 


        if (lastDir != Vector2Int.zero)
        {
            Cell straightCell = new Cell(currentCell.X + lastDir.x, currentCell.Y + lastDir.y);
            if (IsInside(straightCell) && GetDistance(distanceToBase, straightCell) == currentDistance - 1)
            {
                nextCell = straightCell;
                nextDir = lastDir;
                return true;
            }
        }

        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector2Int dir = CardinalDirections[i];
            Cell candidate = new Cell(currentCell.X + dir.x, currentCell.Y + dir.y);

            if (!IsInside(candidate))
                continue;

            if (GetDistance(distanceToBase, candidate) == currentDistance - 1)
            {
                nextCell = candidate;
                nextDir = dir;
                return true;
            }
        }

        return false;
    }


    private void ResolveReferencesIfNeeded()
    {
        if (baseTransform != null)
            return;

        try
        {
            GameObject baseObj = GameObject.FindWithTag(BaseTag);
            if (baseObj != null)
                baseTransform = baseObj.transform;
        }
        catch (UnityException)
        {

        }
    }

    private void ClampSettings()
    {
        if (gridWidth < 2) gridWidth = 2;
        if (gridHeight < 2) gridHeight = 2;
        if (cellSize < 0.1f) cellSize = 0.1f;
        if (noBuildBorderThickness < 0) noBuildBorderThickness = 0;
        if (towerHeight < 0.1f) towerHeight = 0.1f;
    }

    private void EnsureBuffers()
    {
        int expectedLength = gridWidth * gridHeight;

        if (cellStates == null || cellStates.Length != expectedLength)
            cellStates = new CellState[expectedLength];

        if (distanceToBase == null || distanceToBase.Length != expectedLength)
            distanceToBase = new int[expectedLength];

        if (previewDistanceToBase == null || previewDistanceToBase.Length != expectedLength)
            previewDistanceToBase = new int[expectedLength];
    }

    private void ResolveBaseCell()
    {
        ResolveBaseCellFromCurrentReferences();

        if (!IsInside(baseCell))
            baseCell = new Cell(gridWidth / 2, gridHeight / 2);
    }

    private void ResolveBaseCellFromCurrentReferences()
    {
        if (baseTransform != null)
        {
            baseCell = WorldToCell(baseTransform.position);
            return;
        }

        baseCell = fallbackBaseCell;
    }

    private void ResetGridState()
    {
        for (int i = 0; i < cellStates.Length; i++)
            cellStates[i] = CellState.Empty;

        if (IsInside(baseCell))
            cellStates[ToIndex(baseCell)] = CellState.Base;
    }

    private void SnapBaseTransformToCellCenter()
    {
        if (baseTransform == null)
            return;

        Vector3 aligned = CellToWorld(baseCell, y: baseTransform.position.y);
        baseTransform.position = aligned;
    }

    private bool IsInsideNoBuildBorder(Cell cell)
    {
        if (noBuildBorderThickness <= 0)
            return false;

        if (cell.X < noBuildBorderThickness) return true;
        if (cell.Y < noBuildBorderThickness) return true;
        if (cell.X >= gridWidth - noBuildBorderThickness) return true;
        if (cell.Y >= gridHeight - noBuildBorderThickness) return true;

        return false;
    }


    private int ToIndex(Cell cell) => cell.Y * gridWidth + cell.X;

    private CellState GetCellState(Cell cell) => cellStates[ToIndex(cell)];

    private void SetCellState(Cell cell, CellState state) => cellStates[ToIndex(cell)] = state;

    private bool IsCellWalkable(Cell cell, Cell? assumedBlockedCell)
    {
        if (assumedBlockedCell.HasValue && cell == assumedBlockedCell.Value)
            return false;

        CellState state = GetCellState(cell);
        return state == CellState.Empty || state == CellState.Base;
    }


    private int GetDistance(int[] distanceField, Cell cell) => distanceField[ToIndex(cell)];

    private void RebuildDistanceField(int[] outDistanceField, Cell? assumedBlockedCell)
    {
        for (int i = 0; i < outDistanceField.Length; i++)
            outDistanceField[i] = -1;

        if (!IsCellWalkable(baseCell, assumedBlockedCell))
        {
            Debug.LogError("[GridSystem] Base cell is not walkable. Check grid state.");
            return;
        }

        Queue<Cell> queue = new Queue<Cell>(256);

        outDistanceField[ToIndex(baseCell)] = 0;
        queue.Enqueue(baseCell);

        while (queue.Count > 0)
        {
            Cell current = queue.Dequeue();
            int currentDistance = outDistanceField[ToIndex(current)];

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int dir = CardinalDirections[i];
                Cell neighbor = new Cell(current.X + dir.x, current.Y + dir.y);

                if (!IsInside(neighbor))
                    continue;

                if (!IsCellWalkable(neighbor, assumedBlockedCell))
                    continue;

                int neighborIndex = ToIndex(neighbor);
                if (outDistanceField[neighborIndex] != -1)
                    continue;

                outDistanceField[neighborIndex] = currentDistance + 1;
                queue.Enqueue(neighbor);
            }
        }
    }

    private bool HasAnyReachableEdgeSpawn(int[] distanceField, Cell? assumedBlockedCell)
    {

        for (int x = 0; x < gridWidth; x++)
        {
            if (IsReachableSpawnCell(new Cell(x, 0), distanceField, assumedBlockedCell))
                return true;
        }

        int bottomY = gridHeight - 1;
        if (bottomY != 0)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (IsReachableSpawnCell(new Cell(x, bottomY), distanceField, assumedBlockedCell))
                    return true;
            }
        }

        for (int y = 1; y < gridHeight - 1; y++)
        {
            if (IsReachableSpawnCell(new Cell(0, y), distanceField, assumedBlockedCell))
                return true;

            int rightX = gridWidth - 1;
            if (rightX != 0 && IsReachableSpawnCell(new Cell(rightX, y), distanceField, assumedBlockedCell))
                return true;
        }

        return false;
    }

    private bool IsReachableSpawnCell(Cell cell, int[] distanceField, Cell? assumedBlockedCell)
    {
        if (!IsInside(cell)) return false;
        if (cell == baseCell) return false;

        if (!IsCellWalkable(cell, assumedBlockedCell))
            return false;

        return GetDistance(distanceField, cell) != -1;
    }

    private void CollectReachableEdgeCells(List<Cell> buffer)
    {
        buffer.Clear();


        for (int x = 0; x < gridWidth; x++)
            AddIfReachableSpawnCell(new Cell(x, 0), buffer);

     
        int bottomY = gridHeight - 1;
        if (bottomY != 0)
        {
            for (int x = 0; x < gridWidth; x++)
                AddIfReachableSpawnCell(new Cell(x, bottomY), buffer);
        }

    
        int rightX = gridWidth - 1;
        for (int y = 1; y < gridHeight - 1; y++)
        {
            AddIfReachableSpawnCell(new Cell(0, y), buffer);

            if (rightX != 0)
                AddIfReachableSpawnCell(new Cell(rightX, y), buffer);
        }
    }

    private void AddIfReachableSpawnCell(Cell cell, List<Cell> buffer)
    {
        if (!IsReachableSpawnCell(cell, distanceToBase, assumedBlockedCell: null))
            return;

        if (GetCellState(cell) != CellState.Empty)
            return;

        buffer.Add(cell);
    }
    private void SpawnTowerVisual(Cell cell)
    {
        int index = ToIndex(cell);

        if (towerVisualByIndex.TryGetValue(index, out GameObject existing) && existing != null)
            return;

        Vector3 position = CellToWorld(cell, y: towerHeight * 0.5f);

        GameObject towerObj;
        if (towerPrefab != null)
        {
            towerObj = Instantiate(towerPrefab, position, Quaternion.identity);
        }
        else
        {
            towerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            towerObj.transform.position = position;

            float side = cellSize * 0.9f;
            towerObj.transform.localScale = new Vector3(side, towerHeight, side);
        }

        towerObj.name = $"Tower_{cell.X}_{cell.Y}";
        towerVisualByIndex[index] = towerObj;
    }
    private void OnDrawGizmos()
    {
        if (!drawGridGizmos)
            return;

        if (cellStates == null || distanceToBase == null)
            return;

        int expectedLength = gridWidth * gridHeight;
        if (cellStates.Length != expectedLength || distanceToBase.Length != expectedLength)
            return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);

        float worldW = gridWidth * cellSize;
        float worldH = gridHeight * cellSize;

        for (int x = 0; x <= gridWidth; x++)
        {
            float px = x * cellSize;
            Gizmos.DrawLine(new Vector3(px, 0f, 0f), new Vector3(px, 0f, worldH));
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            float pz = y * cellSize;
            Gizmos.DrawLine(new Vector3(0f, 0f, pz), new Vector3(worldW, 0f, pz));
        }

        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f);
        Gizmos.DrawCube(CellToWorld(baseCell, 0.05f), new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));

        if (drawBlockedCells)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.55f);
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Cell c = new Cell(x, y);
                    if (GetCellState(c) == CellState.Blocked)
                        Gizmos.DrawCube(CellToWorld(c, 0.05f), new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                }
            }
        }

        if (drawUnreachableCells)
        {
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.25f);
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Cell c = new Cell(x, y);
                    if (GetCellState(c) == CellState.Empty && GetDistance(distanceToBase, c) == -1)
                        Gizmos.DrawCube(CellToWorld(c, 0.05f), new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                }
            }
        }
    }
}
