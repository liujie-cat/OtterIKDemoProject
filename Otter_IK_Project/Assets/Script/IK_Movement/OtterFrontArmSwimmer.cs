using System;
using System.Collections;
using UnityEngine;

public class OtterFrontArmSwimmer : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] Transform chest;
    [SerializeField] Transform hint_bone_first; // use to check the intended rotation
    [SerializeField] Transform hint_bone_second; // 
    [SerializeField] Transform shoulder_bone; // use to check whether it is the outer limb
    [SerializeField] Transform neighbor_shoulder_bone;
    [SerializeField] Transform arm_bone;
    [SerializeField] Transform home;
    [SerializeField] float distance_threshould = 0.2f;
    [Header("Motion Control")]
    [SerializeField] float swingMagnitude = 2.3f;
    [SerializeField] float inner_side_factor = 0.2f;
    [SerializeField] Vector3 targetOffset;            // used to tuning the final output 
    [SerializeField] float swingDuration = 0.4f;      // swing time
    [SerializeField] float swingAngleMagnitude = 1.2f;
    [SerializeField] float swingTriggerAngle = 15f;
    [SerializeField] float inner_adjust_time = 0.2f;
    //TODO:: Add a parameter to limit the angular speed
    Vector3 startPoint;
    Quaternion startRot;
    Coroutine moveCoroutine;
    float counter = 0;
    public bool Moving { get; private set; }
    Vector3 endPos;
    Quaternion endRot;


    void Awake()
    {
        TrySwing();
    }

    Vector3 MirrorInPlane(Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 normal = Vector3.Cross(A, B).normalized;
        Vector3 mirroredC = C - 2 * Vector3.Dot(C, normal) * normal;
        return mirroredC;
    }


    bool MovingOutSide(Vector3 endpos)
    {
        Vector3 current_pos = transform.position;
        Vector3 swing_dir = endpos - current_pos;
        Vector3 dir = shoulder_bone.position - chest.position;
        Vector3 proj = Vector3.Project(swing_dir, dir);
        if (Vector3.Dot(proj, dir) > 0)
        {
            return true;
        }
        return false;
        //return true;
    }

    bool IsOuterArm()
    {

        float neighbor_distance = Vector3.Distance(neighbor_shoulder_bone.position, hint_bone_first.position);
        float current_distance = Vector3.Distance(shoulder_bone.position, hint_bone_first.position);

        return neighbor_distance < current_distance;


    }

    void ComputeSwingTargetAndRotation(
     Transform chest,
     Transform hint_bone_first,
     Transform arm_bone,
     Transform shoulder_bone,
     float swingTriggerAngle,
     float swingMagnitude,
     float swingAngleMagnitude,
     out bool isOuter,
     out Vector3 finalTarget,
     out Quaternion finalRot
 )
    {
        Vector3 headToChest = (chest.position - hint_bone_first.position).normalized;
        Vector3 chestToArm = (arm_bone.position - chest.position).normalized;
        Vector3 chestToShoulder = (shoulder_bone.position - chest.position).normalized;
        isOuter = IsOuterArm();
        // Vector3 swingDiff = (rotationDeltaDir + chest.forward).normalized;



        Vector3 projection = Vector3.Project(headToChest, -chest.forward);
        Vector3 perpendicular = headToChest - projection;

        if (Vector3.Dot(projection, -chest.forward) < 0)
        {
            // Approximate to small forward swing
            Debug.Log("reverse the direction");
            projection = 0.2f * (-chest.forward);
        }

        Vector3 targetOffset = -0.25f * chest.up * swingMagnitude;
        Vector3 swingDir;

        if (isOuter)
        {
            swingDir = (projection + perpendicular * swingAngleMagnitude).normalized;
            finalTarget = shoulder_bone.position + swingDir * swingMagnitude + targetOffset;
        }
        else
        {
            perpendicular = MirrorInPlane(chest.forward, chest.up, perpendicular);
            swingDir = (projection + perpendicular * swingAngleMagnitude * inner_side_factor).normalized;
            finalTarget = shoulder_bone.position + swingDir * swingMagnitude + targetOffset;
        }

        finalRot = Quaternion.LookRotation(swingDir, (home.position - finalTarget).normalized);
    }
    public void TrySwing()
    {
        //Debug.Log("TrySwing Triggered");

        Vector3 headToChest = (chest.position - hint_bone_first.position).normalized;
        Vector3 chestToArm = (arm_bone.position - chest.position).normalized;
        float angle = Mathf.Abs(Vector3.Angle(headToChest, chestToArm));
        if (angle < swingTriggerAngle)
            return; // if the angle is too small return. 

        Vector3 finalTarget;
        Quaternion finalRot;
        bool isOuter;
        ComputeSwingTargetAndRotation(
            chest,
            hint_bone_first,
            arm_bone,
            shoulder_bone,
            swingTriggerAngle,
            swingMagnitude,
            swingAngleMagnitude,
            out isOuter,
            out finalTarget,
            out finalRot
            );
        bool isOutSide = MovingOutSide(finalTarget) && isOuter;

        bool distance_check = Vector3.Distance(transform.position, finalTarget) > distance_threshould;

        endPos = finalTarget;
        endRot = finalRot;


        if (Moving) return;


        if ((!Moving) && distance_check)
        {
            moveCoroutine = StartCoroutine(SwingCoroutine(isOutSide));
        }
    }

    IEnumerator SwingCoroutine(bool isOutSide)
    {
        // Indicate we're moving
        Moving = true;
        startPoint = transform.position;
        startRot = transform.rotation;


        //calculate the relative pos
        Vector3 relative_pos = endPos - shoulder_bone.position;



        // 中点抬升形成弧线
        //Vector3 centerPoint = (transform.position + endPos) / 2f;
        //centerPoint += -chest.up * 0.25f / 2f;

        float timeElapsed = 0;

        Vector3 prepare_pos = transform.position;
        Quaternion prepare_rot = transform.rotation;
        float prepare_time = 0f;

        if (isOutSide)
        {
            //calculate a middle point via current rotation 
            prepare_pos = chest.position + chest.forward * 1.3f + chest.up * -0.8f + (shoulder_bone.position - chest.position).normalized * 1.5f;
            prepare_rot = Quaternion.LookRotation(chest.forward, chest.up);
            prepare_time = 0.5f;

        }

        float totaltime = swingDuration + prepare_time;

        while (timeElapsed < (totaltime))
        {
            if (isOutSide)
            {
                if (timeElapsed <= prepare_time)
                {
                    // First half (0.0–0.5): slow easing from start to mid
                    float t1 = timeElapsed / (prepare_time); // remap to [0,1]
                    //float easedT = Easing.EaseInOutQuad(t1); // slow ease
                    transform.position = Vector3.Lerp(startPoint, prepare_pos, t1);
                    transform.rotation = Quaternion.Slerp(startRot, prepare_rot, t1);
                }
                else if (timeElapsed < totaltime)
                {
                    // Second half (0.5–1.0): fast easing from mid to end
                    float t2 = (timeElapsed - prepare_time) / (swingDuration); // remap to [0,1]
                    //float easedT = Easing.EaseOutQuad(t2); // fast ease
                    Vector3 control_vector = 1.5f * calculate_controll_vector(prepare_pos, endPos, chest.position);
                    Vector3 centerPoint = (prepare_pos + endPos) / 2 + control_vector;
                    transform.position = Bezier(prepare_pos, centerPoint, endPos, t2);
                    transform.rotation = Quaternion.Slerp(prepare_rot, endRot, t2);
                }
            }
            else
            {
                // Apply easing
                float normalizedTime = timeElapsed / totaltime;
                float easedT = Easing.EaseInOutCubic(normalizedTime);
                //Vector3 control_vector = 1.5f * calculate_controll_vector(startPoint, endPos, chest.position);
                //Vector3 centerPoint = (startPoint + endPos) / 2;
                ////control_vector;
                //transform.position = Bezier(startPoint, centerPoint, endPos, easedT);
                transform.position = Vector3.Lerp(startPoint, endPos, easedT);
                transform.rotation = Quaternion.Slerp(startRot, endRot, easedT);
            }

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        //// 最终对齐
        transform.position = endPos;
        transform.rotation = endRot;
        moveCoroutine = null;
        Moving = false;
    }


    Vector3 calculate_controll_vector(Vector3 prepare_pos, Vector3 endPos, Vector3 chestpos)
    {

        Vector3 A = prepare_pos;
        Vector3 B = endPos;
        Vector3 C = chestpos;

        // Step 1: AB vector and midpoint
        Vector3 AB = B - A;
        Vector3 mid = (A + B) * 0.5f;

        // Step 2: Plane normal using triangle ABC
        Vector3 planeNormal = Vector3.Cross(B - A, C - A).normalized;

        // Step 3: Outward swing direction: perpendicular to AB, lying on plane
        Vector3 swingDir = Vector3.Cross(planeNormal, AB).normalized;

        // Step 4: Make sure it's pointing **away** from the body (optional check)
        if (Vector3.Dot(swingDir, mid - C) < 0)
            swingDir = -swingDir;

        return swingDir;

    }


    void OnDrawGizmos()
    {

        //// get the rotation direction from the two hint bones. 
        Vector3 finalTarget;
        Quaternion finalRot;
        bool isOuter;
        ComputeSwingTargetAndRotation(
            chest,
            hint_bone_first,
            arm_bone,
            shoulder_bone,
            swingTriggerAngle,
            swingMagnitude,
            swingAngleMagnitude,
            out isOuter,
            out finalTarget,
            out finalRot
        );
        if (isOuter)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }
        Gizmos.DrawLine(finalTarget, shoulder_bone.position);

    }


    Vector3 BezierQuadratic(Vector3 a, Vector3 c, Vector3 b, float t)
    {
        // (1 - t)^2 * a + 2(1 - t)t * c + t^2 * b
        return Mathf.Pow(1 - t, 2) * a + 2 * (1 - t) * t * c + Mathf.Pow(t, 2) * b;
    }


    Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        Vector3 lerp1 = Vector3.Lerp(p0, p1, t);
        Vector3 lerp2 = Vector3.Lerp(p1, p2, t);
        return Vector3.Lerp(lerp1, lerp2, t);
    }


}
