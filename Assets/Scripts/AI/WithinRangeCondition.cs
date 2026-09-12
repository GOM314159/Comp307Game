using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "WithinRange", story: "[Doll] within [Range] of [Self]", category: "Conditions", id: "d90e0d13073351c4a783121ac6ecc636")]
public partial class WithinRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Doll;
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> Range;


    public override bool IsTrue()
    {

        if (Vector3.Distance(Doll.Value.transform.position, Self.Value.transform.position) <= Range.Value)
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
