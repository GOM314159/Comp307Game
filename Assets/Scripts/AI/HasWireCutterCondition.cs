using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasWireCutter", story: "[HasCableCutter] = [flag]", category: "Conditions", id: "03de4fac434338be7d69312fd7cc8bac")]
public partial class HasWireCutterCondition : Condition
{
    [SerializeReference] public BlackboardVariable<bool> HasCableCutter;
    [SerializeReference] public BlackboardVariable<bool> Flag;

    public override bool IsTrue()
    {
        return HasCableCutter.Value == Flag.Value;
    }

    public override void OnStart()
    {
        Debug.Log($"HasCableCutter: {HasCableCutter.Value}, Flag: {Flag.Value}");
    }

    public override void OnEnd()
    {
    }
}
