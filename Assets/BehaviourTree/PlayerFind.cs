using Unity.Behavior;
using UnityEngine;

public class PlayerFind : MonoBehaviour
{
    private BehaviorGraphAgent graphAgent;

    void Start()
    {
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");

            // Inject it into the Visual Scripting Variables component on this enemy
            graphAgent = GetComponent<BehaviorGraphAgent>();

            if (player != null && graphAgent != null)
            {
                // Inject the player into the blackboard variable named "Player"
                graphAgent.SetVariableValue("Player", player);
            }
            else
            {
                Debug.LogWarning("Player or BehaviorGraphAgent not found!");
            }
        
    }
}
