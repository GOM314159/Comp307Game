using UnityEngine;

public class PlayerHeadBob : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    [SerializeField] private Transform cameraTransform;

    [Header("Walking Head Bob")]

    // Adjust how fast the camera bobs while walking
    [SerializeField] private float walkBobFrequency = 8f;

    // Adjust how much the camera moves while walking
    [SerializeField] private float walkBobAmount = 0.04f;


    [Header("Sprinting Head Bob")]

    // Adjust how fast the camera bobs while sprinting
    [SerializeField] private float sprintBobFrequency = 12f;

    // Adjust how much the camera moves while sprinting
    [SerializeField] private float sprintBobAmount = 0.07f;


    [Header("Crouching Head Bob")]

    // Adjust how fast the camera bobs while crouching
    [SerializeField] private float crouchBobFrequency = 6f;

    // Adjust how much the camera moves while crouching
    [SerializeField] private float crouchBobAmount = 0.025f;


    [Header("Camera Return")]

    // Adjust how quickly the camera returns to its normal position
    [SerializeField] private float returnSpeed = 8f;


    private float bobTimer;

    private Vector3 originalCameraPosition;

    private void Start()
    {
        // Remember the camera's starting local position
        originalCameraPosition = cameraTransform.localPosition;
    }


    private void Update()
    {
        HandleHeadBob();
    }


    private void HandleHeadBob()
    {

        Vector3 horizontalVelocity = playerMovement.GetHorizontalVelocity();

        float movementAmount = horizontalVelocity.magnitude;

        // Player isn't moving

        if (movementAmount < 0.1f)
        {
            bobTimer = 0f;

            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                originalCameraPosition,
                returnSpeed * Time.deltaTime
            );

            return;
        }
        // Determine current movement state

        float frequency;
        float amount;

        if (playerMovement.IsCrouching())
        {
            frequency = crouchBobFrequency;
            amount = crouchBobAmount;
        }
        else if (playerMovement.IsSprinting())
        {
            frequency = sprintBobFrequency;
            amount = sprintBobAmount;
        }
        else
        {
            frequency = walkBobFrequency;
            amount = walkBobAmount;
        }

        bobTimer += Time.deltaTime * frequency;

        // Calculate bob movement

        float bobOffsetY = Mathf.Sin(bobTimer) * amount;

        float bobOffsetX =
            Mathf.Cos(bobTimer * 0.5f) * amount * 0.5f;

        Vector3 targetPosition = originalCameraPosition;

        targetPosition.y += bobOffsetY;
        targetPosition.x += bobOffsetX;

        // Smooth camera movement
        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            targetPosition,
            returnSpeed * Time.deltaTime
        );
    }
}