using UnityEngine;
using UnityEngine.AI;

public class AlienFollower : MonoBehaviour
{
    public Transform target;

    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private float turnSpeed = 8f;

    private NavMeshAgent _agent;

    private void Awake()
    {
        TryGetComponent(out _agent);
        ApplyAgentSettings();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ApplyAgentSettings();
    }

    private void ApplyAgentSettings()
    {
        if (_agent == null) return;
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = stopDistance;
    }

    private void Update()
    {
        if (target == null) return;

        
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.SetDestination(target.position);
            return;
        }

        
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude <= stopDistance) return;

        Vector3 dir = toTarget.normalized;
        transform.position += dir * (moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            turnSpeed * Time.deltaTime);
    }
}