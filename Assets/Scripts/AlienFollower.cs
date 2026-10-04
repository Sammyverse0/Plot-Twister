using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AlienFollower : MonoBehaviour
{
    public Transform target;

    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private float turnSpeed = 8f;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float separationWeight = 1.5f;

    private static readonly List<AlienFollower> All = new();

    private NavMeshAgent _agent;

    private void Awake()
    {
        TryGetComponent(out _agent);
        ApplyAgentSettings();
    }

    private void OnEnable() => All.Add(this);

    private void OnDisable() => All.Remove(this);

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

        Vector3 move = Vector3.zero;
        if (toTarget.magnitude > stopDistance)
            move += toTarget.normalized;
        move += GetSeparation() * separationWeight;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.position += move * (moveSpeed * Time.deltaTime);

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(toTarget.normalized),
                turnSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetSeparation()
    {
        Vector3 push = Vector3.zero;

        foreach (AlienFollower other in All)
        {
            if (other == this) continue;

            Vector3 offset = transform.position - other.transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (distance >= separationRadius) continue;

            if (distance < 0.0001f)
            {
                Vector2 random = Random.insideUnitCircle.normalized;
                push += new Vector3(random.x, 0f, random.y);
            }
            else
            {
                push += offset / distance * (1f - distance / separationRadius);
            }
        }

        return push;
    }
}