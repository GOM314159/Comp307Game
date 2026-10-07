using UnityEngine;

public class CameraAttachment : MonoBehaviour
{
    [Header("Camera Assignment")]
    [Tooltip("Drag your Main/First Person Camera here")]
    public Transform playerCamera;

    [Tooltip("Adjust this to move the camera slightly forward so you don't see the inside of the neck")]
    public Vector3 cameraOffset = new Vector3(0f, 0.1f, 0.15f);

    [Tooltip("Adjust this if the bone's default rotation makes the camera look off-center")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Rig Settings")]
    [Tooltip("Check this if your models use Unity's Humanoid Rig type")]
    public bool isHumanoidRig = true;

    [Tooltip("If not Humanoid, type the exact name of the head bone (e.g., 'mixamorig:Head')")]
    public string genericHeadBoneName = "head";



    /// <summary>
    /// Call this method from your model spawning script right after Instantiate().
    /// </summary>
    /// <param name="spawnedModel">The newly instantiated character model prefab.</param>
    public void AttachCameraToHead(GameObject spawnedModel)
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player Camera is not assigned in the inspector!");
            return;
        }

        Transform headBone = null;

        // METHOD 1: Cleanest approach for Humanoid rigs
        if (isHumanoidRig)
        {
            Animator anim = spawnedModel.GetComponent<Animator>();
            if (anim != null && anim.isHuman)
            {
                headBone = anim.GetBoneTransform(HumanBodyBones.Head);
                Debug.Log("Camera attached to Humanoid head bone: " + headBone.name); 
            }
            else
            {
                Debug.LogWarning("Model does not have a Humanoid Animator. Trying generic search...");
                headBone = FindBoneByName(spawnedModel.transform, genericHeadBoneName);
            }
        }
        //Fallback for Generic rigs
        else
        {
            headBone = FindBoneByName(spawnedModel.transform, genericHeadBoneName);
            Debug.Log("Camera attached to Generic head bone: " + headBone?.name);
        }

        // Parent and Reset
        if (headBone != null)
        {
            playerCamera.SetParent(headBone);

            // Snap to the bone's pivot, then apply the offset to avoid rendering the neck gap
            playerCamera.localPosition = cameraOffset;
            playerCamera.localRotation = Quaternion.identity;
            playerCamera.localRotation = Quaternion.Euler(rotationOffset);

            Debug.Log("Camera successfully locked to: " + headBone.name);
        }
        else
        {
            Debug.LogError("Could not find the Head bone on the spawned model. Check rig settings or bone name.");
        }
    }

    // Helper method to recursively search for a bone by string name
    private Transform FindBoneByName(Transform parent, string name)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name == name)
            {
                return child;
            }
        }
        return null;
    }
}