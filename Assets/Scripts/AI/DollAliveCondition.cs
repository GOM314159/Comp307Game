using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "DollAlive", story: "[Doll] is alive = [true]", category: "Conditions", id: "270a2758a73b5e4c121517d37208b6c2")]
public partial class DollAliveCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Doll;
    [SerializeReference] public BlackboardVariable<bool> True;

    public override bool IsTrue()
    {
        if (Doll.Value.GetComponent<Health>().GetCurrentHealth() <= 0 || Doll.Value.GetComponent<Health>().IsDead())
        {
            True.Value = false;
        }

        return True.Value;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
