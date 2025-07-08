using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AnimationControllerMock: MonoBehaviour
{

    #region Head Tracking
    [SerializeField] Transform target;
    [Header("Head Tracking")]
    [SerializeField, Range(1, 3)] int headTrackingBoneCount = 1;
    [Space]
    [SerializeField] Transform headBone;
    [SerializeField] Transform spine2Bone;
    [SerializeField] Transform spine1Bone;
    [SerializeField] Transform Chest;

    [SerializeField] float headMaxTurnAngle = 70;
    [SerializeField] float headTrackingSpeed = 8f;

    Vector3 lastLocalHeadRotationEulers;
    SmoothDamp.EulerAngles currentLocalHeadEulerAngles;


    [Header("Root Motion")]
    [SerializeField] float turnSpeed = 100f;
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float turnAcceleration = 5f;
    [SerializeField] float moveAcceleration = 5f;
    [SerializeField] float minDistToTarget = 4.5f;
    [SerializeField] float maxDistToTarget = 6f;

    [Header("Limbs")]
    [SerializeField] OtterFrontArmSwimmer frontLeftLegStepper;
    [SerializeField] OtterFrontArmSwimmer frontRightLegStepper;
    //[SerializeField] OtterBackLegSwimmer backLeftLegStepper;
    //[SerializeField] OtterBackLegSwimmer backRightLegStepper;
    [SerializeField] float swingCooldown = 0.6f;
    [SerializeField] float leftSideStartOffset = 0f;     // optional if left starts at 0
    [SerializeField] float rightSideStartOffset = 0.2f;  // slight delay for right side


    [Header("Amplitude Settings")]
    public float maxAmplitude = 1f;     // Maximum allowed offset (clamped to ±1)
    public float maxSpeed = 5f;         // Speed at which amplitude and frequency are maxed
    public float minSpeedToWave = 2.5f; // ❗ Below this, no waving at all
    public Vector3 localBobDirection = Vector3.up;

    [Header("Frequency Settings")]
    public float baseFrequency = 1f;    // Frequency at low end (speed = minSpeedToWave)
    public float maxFrequency = 5f;     // Frequency at high spee

    SmoothDamp.Vector3 currentVelocity;
    SmoothDamp.Float currentAngularVelocity;
    private Vector3 originalLocalPosition;

    public void Awake()
    {
        Debug.Log("Hello, Unity Awaked!");
        StartLimbCoroutine();
        originalLocalPosition = Chest.localPosition;
    }
    void HeadTrackingUpdate()
    {
        // Freelook dir
        Vector3 targetLookDir = target.position - headBone.position;

        // Since we have multiple bones in the chain, we clamp the angle here from the root forward vector 
        // rather than from the bone's default orientation
        targetLookDir = Vector3.RotateTowards(transform.forward, targetLookDir, Mathf.Deg2Rad * headMaxTurnAngle, 0);

        // Look dir on the gecko's XZ plane
        Vector3 planarLookDir = Vector3.ProjectOnPlane(targetLookDir, transform.up).normalized;

        // If target is behind the gecko, we approach the planar look dir to prevent wacky up/down head rotations
        var dotProduct = Vector3.Dot(transform.forward, targetLookDir);
        if (dotProduct < 0)
        {
            targetLookDir = Vector3.Lerp(targetLookDir, Vector3.ProjectOnPlane(planarLookDir, transform.up), -dotProduct);
        }

        // Up dir is partially biased toward world up for a more interesting head rotation when upside down
        Quaternion targetWorldRotation = Quaternion.LookRotation(targetLookDir, Vector3.Slerp(transform.up, Vector3.up, 0.5f));

        // Get head world rotation when its local rotations are zero
        Quaternion defaultHeadRotation = headBone.rotation * Quaternion.Inverse(headBone.localRotation);

        // Move the look rotation to local space by "subtracting" the world rotation
        Quaternion targetLocalRotation = Quaternion.Inverse(defaultHeadRotation) * targetWorldRotation;

        // Since we apply this to each bone, the speed is multiplied by the bone count,
        // so we divide here to keep it constant
        float headTrackingSpeed = this.headTrackingSpeed / headTrackingBoneCount;

        currentLocalHeadEulerAngles.Step(targetLocalRotation.eulerAngles, headTrackingSpeed);

        headBone.localEulerAngles = currentLocalHeadEulerAngles;

        // Because the target rotation is derived from the last bone in the chain,
        // the angles will balance themselves out automatically, even if we don't apply
        // it to all three bones!

        if (headTrackingBoneCount > 1)
            spine1Bone.localEulerAngles = currentLocalHeadEulerAngles;
        else
            spine1Bone.localRotation = Quaternion.identity;

        if (headTrackingBoneCount > 2)
            spine2Bone.localEulerAngles = currentLocalHeadEulerAngles;
        else
            spine2Bone.localRotation = Quaternion.identity;

    }

    void RootMotionUpdate()
    {
        Vector3 towardTarget = target.position - transform.position;
        Vector3 towardTargetProjected = Vector3.ProjectOnPlane(towardTarget, transform.up);

        var angToTarget = Vector3.SignedAngle(transform.forward, towardTargetProjected, transform.up);

        float targetAngularVelocity = Mathf.Sign(angToTarget) * Mathf.InverseLerp(20f, 45f, Mathf.Abs(angToTarget)) * turnSpeed;
        currentAngularVelocity.Step(targetAngularVelocity, turnAcceleration);


        Vector3 targetVelocity = Vector3.zero;

        // Don't translate if we're facing away, rotate in place
        if (Mathf.Abs(angToTarget) < 90)
        {
            var distToTarget = towardTargetProjected.magnitude;

            // If we're too far away, move toward target
            if (distToTarget > maxDistToTarget)
            {
                targetVelocity = moveSpeed * towardTargetProjected.normalized;
            }
            // If we're too close, move in reverse
            else if (distToTarget < minDistToTarget)
            {
                // Speed also reduced since the stubby front legs can't keep up with full speed
                targetVelocity = moveSpeed * -towardTargetProjected.normalized * 0.66f;
            }

            // Limit velocity progressively as we approach max angular velocity,
            // so that above 20% of max angvel we start slowing down translation
            targetVelocity *= Mathf.InverseLerp(turnSpeed, turnSpeed * 0.2f, Mathf.Abs(currentAngularVelocity));
        }

        currentVelocity.Step(targetVelocity, moveAcceleration);

        // Apply translation and rotation
        transform.position += currentVelocity.currentValue * Time.deltaTime;
        transform.rotation *= Quaternion.AngleAxis(Time.deltaTime * currentAngularVelocity, transform.up);
        //backLeftLegStepper.currentSpeedScale = Vector3.Magnitude(currentVelocity.currentValue);
        //backRightLegStepper.currentSpeedScale = Vector3.Magnitude(currentVelocity.currentValue);
        //# backLeftLegStepper.targetVelocity = targetVelocity;

    }



    void ChestBobbingUpdate()
    {
        float speedMag = currentVelocity.currentValue.magnitude;

        if (speedMag < minSpeedToWave)
        {
            Chest.localPosition = originalLocalPosition;
            return;
        }

        float speedFactor = Mathf.Clamp01((speedMag - minSpeedToWave) / (maxSpeed - minSpeedToWave));
        float frequency = baseFrequency;
        float amplitude = Mathf.Lerp(0f, maxAmplitude, speedFactor);

        float wave = Mathf.Sin(Time.time * frequency); // [-1, 1]
        float offset = wave * amplitude;

        Chest.localPosition = originalLocalPosition + localBobDirection.normalized * offset;
    }

    #endregion
    void LateUpdate()
    {
        // Update order is important! 
        // We update things in order of dependency, so we update the body first via IdleBobbingUpdate,
        // since the head is moved by the body, then we update the head, since the eyes are moved by the head,
        // and finally the eyes.
        HeadTrackingUpdate();
        ChestBobbingUpdate();
    }

    void Update()
    {
        RootMotionUpdate();
    }

    void StartLimbCoroutine()
    {
        //StartCoroutine(SwingBackLoop(backLeftLegStepper, swingCooldown, leftSideStartOffset));
        //StartCoroutine(SwingBackLoop(backRightLegStepper, swingCooldown, rightSideStartOffset));
        StartCoroutine(SwingFrontLoop(frontLeftLegStepper, swingCooldown, leftSideStartOffset));
        StartCoroutine(SwingFrontLoop(frontRightLegStepper, swingCooldown, rightSideStartOffset));
    }

    IEnumerator SwingBackLoop(OtterBackLegSwimmer backLegStepper, float cooldown, float startOffset)
    {
        //yield return new WaitForSeconds(startOffset); // Offset at the beginning
        yield return new WaitForSeconds(startOffset); // Offset at the beginning
        while (true)
        {
            backLegStepper.TrySwing();
            yield return new WaitForSeconds(cooldown);
        }
    }

    IEnumerator SwingFrontLoop(OtterFrontArmSwimmer frontArmStepper, float cooldown, float startOffset)
    {
        yield return new WaitForSeconds(startOffset); // Offset at the beginning

        while (true)
        {
            frontArmStepper.TrySwing();
            yield return new WaitForSeconds(0.3f);

        }
    }




}
