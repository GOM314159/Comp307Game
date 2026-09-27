using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "PlayerAlive",
    story: "[Self] = [PlayerAlive]",
    category: "Conditions",
    id: "ced817f865c5cb018d2ccf1c46696570"
)]
public partial class PlayerAliveCondition : Condition
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Self;

    [SerializeReference]
    public BlackboardVariable<bool> PlayerAlive;

    public override bool IsTrue()
    {
        GameObject playerObject = null;

        // =====================================================
        // TRY BLACKBOARD SELF FIRST
        // =====================================================

        if (Self != null)
        {
            playerObject = Self.Value;
        }

        // =====================================================
        // FALLBACK TO PLAYER TAG
        // =====================================================

        if (playerObject == null)
        {
            playerObject =
                GameObject.FindGameObjectWithTag("Player");
        }

        // =====================================================
        // FALLBACK TO PLAYER STATE MANAGER
        // =====================================================

        PlayerStateManager stateManager = null;

        if (playerObject != null)
        {
            stateManager =
                playerObject.GetComponent<PlayerStateManager>();
        }

        if (stateManager == null)
        {
            stateManager =
                PlayerStateManager.Instance;
        }

        // =====================================================
        // PLAYER NOT READY YET
        // =====================================================

        // This can legitimately happen for one or two frames
        // while a new scene is loading.

        if (stateManager == null)
        {
            if (PlayerAlive != null)
            {
                PlayerAlive.Value = false;
            }

            return false;
        }

        // =====================================================
        // CHECK RIP STATE
        // =====================================================

        bool alive =
            stateManager.GetCurrentState()
            != PlayerStateManager.PlayerState.RIP;

        if (PlayerAlive != null)
        {
            PlayerAlive.Value = alive;
        }

        return alive;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}