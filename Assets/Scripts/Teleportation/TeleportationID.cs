using UnityEngine;

[CreateAssetMenu(
    fileName = "TeleportDestinationID",
    menuName = "Game/Teleport Destination ID"
)]
public class TeleportDestinationID : ScriptableObject
{
    [Header("Destination Information")]

    [Tooltip("Friendly name used to identify this destination.")]
    public string destinationName;
}