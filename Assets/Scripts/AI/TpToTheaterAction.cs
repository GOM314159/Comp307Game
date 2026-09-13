using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TP to theater", story: "[Self] respawns at Theater [True]", category: "Action", id: "59fb7ece0bfab0c8b0007786cde5c204")]
public partial class TpToTheaterAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<PlayerRespawnPoint> Respawn;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {

        Respawn.Value.SpawnPlayer(Self.Value);


        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

