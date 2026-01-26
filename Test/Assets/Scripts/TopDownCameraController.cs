using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public sealed class TopDownCameraController : MonoBehaviour
{
    private const string BaseTag = "Base";

    [Header("References")]
    [FormerlySerializedAs("target_camera")]
    [SerializeField] private Camera controlledCamera;

    [FormerlySerializedAs("follow_target")]
    [SerializeField] private Transform baseTarget;

    [FormerlySerializedAs("grid_system")]
    [SerializeField] private GridSystem gridSystem;

    [Header("View")]
    [FormerlySerializedAs("camera_height")]
    [Tooltip("Camera Y height in world space.")]
    [SerializeField, Min(0.1f)] private float cameraHeight = 40f;

    [FormerlySerializedAs("world_padding")]
    [Tooltip("Extra padding so the grid is not clipped by screen edges.")]
    [SerializeField, Min(0f)] private float worldPadding = 2f;

    private void Reset()
    {
        controlledCamera = GetComponent<Camera>();
        if (controlledCamera == null)
            controlledCamera = Camera.main;
    }

    private void OnEnable() => Apply();
    private void Update() => Apply();
    private void OnValidate() => Apply();

    private void Apply()
    {
        if (controlledCamera == null)
            controlledCamera = GetComponent<Camera>();

        if (controlledCamera == null)
            controlledCamera = Camera.main;

        if (controlledCamera == null)
            return;

        if (baseTarget == null)
        {
            try
            {
                GameObject baseObj = GameObject.FindWithTag(BaseTag);
                if (baseObj != null)
                    baseTarget = baseObj.transform;
            }
            catch (UnityException)
            {
                // Tag not defined -> ignore.
            }
        }

        if (gridSystem == null)
            gridSystem = FindObjectOfType<GridSystem>();

        if (!controlledCamera.orthographic)
            controlledCamera.orthographic = true;

        // Center camera on base (or base cell)
        Vector3 desired = controlledCamera.transform.position;

        if (baseTarget != null)
        {
            desired.x = baseTarget.position.x;
            desired.z = baseTarget.position.z;
        }
        else if (gridSystem != null)
        {
            Vector3 baseWorld = gridSystem.CellToWorld(gridSystem.BaseCell, y: 0f);
            desired.x = baseWorld.x;
            desired.z = baseWorld.z;
        }

        desired.y = cameraHeight;
        controlledCamera.transform.position = desired;

        // Auto-fit orthographic size to grid
        if (gridSystem == null)
            return;

        float mapWidth = gridSystem.Width * gridSystem.CellSize;
        float mapHeight = gridSystem.Height * gridSystem.CellSize;

        float halfW = mapWidth * 0.5f + worldPadding;
        float halfH = mapHeight * 0.5f + worldPadding;

        float aspect = Mathf.Max(0.0001f, controlledCamera.aspect);
        float requiredSize = Mathf.Max(halfH, halfW / aspect);

        controlledCamera.orthographicSize = Mathf.Max(0.1f, requiredSize);
    }
}
