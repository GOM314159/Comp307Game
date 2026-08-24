using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Procedurally animates the CharacterHandModel while it is
// holding the axe. No animation clips required.
//
// Put this on the hand's ViewModel object under the camera,
// and parent the axe model to the hand's grip socket.
public class HandAxeAnimator : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private PlayerMovement playerMovement;


    [Header("Idle Sway")]

    // Adjust how fast the hand drifts while standing still
    [SerializeField] private float idleFrequency = 1.2f;

    // Adjust how far the hand drifts while standing still
    [SerializeField] private float idleAmount = 0.01f;


    [Header("Walking Sway")]

    // Adjust how fast the hand bobs while moving
    [SerializeField] private float walkFrequency = 7f;

    // Adjust how much the hand bobs while moving
    [SerializeField] private float walkAmount = 0.035f;

    // Adjust how quickly the hand settles back to its rest pose
    [SerializeField] private float returnSpeed = 10f;


    [Header("Swing Timing")]

    // How long the axe is pulled back before the swing.
    [SerializeField] private float windUpDuration = 0.18f;

    // How long the forward swing itself takes.
    [SerializeField] private float swingDuration = 0.12f;

    // How long the hand takes to settle back to rest.
    [SerializeField] private float recoverDuration = 0.25f;

    // Minimum time between swings, in seconds.
    [SerializeField] private float swingCooldown = 0.6f;


    [Header("Swing Shape")]

    // How far the axe tilts back during the wind up, in degrees.
    [SerializeField] private float windUpAngle = 45f;

    // How far the axe chops forward and down, in degrees.
    [SerializeField] private float swingAngle = 70f;

    // Sideways twist that makes the chop feel diagonal.
    [SerializeField] private float swingTwist = 20f;

    // How far the hand pulls back during the wind up.
    [SerializeField] private Vector3 windUpOffset =
        new Vector3(0.04f, 0.06f, -0.1f);

    // How far the hand drives forward during the chop.
    [SerializeField] private Vector3 swingOffset =
        new Vector3(-0.05f, -0.12f, 0.18f);


    [Header("Impact")]

    // Fired at the exact moment the chop lands.
    // Hook damage, sound or particles up here in the Inspector.
    public UnityEvent onImpact;


    private Vector3 restPosition;

    private Quaternion restRotation;

    private float swayTimer;

    private float nextSwingTime;

    private bool isSwinging;


    private void Start()
    {
        // Remember the pose the hand starts in
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
    }


    private void Update()
    {
        // The swing drives the hand itself
        if (isSwinging)
        {
            return;
        }

        HandleSway();
    }


    // IDLE SWAY

    private void HandleSway()
    {
        float speed = playerMovement != null
            ? playerMovement.GetHorizontalVelocity().magnitude
            : 0f;

        bool isMoving = speed > 0.1f;

        float frequency = isMoving ? walkFrequency : idleFrequency;
        float amount = isMoving ? walkAmount : idleAmount;

        if (isMoving && playerMovement.IsSprinting())
        {
            frequency *= 1.4f;
            amount *= 1.5f;
        }

        swayTimer += Time.deltaTime * frequency;

        // Figure-eight motion so the hand never looks locked off
        Vector3 targetPosition = restPosition;

        targetPosition.x += Mathf.Cos(swayTimer * 0.5f) * amount;
        targetPosition.y += Mathf.Sin(swayTimer) * amount * 0.5f;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            returnSpeed * Time.deltaTime
        );

        // The axe is heavy, so let it tip with the bob
        Quaternion targetRotation =
            restRotation
            * Quaternion.Euler(
                Mathf.Sin(swayTimer) * amount * 60f,
                Mathf.Cos(swayTimer * 0.5f) * amount * 60f,
                0f);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            returnSpeed * Time.deltaTime
        );
    }


    // SWING

    // Call this when the player attacks with the axe.
    public void PlaySwing()
    {
        if (isSwinging || Time.time < nextSwingTime)
        {
            return;
        }

        nextSwingTime = Time.time + swingCooldown;

        StartCoroutine(SwingRoutine());
    }


    // True while a swing is playing, so other scripts
    // can avoid starting a second one.
    public bool IsSwinging()
    {
        return isSwinging;
    }


    private IEnumerator SwingRoutine()
    {
        isSwinging = true;

        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;

        // Pose at the top of the wind up
        Vector3 windUpPosition = restPosition + windUpOffset;

        Quaternion windUpRotation =
            restRotation
            * Quaternion.Euler(-windUpAngle, 0f, -swingTwist);

        // Pose at the bottom of the chop
        Vector3 chopPosition = restPosition + swingOffset;

        Quaternion chopRotation =
            restRotation
            * Quaternion.Euler(swingAngle, 0f, swingTwist);


        // 1. WIND UP - slow pull back, eased out
        yield return Blend(
            startPosition, startRotation,
            windUpPosition, windUpRotation,
            windUpDuration, true);

        // 2. CHOP - fast drive forward, no easing
        yield return Blend(
            windUpPosition, windUpRotation,
            chopPosition, chopRotation,
            swingDuration, false);

        // The axe head has landed
        onImpact.Invoke();

        // 3. RECOVER - drift back to the rest pose
        yield return Blend(
            chopPosition, chopRotation,
            restPosition, restRotation,
            recoverDuration, true);

        // Snap the pose back so sway resumes cleanly
        transform.localPosition = restPosition;
        transform.localRotation = restRotation;

        swayTimer = 0f;
        isSwinging = false;
    }


    // Moves the hand from one pose to another over time.
    // Set smooth to false for the fast, linear chop.
    private IEnumerator Blend(
        Vector3 fromPosition, Quaternion fromRotation,
        Vector3 toPosition, Quaternion toRotation,
        float duration, bool smooth)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            if (smooth)
            {
                progress = Mathf.SmoothStep(0f, 1f, progress);
            }

            transform.localPosition = Vector3.Lerp(
                fromPosition, toPosition, progress);

            transform.localRotation = Quaternion.Slerp(
                fromRotation, toRotation, progress);

            yield return null;
        }
    }
}
