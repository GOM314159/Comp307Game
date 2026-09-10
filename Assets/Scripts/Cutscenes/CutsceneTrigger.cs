using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using System.Collections.Generic;

// =============================================================
// CUTSCENE TRIGGER
// =============================================================
//
// Fires a video cutscene from a trigger volume, or from any other
// script or UnityEvent by calling Fire().
//
// Put this on a GameObject with a Collider marked Is Trigger and
// walk into it, or leave the collider off and drive it manually.
//
// Player lockout is handled by CutsceneManager through
// PlayerStateManager, so nothing needs wiring up here for that.
//
// =============================================================

public class CutsceneTrigger : MonoBehaviour
{
    // =========================================================
    // SOURCE
    // =========================================================

    public enum SourceKind
    {
        VideoClipAsset,

        StreamingAssetsFile
    }


    public enum RepeatMode
    {
        EveryTime,

        OncePerSession,

        OncePerSave
    }


    // Where control goes once the cutscene ends.
    public enum CompletionRoute
    {
        None,

        IntroFinished,

        FinalCutsceneFinished
    }


    [Header("Video")]

    [SerializeField]
    private SourceKind sourceKind = SourceKind.VideoClipAsset;

    [SerializeField]
    private VideoClip clip;

    // Path inside StreamingAssets, e.g. "Cutscenes/intro.mp4".
    // Keeps large video files out of the asset database.
    [SerializeField]
    private string streamingAssetsPath = "";


    // =========================================================
    // TRIGGER
    // =========================================================

    [Header("Trigger")]

    [SerializeField]
    private bool fireOnTriggerEnter = true;

    [SerializeField]
    private string requiredTag = "Player";

    [SerializeField]
    private RepeatMode repeatMode = RepeatMode.OncePerSession;

    // Stable id used to remember that this cutscene was seen.
    // Defaults to scene name plus object name when left empty.
    [SerializeField]
    private string saveKey = "";


    // =========================================================
    // COMPLETION
    // =========================================================

    [Header("On Complete")]

    // Routes into the matching GameFlowManager method, so the
    // existing intro and ending flows are used rather than
    // duplicated here.
    [SerializeField]
    private CompletionRoute completionRoute = CompletionRoute.None;

    // Fires whether the cutscene was watched or skipped.
    [SerializeField]
    private UnityEvent onComplete;

    // Fires only when the player watched it all the way through.
    [SerializeField]
    private UnityEvent onWatchedFully;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugTrigger = true;


    // =========================================================
    // RUNTIME STATE
    // =========================================================

    private static readonly HashSet<string> SessionPlayed =
        new HashSet<string>();

    private bool firing;


    private string Key
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(saveKey))
            {
                return "cutscene." + saveKey;
            }

            return "cutscene."
                   + gameObject.scene.name
                   + "."
                   + name;
        }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void Reset()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!fireOnTriggerEnter)
        {
            return;
        }

        if (!string.IsNullOrEmpty(requiredTag)
            && !other.CompareTag(requiredTag))
        {
            return;
        }

        Fire();
    }


    // =========================================================
    // FIRE
    // =========================================================

    public void Fire()
    {
        if (firing)
        {
            return;
        }

        if (CutsceneManager.IsCutscenePlaying)
        {
            return;
        }

        if (CutsceneManager.Instance == null)
        {
            Debug.LogError(
                "[Cutscene Trigger] No CutsceneManager in the scene.",
                this);

            return;
        }

        if (AlreadyPlayed())
        {
            DebugTrigger("Already seen, skipping.");

            return;
        }

        firing = true;

        MarkPlayed();

        switch (sourceKind)
        {
            case SourceKind.VideoClipAsset:

                if (clip == null)
                {
                    Debug.LogError(
                        "[Cutscene Trigger] No VideoClip assigned on "
                        + name,
                        this);

                    Complete(false);

                    return;
                }

                DebugTrigger("Playing clip " + clip.name);

                CutsceneManager.Instance.Play(clip, Complete);

                break;

            case SourceKind.StreamingAssetsFile:

                if (string.IsNullOrWhiteSpace(streamingAssetsPath))
                {
                    Debug.LogError(
                        "[Cutscene Trigger] No StreamingAssets path on "
                        + name,
                        this);

                    Complete(false);

                    return;
                }

                // Built with a plain separator. On Android
                // streamingAssetsPath is already a jar: URL and
                // Path.Combine would mangle it.
                string url =
                    Application.streamingAssetsPath.TrimEnd('/')
                    + "/"
                    + streamingAssetsPath.TrimStart('/');

                DebugTrigger("Playing url " + url);

                CutsceneManager.Instance.Play(url, Complete);

                break;
        }
    }


    // =========================================================
    // COMPLETE
    // =========================================================

    private void Complete(bool skipped)
    {
        firing = false;

        if (onComplete != null)
        {
            onComplete.Invoke();
        }

        if (!skipped && onWatchedFully != null)
        {
            onWatchedFully.Invoke();
        }

        if (GameFlowManager.Instance == null)
        {
            return;
        }

        switch (completionRoute)
        {
            case CompletionRoute.IntroFinished:

                GameFlowManager.Instance.IntroFinished();

                break;

            case CompletionRoute.FinalCutsceneFinished:

                GameFlowManager.Instance.FinalCutsceneFinished();

                break;
        }
    }


    // =========================================================
    // REPEAT TRACKING
    // =========================================================

    private bool AlreadyPlayed()
    {
        switch (repeatMode)
        {
            case RepeatMode.OncePerSession:

                return SessionPlayed.Contains(Key);

            case RepeatMode.OncePerSave:

                return PlayerPrefs.GetInt(Key, 0) == 1;

            default:

                return false;
        }
    }


    private void MarkPlayed()
    {
        switch (repeatMode)
        {
            case RepeatMode.OncePerSession:

                SessionPlayed.Add(Key);

                break;

            case RepeatMode.OncePerSave:

                // PlayerPrefs is a placeholder. The project has no
                // save system yet, so swap this out when one lands
                // and "seen cutscenes" should travel with the save.
                PlayerPrefs.SetInt(Key, 1);

                PlayerPrefs.Save();

                break;
        }
    }


    // Clears the seen flag. Useful from a New Game or debug menu.
    public void ResetPlayedFlag()
    {
        SessionPlayed.Remove(Key);

        PlayerPrefs.DeleteKey(Key);
    }


    // Called by GameStateManager.ResetEntireGame() style flows.
    public static void ClearAllSessionFlags()
    {
        SessionPlayed.Clear();
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugTrigger(string message)
    {
        if (!debugTrigger)
        {
            return;
        }

        Debug.Log("[Cutscene Trigger] " + message);
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();

        if (col == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);

        Gizmos.matrix = transform.localToWorldMatrix;

        BoxCollider box = col as BoxCollider;

        if (box != null)
        {
            Gizmos.DrawCube(box.center, box.size);
        }
    }
#endif
}
