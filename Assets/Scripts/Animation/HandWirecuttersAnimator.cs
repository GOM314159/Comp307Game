using System.Collections;
using UnityEngine;

/// <summary>
/// Procedurally animates the CharacterHandModel while holding the wirecutters.
/// Attach to the hand model under the camera holding the wirecutters.
/// </summary>
public class HandWirecuttersAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    // The moving jaw of the wirecutters.
    [SerializeField] private Transform wirecutterJaw;

    [Header("Idle Sway")]
    [SerializeField] private float idleFrequency = 1.5f;
    [SerializeField] private float idleAmount = 0.008f;

    [Header("Walking Sway")]
    [SerializeField] private float walkFrequency = 8f;
    [SerializeField] private float walkAmount = 0.03f;
    [SerializeField] private float returnSpeed = 10f;

    [Header("Cut Animation")]
    [SerializeField] private float cutDuration = 0.35f;
    [SerializeField] private float cutThrust = 0.12f;
    [SerializeField] private float jawCloseAngle = 25f;

    private Vector3 restPosition;
    private Quaternion jawRestRotation;
    private float swayTimer;
    private bool isCutting;

    private void Start()
    {
        restPosition = transform.localPosition;

        if (wirecutterJaw != null)
        {
            jawRestRotation = wirecutterJaw.localRotation;
        }
    }

    private void Update()
    {
        if (isCutting)
        {
            return;
        }

        HandleSway();
    }

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

        Vector3 targetPosition = restPosition;
        targetPosition.x += Mathf.Cos(swayTimer * 0.5f) * amount;
        targetPosition.y += Mathf.Sin(swayTimer) * amount * 0.5f;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            returnSpeed * Time.deltaTime
        );
    }

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
        Vector3 thrustPosition = restPosition + Vector3.forward * cutThrust;
        Quaternion jawClosed = jawRestRotation * Quaternion.Euler(jawCloseAngle, 0f, 0f);

        float elapsed = 0f;

        while (elapsed < cutDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / cutDuration;
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

        transform.localPosition = restPosition;

        if (wirecutterJaw != null)
        {
            wirecutterJaw.localRotation = jawRestRotation;
        }

        swayTimer = 0f;
        isCutting = false;
    }
}