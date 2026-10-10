using System;
using System.IO;
using UnityEngine;

public class GameEndDesktopMessage : MonoBehaviour
{
    private bool messageCreated;

    private void OnEnable()
    {
        SubscribeToGameEnd();
    }

    private void Start()
    {
        SubscribeToGameEnd();

        // Handles the case where this object starts after the ending has completed.
        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.IsFinalCutsceneComplete())
        {
            CreateDesktopMessage();
        }
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnFinalCutsceneCompleted -= CreateDesktopMessage;
        }
    }

    private void SubscribeToGameEnd()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnFinalCutsceneCompleted -= CreateDesktopMessage;
            GameStateManager.Instance.OnFinalCutsceneCompleted += CreateDesktopMessage;
        }
    }

    private void CreateDesktopMessage()
    {
        if (messageCreated)
        {
            return;
        }

        messageCreated = true;

        try
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string filePath = Path.Combine(desktopPath, "The Puppet Master.txt");

            File.WriteAllText(
                filePath,
                "Are you really sure the game is over?\r\n~ The Puppet Master"
            );

            Debug.Log("[GameEndDesktopMessage] Created desktop message: " + filePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("[GameEndDesktopMessage] Could not create the desktop message: " + exception.Message);
        }
    }
}
