using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerAlive", story: "[Self] = [PlayerAlive]", category: "Conditions", id: "ced817f865c5cb018d2ccf1c46696570")]
public partial class PlayerAliveCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<bool> PlayerAlive;
    public override bool IsTrue()
    {
        // Check if the player is alive
        if (Self.Value.GetComponent<PlayerStateManager>().GetCurrentState() == PlayerStateManager.PlayerState.RIP)
        {
            PlayerAlive.Value = false;
            return PlayerAlive.Value;
            
        }

        return PlayerAlive.Value;
        
    }

    public override void OnStart()
    {

    }

    public override void OnEnd()
    {
    }
}
