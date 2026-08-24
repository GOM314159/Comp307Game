using System.Collections;
using UnityEngine;

// Procedurally animates the CharacterHandModel while it is
// holding the wirecutters. No animation clips required.
//
// Put this on the CharacterHandModel object under the camera,
// and parent the wirecutters model to the hand's grip socket.
public class HandWirecuttersAnimator : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private PlayerMovement playerMovement;

    // The moving jaw of the wirecutters.
    [SerializeField] private Transform wirecutterJaw;


    [Header("Idle Sway")]

    // Adjust how fast the hand drifts while standing still
    [SerializeField] private float idleFrequency = 1.5f;

    // Adjust how far the hand drifts while standing still
    [SerializeField] private float idleAmount = 0.008f;


    [Header("Walking Sway")]

    // Adjust how fast the hand bobs while moving
    [SerializeField] private float walkFrequency = 8f;

    // Adjust how much the hand bobs while moving
    [SerializeField] private float walkAmount = 0.03f;

    // Adjust how quickly the hand settles back to its rest pose
    [SerializeField] private float returnSpeed = 10f;


    [Header("Cut Animation")]

    // How long a single cut takes, in seconds.
    [SerializeField] private float cutDuration = 0.35f;

    // How far the hand thrusts forward during a cut.
    [SerializeField] private float cutThrust = 0.12f;

    // How far the jaw closes during a cut, in degrees.
    [SerializeField] private float jawCloseAngle = 25f;


    private Vector3 restPosition;

    private Quaternion jawRestRotation;

    private float swayTimer;

    private bool isCutting;


    private void Start()
    {
        // Remember the pose the hand and jaw start in
        restPosition = transform.localPosition;

        if (wirecutterJaw != null)
        {
            jawRestRotation = wirecutterJaw.localRotation;
        }
    }


    private void Update()
    {
        // The cut animation drives the hand itself
        if (isCutting)
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
    }


    // CUT

    // Call this when the player uses the wirecutters.
    public void PlayCut()
    {
        if (isCutting)
        {
            return;
        }

        StartCoroutine(CutRoutine());
    }


    private IEnumerator CutRoutine()
    {
        isCutting = true;

        Vector3 startPosition = transform.localPosition;

        Vector3 thrustPosition =
            restPosition + Vector3.forward * cutThrust;

        Quaternion jawClosed =
            jawRestRotation
            * Quaternion.Euler(jawCloseAngle, 0f, 0f);

        float elapsed = 0f;

        while (elapsed < cutDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / cutDuration;

            // Punch out and pull back over the same curve
            float blend = Mathf.Sin(progress * Mathf.PI);

            transform.localPosition = Vector3.Lerp(
                startPosition,
                thrustPosition,
                blend
            );

            if (wirecutterJaw != null)
            {
                wirecutterJaw.localRotation = Quaternion.Slerp(
                    jawRestRotation,
                    jawClosed,
                    blend
                );
            }

            yield return null;
        }

        // Snap the pose back so sway resumes cleanly
        transform.localPosition = restPosition;

        if (wirecutterJaw != null)
        {
            wirecutterJaw.localRotation = jawRestRotation;
        }

        swayTimer = 0f;
        isCutting = false;
    }
}
