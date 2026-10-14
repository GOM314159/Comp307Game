using System;
using Unity.Behavior;
using Unity.VisualScripting;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "ChandelierCut", story: "[Chandelier] active = [ChandelierIsCut]", category: "Conditions", id: "598dfb3e06934dce1d5081637e1dd035")]
public partial class ChandelierCutCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Chandelier;
    [SerializeReference] public BlackboardVariable<bool> ChandelierIsCut;

    public override bool IsTrue()
    {

        if (Chandelier.Value.activeSelf == ChandelierIsCut.Value)
        {
            return true;
        }

        return false;
    }

    public override void OnStart()
    {

        Debug.Log($"Chandelier active: {Chandelier.Value.activeSelf}, ChandelierIsCut Value: {ChandelierIsCut.Value}");

    }

    public override void OnEnd()
    {
    }
}
