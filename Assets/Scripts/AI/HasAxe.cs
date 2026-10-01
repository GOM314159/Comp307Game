using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasAxe", story: "[AxeInteracted] set to [True]", category: "Conditions", id: "689f95bb2930e699ba0e0abd19b97598")]
public partial class HasAxeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> AxeInteracted;
    [SerializeReference] public BlackboardVariable<bool> True;

    public override bool IsTrue()
    {
        
        return AxeInteracted.Value == True.Value;
    }

    public override void OnStart()
    {
        Debug.Log($"AxeInteracted: {AxeInteracted.Value}, Flag: {True.Value}");
    }

    public override void OnEnd()
    {


    }
}
