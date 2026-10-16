using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "GargamelAlive", story: "[Gargamel] is alive = [true]", category: "Conditions", id: "ec6494834c6b03f12e742953760d7435")]
public partial class GargamelAliveCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Gargamel;
    [SerializeReference] public BlackboardVariable<bool> True;

    public override bool IsTrue()
    {
        if (Gargamel.Value.GetComponent<FinalBossHealth>().GetCurrentHealth() <= 0 || Gargamel.Value.GetComponent<FinalBossHealth>().IsDead())
        {
            return false;
        }
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
