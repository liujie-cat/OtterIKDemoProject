using UnityEngine;
using System.Collections;
using UnityEngine;


public class OtterBackLegSwimmer : MonoBehaviour
{
    [Header("CoreReference")]
    [SerializeField] public Transform footTarget;
    [SerializeField] public Transform relax;
    [SerializeField] public Transform prepare;
    [SerializeField] public Transform thigh;

    [Header("Overshoot")]
    public bool enableOvershoot = true;
    public Vector3 overshootOffset;

    [Header("Tuning Parameter")]
    [SerializeField] public Transform Knee;
    [SerializeField] public float RetractTime = 0.3f;
    [SerializeField] public float KickTime = 0.3f;
    [SerializeField] public float RecoverTime = 0.5f;
    [SerializeField] float kickMagnitude = 2f;

    public enum LegState { Retract, Kick, Relax, Recover}
    public bool Moving { get; private set; }
    // private Vector3 desiredFootPos;
    public float radius = 0.5f;
    //[SerializeField] Transform hint_bone;
    [SerializeField] Transform A_Mimic;
    [SerializeField] Transform C_Mimic;
    [SerializeField] float velocityScale = 1f;
    [SerializeField] public float currentSpeedScale = 1;
    [SerializeField] public Vector3 targetVelocity;
    [SerializeField] public LegState currentState = LegState.Relax;
    public float maxSwimSpeed;
    private Vector3 currentSpeed;
    private void Awake()
    {
        currentState = LegState.Relax;
        //TrySwing();
    }

    public void FixedUpdate()
    {
        if (currentSpeedScale < 0.2f)
        {
            currentSpeedScale = 0;
        }
        currentSpeed = currentSpeedScale * Vector3.Normalize(A_Mimic.position - C_Mimic.position);
        if (currentState == LegState.Relax) {
            RecoverRelax();
        }
    }

    public void TrySwing()
    {
        if (Moving)
        {
            return;
        }

        switch (currentState)
        {
            case LegState.Retract:
                //If the leg is retracting. 
                break;
            case LegState.Recover:
                TryRecover();
                break;
            case LegState.Kick:
                //If the leg is kicking
                TryKick();
                break;
            case LegState.Relax:
                // Try retracting if the desired speed is not zero
                TryRetract(out bool willRetract);
                break;
        }
    }

    IEnumerator Retract(Vector3 targetPos, Quaternion targetRot)
    {
        Moving = true;
        Vector3 start_position = footTarget.position;
        Quaternion start_rotation = footTarget.rotation;
        Vector3 currPosition = footTarget.position;

        float hip_distance = Vector3.Distance(start_position, prepare.position);
        float timeElapsed = 0f;
        while (timeElapsed < RetractTime)
        {
            float normalizeTime = timeElapsed / RetractTime;
            footTarget.position = Vector3.Lerp(start_position, targetPos, normalizeTime);
            hip_distance = Vector3.Distance(footTarget.position, prepare.position);
            footTarget.rotation = Quaternion.Slerp(start_rotation, targetRot, normalizeTime);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        Moving = false;
        currentState = LegState.Kick;
        // check whether the currentDirection is still correct 

    }


    IEnumerator Recover()
    {
        Moving = true;
        Vector3 start_position = footTarget.position;
        Quaternion start_rotation = footTarget.rotation;
        Quaternion target_rotation = GetTargetRot();
        float timeElapsed = 0f;
        while (timeElapsed < RecoverTime)
        {
            float normalizeTime = timeElapsed / RecoverTime;
            footTarget.position = Vector3.Lerp(start_position, relax.position, normalizeTime);
            footTarget.rotation = Quaternion.Slerp(start_rotation, target_rotation, normalizeTime);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        Moving = false;
        currentState = LegState.Relax;
        // check whether the currentDirection is still correct 

    }


    public void RecoverRelax() { 
        footTarget.position = relax.position;
        footTarget.rotation = GetTargetRot();

    }

    public void TryKick() {
        if (Moving)
        {
            Debug.Log("It is Moving");
            return;
        }
        Vector3 swimDir = Vector3.Normalize(-currentSpeed);
        Debug.Log("Current speed " + currentSpeed);
        GetLegPrepareDirection(swimDir, out bool isValid, out Vector3 TargetPos);
        Vector3 kick_target_pos;
        //if (isValid) {
        //    isValid = Vector3.Magnitude(currentSpeed) < maxSwimSpeed;
        //}

        Debug.Log("Is valid " + isValid);

        if (!isValid)
        {
            Debug.Log("The target kick is not valid");
            currentState = LegState.Recover;
            return;
        }
        else {

            Vector3 kick_target_dir = Vector3.Normalize(TargetPos - thigh.position);
            Debug.Log("The target kick is valid");
            Vector3 offset = kick_target_dir * kickMagnitude;
            Quaternion TargetRot = GetTargetRot();
            StartCoroutine(Kick(offset, TargetRot));
        }
        

    }

    public void TryRecover()
    {
        if (Moving) return;
        StartCoroutine(Recover());
    }

    public Quaternion GetTargetRot() {
        Vector3 direction = Vector3.Normalize(Knee.position - thigh.position);
        Quaternion finalRot = Quaternion.LookRotation(direction);
        return finalRot;
    }



    public static Vector3 GetBCDirection(Vector3 A, Vector3 B, Vector3 D)
    {
        Vector3 AB = B - A;
        Vector3 DA = A - D;

        // 检查是否垂直
        if (Mathf.Abs(Vector3.Dot(AB.normalized, DA.normalized)) < 1e-4f)
        {
            return DA.normalized;
        }

        // 构造一个垂直于 AB 的平面方向
        Vector3 perpDir = Vector3.Cross(AB, Vector3.Cross(DA, AB)).normalized;

        // 可能共线或太接近无法判断
        if (perpDir == Vector3.zero)
        {
            return DA.normalized;
        }

        return perpDir;
    }


    private IEnumerator Kick(Vector3 offset, Quaternion TargetRotation) {
       
        Moving = true;
        float timeElapsed = 0;
        Vector3 start_position = footTarget.position;
        Quaternion start_rotation = footTarget.rotation;

        while (timeElapsed < KickTime)
        {
            //Debug.Log("Kick");
//Debug.Log("This is the target location" +TargetPos);
            float normalizeTime = timeElapsed / KickTime;
            footTarget.position = Vector3.Lerp(start_position, offset+thigh.position, normalizeTime);
            footTarget.rotation = Quaternion.Slerp(start_rotation, TargetRotation, normalizeTime);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        currentState = LegState.Recover;
        Moving = false;
        transform.position = offset + thigh.position;
        transform.rotation = TargetRotation;
    }
    

    public void TryRetract(out bool willRetract) 
    {
        if (Moving) 
        {
            willRetract = false;
            return ;
        }
        //first check current speed
        willRetract = true;
        if (Vector3.Magnitude(currentSpeed) > 3f){
            willRetract = false;
            return;
        }
        Vector3 swimDir = - Vector3.Normalize(currentSpeed);
        GetLegPrepareDirection(swimDir, out bool isValid, out Vector3 TargetPos);
        if (!isValid) 
        {
            willRetract = false;
            return;
        }      
        else
        {
            TargetPos = CalculateOvershoot(TargetPos, overshootOffset);
            currentState = LegState.Retract;
            Quaternion TargetRot = GetTargetRot();
            StartCoroutine(Retract(TargetPos, TargetRot));
        }
    }


    Vector3 CalculateOvershoot(Vector3 targetpos, Vector3 overShootOffset) {


       Vector3 offset = new Vector3(
       Random.Range(-overShootOffset.x, overShootOffset.x),
       Random.Range(-overShootOffset.y, overShootOffset.y),
       Random.Range(-overShootOffset.z, overShootOffset.z)
       );
       return targetpos + offset;

    }

    void GetLegPrepareDirection(Vector3 dir, out bool isValid, out Vector3 targetPos)
    {
        Vector3 A = thigh.position;
        Vector3 B = prepare.position;
        dir = dir.normalized;
        isValid = true;
        targetPos = Vector3.one;
        Vector3 axis = B - A;
        float height = axis.magnitude;
        Vector3 axisDir = axis.normalized;
        float cosTheta = height / Mathf.Sqrt(height * height + radius * radius);
        float cosToDir = Vector3.Dot(dir, axisDir);

        // Check if pointing backwards
        if (cosToDir < 0)
        {
            Debug.Log("It is backward of the cone");
            isValid = false; // Opposite direction
            return;
        }

        // Compute intersection point with plane at B
        Plane basePlane = new Plane(axisDir, B);
        if (!basePlane.Raycast(new Ray(A, dir), out float distanceToPlane))
        {
            isValid = false; // Should not happen unless degenerate
            Debug.Log("Fail to hit the target");
            return;
        }

        Vector3 intersection = A + dir * distanceToPlane;
        float distanceFromCenter = Vector3.Distance(intersection, B);

        if (cosToDir >= cosTheta)
        {
            // Inside cone, and intersects within the base circle
            if (distanceFromCenter <= radius)
            {
                targetPos = intersection;
                isValid = true;
                return;
            }
        }
        else {
            Vector3 toEdge = (intersection - B).normalized * radius;
            targetPos = B + toEdge;
            isValid = true;
            return;
        }
        // Outside the cone or outside the circle: return nearest point on rim

    }
    //void OnDrawGizmos()
    //{
    //    if (thigh == null) return;

    //    // Draw inverse of current velocity from the thigh
    //    Gizmos.color = Color.cyan;
    //    Vector3 velocityDir = -currentSpeed;
    //    Gizmos.DrawLine(thigh.position, thigh.position + velocityDir*5f);

    //    // Predict and draw retract point
    //    Vector3 swimDir = Vector3.Normalize(velocityDir);
    //    GetLegPrepareDirection(swimDir, out bool isValid, out Vector3 retractTarget);
    //    if (enableOvershoot && isValid)
    //    {
    //        retractTarget = CalculateOvershoot(retractTarget, overshootOffset);
    //    }

    //    Gizmos.color = isValid ? Color.green : Color.gray;
    //    Gizmos.DrawSphere(retractTarget, 0.05f);

    //    // Draw kick point
    //    if (isValid)
    //    {
    //        Vector3 kickDir = (retractTarget - thigh.position).normalized;
    //        Vector3 kickTarget = thigh.position + kickDir * kickMagnitude;
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawSphere(kickTarget, 0.05f);
    //    }

    //    // === 4. Draw the valid retraction cone ===
    //    Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // Orange and semi-transparent
    //    Vector3 coneApex = thigh.position;
    //    Vector3 coneAxis = (prepare.position - thigh.position);
    //    float height = coneAxis.magnitude;
    //    Vector3 coneDir = coneAxis.normalized;

    //    // Base of the cone
    //    int segments = 20;
    //    Vector3 baseCenter = prepare.position;
    //    Vector3 ortho1 = Vector3.Cross(coneDir, Vector3.up).normalized;
    //    if (ortho1 == Vector3.zero) ortho1 = Vector3.Cross(coneDir, Vector3.forward).normalized;
    //    Vector3 ortho2 = Vector3.Cross(coneDir, ortho1).normalized;

    //    Vector3[] circlePoints = new Vector3[segments + 1];
    //    for (int i = 0; i <= segments; i++)
    //    {
    //        float angle = i * Mathf.PI * 2 / segments;
    //        Vector3 point = baseCenter + (ortho1 * Mathf.Cos(angle) + ortho2 * Mathf.Sin(angle)) * radius;
    //        circlePoints[i] = point;

    //        // Draw lines from apex to base circle
    //        Gizmos.DrawLine(coneApex, point);

    //        // Connect base circle points
    //        if (i > 0)
    //            Gizmos.DrawLine(circlePoints[i - 1], circlePoints[i]);
    //    }

    //    // Optional: draw circle center
    //    Gizmos.DrawWireSphere(baseCenter, 0.01f);
    //}
}
