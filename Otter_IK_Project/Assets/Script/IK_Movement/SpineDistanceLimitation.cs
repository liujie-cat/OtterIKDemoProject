using System.Collections;
using UnityEngine;

public class DistanceLimiterConstraint : MonoBehaviour
{
    public Transform source;
    public Transform constrained;
    public float minDistance = 0.1f;
    public float maxDistance = 0.5f;

    void LateUpdate()
    {
        if (source == null || constrained == null) return;

        Vector3 dir = constrained.position - source.position;
        float dist = dir.magnitude;

        if (dist < minDistance || dist > maxDistance)
        {
            float clampedDist = Mathf.Clamp(dist, minDistance, maxDistance);
            constrained.position = source.position + dir.normalized * clampedDist;
        }
    }
}
