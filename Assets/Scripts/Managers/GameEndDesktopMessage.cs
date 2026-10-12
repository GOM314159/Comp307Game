using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class GameEndDesktopMessage : MonoBehaviour
{
    [Header("Optional Windows desktop shortcut icon")]
    [Tooltip("Absolute path to a .ico file. If assigned and present, a desktop shortcut to the generated text file is created with this icon. The .txt file itself cannot reliably have a per-file custom icon on Windows.")]
    [SerializeField] private string customIconPath = "";

    [Tooltip("Create a shortcut beside the text file when a valid custom .ico path is supplied.")]
    [SerializeField] private bool createCustomIconShortcut = true;

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

            if (createCustomIconShortcut && Application.platform == RuntimePlatform.WindowsPlayer &&
                !string.IsNullOrWhiteSpace(customIconPath) && File.Exists(customIconPath) &&
                string.Equals(Path.GetExtension(customIconPath), ".ico", StringComparison.OrdinalIgnoreCase))
            {
                CreateWindowsShortcut(filePath, desktopPath, customIconPath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("[GameEndDesktopMessage] Could not create the desktop message: " + exception.Message);
        }
    }

    private void CreateWindowsShortcut(string targetFile, string desktopPath, string iconPath)
    {
        object shell = null;
        object shortcut = null;
        try
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            shell = Activator.CreateInstance(shellType);
            object shortcutPath = Path.Combine(desktopPath, "The Puppet Master.lnk");
            shortcut = shellType.InvokeMember("CreateShortcut",
                System.Reflection.BindingFlags.InvokeMethod, null, shell,
                new object[] { shortcutPath });

            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetFile });
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { desktopPath });
            shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { iconPath + ",0" });
            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "The Puppet Master - ending message" });
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
            Debug.Log("[GameEndDesktopMessage] Created custom-icon desktop shortcut.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[GameEndDesktopMessage] The text file was created, but the custom-icon shortcut could not be created: " + exception.Message);
        }
        finally
        {
            if (shortcut != null && Marshal.IsComObject(shortcut)) Marshal.FinalReleaseComObject(shortcut);
            if (shell != null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell);
        }
    }
}
