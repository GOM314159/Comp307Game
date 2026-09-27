using UnityEngine;

/// <summary>
/// Scene-local binding for a case.
///
/// This component belongs INSIDE the level scene.
/// It contains references to objects that only exist in
/// that scene.
///
/// The persistent CaseManager finds this component after
/// the scene loads.
///
/// Example:
///
/// Level 1
///     CaseSceneBinding
///         Case ID = 0
///         Required Object = Axe
///         Side Quest Item = White Powder
///
/// Level 2
///     CaseSceneBinding
///         Case ID = 1
///         Required Object = Cable Cutter
///         Side Quest Item = Blue Tablet
/// </summary>
public class CaseSceneBinding : MonoBehaviour
{
    // =========================================================
    // CASE
    // =========================================================

    [Header("Case")]

    [Tooltip(
        "Must match the Case ID configured in CaseManager."
    )]
    [SerializeField]
    private int caseID = 0;


    // =========================================================
    // MAIN OBJECT
    // =========================================================

    [Header("Main Objective")]

    [Tooltip(
        "The WorldInteractable that completes the main " +
        "objective for this case."
    )]
    [SerializeField]
    private WorldInteractable requiredObject;


    // =========================================================
    // SIDE QUEST
    // =========================================================

    [Header("Side Quest")]

    [Tooltip(
        "The WorldInteractable that completes the side " +
        "quest for this case."
    )]
    [SerializeField]
    private WorldInteractable sideQuestItem;


    // =========================================================
    // GETTERS
    // =========================================================

    public int GetCaseID()
    {
        return caseID;
    }


    public WorldInteractable GetRequiredObject()
    {
        return requiredObject;
    }


    public WorldInteractable GetSideQuestItem()
    {
        return sideQuestItem;
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        if (requiredObject == null)
        {
            Debug.LogWarning(
                "[CaseSceneBinding] Required Object is not assigned " +
                "on " + gameObject.name + ".",
                this
            );
        }

        if (sideQuestItem == null)
        {
            Debug.LogWarning(
                "[CaseSceneBinding] Side Quest Item is not assigned " +
                "on " + gameObject.name + ".",
                this
            );
        }
    }
}