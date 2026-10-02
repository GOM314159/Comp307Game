using System;
using Unity.Behavior;
using Unity.VisualScripting;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "ChandelierCut", story: "[Chandelier] cut = [flag]", category: "Conditions", id: "598dfb3e06934dce1d5081637e1dd035")]
public partial class ChandelierCutCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Chandelier;
    [SerializeReference] public BlackboardVariable<bool> Flag;

    public override bool IsTrue()
    {

        if (!Chandelier.Value.activeSelf == Flag.Value)
        {
            return true;
        }

        return false;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
