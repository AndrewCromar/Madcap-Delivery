using UnityEngine;
using UnityEngine.AI;

public class ChaserAI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float waypointTolerance = 2f;
    [SerializeField] private float maxSteerInput = 1f;
    [SerializeField] private float maxThrottleInput = 1f;
    [SerializeField] private float reverseThreshold = -0.5f; // how far behind before reversing

    [Header("Debug")]
    [SerializeField] private CarController carController;
    [SerializeField] private NavMeshAgent navMeshAgent;

    private NavMeshPath path;
    private int currentCorner;

    private void Start()
    {
        carController = GetComponentInParent<CarController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();

        // Disable agent auto-movement — physics drives the car
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
    }

    private void Update()
    {
        if (target == null || carController == null || navMeshAgent == null) return;

        // Keep agent synced with car’s transform
        navMeshAgent.Warp(transform.position);

        // Recalculate path from car -> target
        if (NavMesh.SamplePosition(target.position, out var hit, 2f, NavMesh.AllAreas))
        {
            if (navMeshAgent.CalculatePath(hit.position, path))
            {
                currentCorner = 0; // reset whenever we get a fresh path
            }
        }

        // Path must have at least 2 corners (start + first waypoint)
        if (path == null || path.corners.Length < 2) return;

        // Draw path for debug
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.green);
        }

        // Always skip the first corner (it's the car’s current pos)
        int targetCornerIndex = Mathf.Clamp(currentCorner + 1, 1, path.corners.Length - 1);
        Vector3 nextCorner = path.corners[targetCornerIndex];

        Vector3 toWaypoint = nextCorner - transform.position;
        toWaypoint.y = 0;

        // Advance waypoint when close enough
        if (toWaypoint.magnitude < waypointTolerance && targetCornerIndex < path.corners.Length - 1)
        {
            currentCorner++;
            return;
        }

        // Convert to local space for steering/throttle
        Vector3 localTarget = transform.InverseTransformPoint(nextCorner);

        // Steering: left/right based on local X
        float steerInput = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f) * maxSteerInput;

        // Throttle: prefer forward, only reverse if strongly behind
        float throttleInput = maxThrottleInput;
        if (localTarget.z < reverseThreshold)
        {
            throttleInput = -maxThrottleInput;
        }

        // Feed inputs into CarController
        carController.SendAIInputs(throttleInput, steerInput);
    }
}
