using UnityEngine;

public class CameraAttachment : MonoBehaviour
{
    [Header("Camera Assignment")]
    [Tooltip("Drag your Main/First Person Camera here")]
    public Transform playerCamera;

    [Tooltip("Adjust this to move the camera slightly forward so you don't see the inside of the neck")]
    public Vector3 cameraOffset = new Vector3(0f, 0f, 0.15f);

    [Header("Rig Settings")]
    [Tooltip("Check this if your models use Unity's Humanoid Rig type")]
    public bool isHumanoidRig = true;

    [Tooltip("If not Humanoid, type the exact name of the head bone")]
    public string genericHeadBoneName = "head";

    [Header("Procedural Headbob")]
    [Tooltip("Enable to simulate footstep impact mathematically")]
    public bool useProceduralBob = true;

    [Tooltip("How fast the camera bounces. Should match your footstep audio interval.")]
    public float walkBobSpeed = 12f;

    [Tooltip("How high the camera bounces vertically in meters.")]
    public float bobAmount = 0.04f;

    // References
    private Transform headBone = null;
    private PlayerMovement playerMovement;
    private float bobTimer = 0f;

    public void AttachCameraToHead(GameObject spawnedModel)
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player Camera is not assigned!");
            return;
        }

        // Grab the movement script so we know when the player is walking vs sprinting
        playerMovement = GetComponent<PlayerMovement>();

        // Find the bone
        if (isHumanoidRig)
        {
            Animator anim = spawnedModel.GetComponent<Animator>();
            if (anim != null && anim.isHuman)
            {
                headBone = anim.GetBoneTransform(HumanBodyBones.Head);
            }
            else
            {
                headBone = FindBoneByName(spawnedModel.transform, genericHeadBoneName);
            }
        }
        else
        {
            headBone = FindBoneByName(spawnedModel.transform, genericHeadBoneName);
        }

        if (headBone != null)
        {
            // Magnet setup: Parent to root, not the bone
            playerCamera.SetParent(transform);
            Debug.Log("Camera successfully tracking position of: " + headBone.name);
        }
        else
        {
            Debug.LogError("Could not find the Head bone on the spawned model.");
        }
    }

    private void LateUpdate()
    {
        if (headBone != null && playerCamera != null)
        {
            // 1. Base Position from Bone (Maintains crouch drops)
            Vector3 targetPosition = headBone.position;

            // 2. Procedural Headbob (Adds the realism back)
            if (useProceduralBob && playerMovement != null)
            {
                // Only bob if we are actively providing input and on the floor
                if (playerMovement.IsMoving() && playerMovement.IsGrounded())
                {
                    // Sprinting makes the bob faster and slightly more aggressive
                    float currentSpeed = playerMovement.IsSprinting() ? walkBobSpeed * 1.4f : walkBobSpeed;
                    float currentAmount = playerMovement.IsSprinting() ? bobAmount * 1.2f : bobAmount;

                    bobTimer += Time.deltaTime * currentSpeed;

                    // Sine wave creates a smooth, continuous up-and-down arc
                    targetPosition.y += Mathf.Sin(bobTimer) * currentAmount;
                }
                else
                {
                    // Smoothly reset the timer so it doesn't snap abruptly when stopping
                    bobTimer = 0f;
                }
            }

            playerCamera.position = targetPosition;

            // 3. Apply Offset Directionally
            playerCamera.position += playerCamera.forward * cameraOffset.z;
            playerCamera.position += playerCamera.up * cameraOffset.y;
            playerCamera.position += playerCamera.right * cameraOffset.x;
        }
    }

    private Transform FindBoneByName(Transform parent, string name)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name.ToLower() == name.ToLower())
            {
                return child;
            }
        }
        return null;
    }
}