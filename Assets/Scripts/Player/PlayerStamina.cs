using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{

    [Header("Stamina Settings")]

    // Adjust maximum stamina here
    [SerializeField] private float maxStamina = 100f;

    // Adjust maximum stamina here
    [SerializeField] private float minStamina = 0f;

    // Adjust stamina drain here
    [SerializeField] private float staminaDrain = 20f;

    // Adjust stamina regeneration here
    [SerializeField] private float staminaRegen = 15f;

    // Adjust regeneration delay here
    [SerializeField] private float regenDelay = 1.5f;

    // Adjust sprint acceleration here
    [SerializeField] private float sprintTransitionSpeed = 8f;

    // =========================================================
    // EVENT CALLS FOR STAMINA GUI
    // =========================================================

    // DEBUG


    [Header("Debug")]

    [SerializeField]
    private bool debugMessages = true;

    private float currentStamina;

    private float regenTimer;

    private bool sprintEnabled = true;

    private bool isSprinting;

    private float sprintMultiplier;

    public enum SprintBlockReason
    {
        None,
        NoStamina,
        DisabledByGame
    }

    private SprintBlockReason blockReason;

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        UpdateStamina();

        UpdateSprintTransition();

    }

    public float GetMovementSpeed(
        float walkSpeed,
        float sprintSpeed,
        float crouchSpeed,
        bool isCrouching)
    {
        if (isCrouching)
            return crouchSpeed;

        return Mathf.Lerp(
            walkSpeed,
            sprintSpeed,
            sprintMultiplier);
    }



    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }

    public void UpdateStamina()
    {
        if (isSprinting && CanSprint())
        {
            currentStamina -= staminaDrain * Time.deltaTime;

            currentStamina = Mathf.Max(0, currentStamina);

            regenTimer = 0;
        }
        else
        {
            regenTimer += Time.deltaTime;

            if (regenTimer >= regenDelay)
            {   
                currentStamina += staminaRegen * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }
    }
    private void UpdateSprintTransition()
    {
        float target = (isSprinting && CanSprint()) ? 1f : 0f;

        sprintMultiplier = Mathf.MoveTowards(
            sprintMultiplier,
            target,
            sprintTransitionSpeed * Time.deltaTime);
    }


    public bool CanSprint()
    {
        if (!sprintEnabled)
        {
            blockReason = SprintBlockReason.DisabledByGame;
            return false;
        }

        if (currentStamina <= 0)
        {
            blockReason = SprintBlockReason.NoStamina;
            return false;
        }

        blockReason = SprintBlockReason.None;

        return true;
    }

    public float getmaxStamina()
    {
       return maxStamina;
    }

    public void EnableSprint() //gonna be amazing for cutscenes or really scary parts
    {
        sprintEnabled = true;

        if (debugMessages)
            Debug.Log("Sprint Enabled");
    }

    public void DisableSprint()  //gonna be amazing for cutscenes or really scary parts
    {
        sprintEnabled = false;

        if (debugMessages)
            Debug.Log("Sprint Disabled");
    }

    // =========================================================
    // PERMANENT STAMINA BOOST
    // =========================================================

    public void ApplyPermanentStaminaBoost(float amount)
    {
        if (amount <= 0f)
        {
            Debug.LogWarning(
                "[PlayerStamina] Invalid stamina increase: "
                + amount
            );

            return;
        }


        // Increase maximum stamina.
        maxStamina += amount;


        // Give the player the newly gained stamina immediately.
        currentStamina += amount;


        // Make absolutely sure current stamina
        // cannot exceed the new maximum.
        currentStamina = Mathf.Min(
            currentStamina,
            maxStamina
        );


        Debug.Log(
            "[PlayerStamina] Permanent stamina boost applied."
            + " | Increase: +"
            + amount
            + " | Max Stamina: "
            + maxStamina
            + " | Current Stamina: "
            + currentStamina
        );
    }

    public float CurrentStamina => currentStamina;

    public float MaxStamina => maxStamina;

    public SprintBlockReason BlockReason => blockReason;
}