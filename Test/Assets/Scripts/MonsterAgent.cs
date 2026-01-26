using UnityEngine;
using UnityEngine.Serialization;

public sealed class MonsterAgent : MonoBehaviour
{
    [Header("References")]
    [FormerlySerializedAs("grid_system")]
    [SerializeField] private GridSystem gridSystem;

    [Header("Movement")]
    [FormerlySerializedAs("move_speed")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 3f;

    [FormerlySerializedAs("arrival_radius")]
    [SerializeField, Min(0.001f)] private float arrivalRadius = 0.05f;

    [SerializeField, Min(0f)] private float turnSpeed = 12f;

    [Header("Test Spawn")]
    [FormerlySerializedAs("use_fixed_spawn_cell")]
    [SerializeField] private bool useFixedSpawnCell = false;

    [FormerlySerializedAs("fixed_spawn_x")]
    [SerializeField] private int fixedSpawnX = 1;

    [FormerlySerializedAs("fixed_spawn_y")]
    [SerializeField] private int fixedSpawnY = 1;

    private Cell currentCell;
    private Cell targetCell;
    private Vector3 targetWorld;

    private Vector2Int lastMoveDir;
    private bool hasTarget;

    private void Awake()
    {
        if (gridSystem == null)
            gridSystem = FindObjectOfType<GridSystem>();

        if (gridSystem == null)
        {
            Debug.LogError("[MonsterAgent] GridSystem not found in scene.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (!TryGetSpawnCell(out Cell spawnCell))
        {
            enabled = false;
            return;
        }

        TeleportToCell(spawnCell);
    }

    private void Update()
    {
        if (!hasTarget || HasArrivedToTarget())
        {
            SnapToTarget();

            currentCell = targetCell;
            hasTarget = false;

            if (!gridSystem.TryGetNextStep(currentCell, lastMoveDir, out Cell nextCell, out Vector2Int nextDir))
                return;

            targetCell = nextCell;
            lastMoveDir = nextDir;
            targetWorld = gridSystem.CellToWorld(nextCell, y: transform.position.y);
            hasTarget = true;
        }

        // Smooth movement (world interpolation)
        transform.position = Vector3.MoveTowards(transform.position, targetWorld, moveSpeed * Time.deltaTime);

        // Optional: rotate toward movement direction (top-down friendly)
        Vector3 dir = targetWorld - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, Time.deltaTime * turnSpeed);
        }
    }

    private bool TryGetSpawnCell(out Cell spawnCell)
    {
        if (useFixedSpawnCell)
        {
            spawnCell = new Cell(fixedSpawnX, fixedSpawnY);

            if (!gridSystem.IsInside(spawnCell))
            {
                Debug.LogWarning($"[MonsterAgent] Fixed spawn cell is outside grid: {spawnCell}");
                return false;
            }

            return true;
        }

        if (!gridSystem.TryGetRandomSpawnCell(out spawnCell))
        {
            Debug.LogWarning("[MonsterAgent] No reachable edge spawn cell.");
            return false;
        }

        return true;
    }

    private void TeleportToCell(Cell cell)
    {
        currentCell = cell;
        targetCell = cell;

        targetWorld = gridSystem.CellToWorld(cell, y: transform.position.y);
        transform.position = targetWorld;

        hasTarget = false;
        lastMoveDir = Vector2Int.zero;
    }

    private bool HasArrivedToTarget()
    {
        float radiusSqr = arrivalRadius * arrivalRadius;
        return (transform.position - targetWorld).sqrMagnitude <= radiusSqr;
    }

    private void SnapToTarget()
    {
        transform.position = targetWorld;
    }
}
