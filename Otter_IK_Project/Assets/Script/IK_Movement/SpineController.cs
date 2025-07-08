using System.Collections.Generic;
using UnityEngine;

public class DampedFollowerWithDistanceClamp : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;

    [Header("Damping Settings")]
    [Range(0f, 1f)] public float dampPosition = 0.5f;
    [Range(0f, 1f)] public float dampRotation = 0.5f;

    [Header("Distance Constraints")]
    public float minDistance = 0.1f;
    public float maxDistance = 0.5f;

    [Header("Debug")]
    public bool showDebug = true;

    void Update()
    {
        if (target == null) return;

        // --- POSITION ---
        Vector3 desiredPos = Vector3.Lerp(transform.position, target.position, 1f - dampPosition);
        Vector3 toTarget = desiredPos - target.position;
        float currentDist = toTarget.magnitude;

        if (currentDist > maxDistance || currentDist < minDistance)
        {
            Vector3 dir = toTarget.normalized;
            float clampedDist = Mathf.Clamp(currentDist, minDistance, maxDistance);
            desiredPos = target.position + dir * clampedDist;
        }

        transform.position = desiredPos;

        // --- ROTATION (Redamp Style) ---
        Quaternion delta = target.rotation * Quaternion.Inverse(transform.rotation);
        delta = Quaternion.Slerp(Quaternion.identity, delta, 1f - dampRotation);
        transform.rotation = delta * transform.rotation;
    }

    void OnDrawGizmos()
    {
        if (!showDebug || target == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position, minDistance);
        Gizmos.DrawWireSphere(target.position, maxDistance);
        Gizmos.DrawSphere(transform.position, 0.01f);
    }
}