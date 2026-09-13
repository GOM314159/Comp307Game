using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerAlive", story: "[Self] is alive = [AliveFlag]", category: "Conditions", id: "ced817f865c5cb018d2ccf1c46696570")]
public partial class PlayerAliveCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<bool> AliveFlag;
    public override bool IsTrue()
    {
        // Check if the player is alive
        if (Self.Value.GetComponent<PlayerStateManager>().GetCurrentState() == PlayerStateManager.PlayerState.RIP)
        {
            AliveFlag.Value = false;
            return AliveFlag.Value;
            
        }

        return AliveFlag.Value;
        
    }

    public override void OnStart()
    {

    }

    public override void OnEnd()
    {
    }
}
