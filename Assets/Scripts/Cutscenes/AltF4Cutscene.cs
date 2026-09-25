using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class AltF4ExitCutscene : MonoBehaviour
{
 

    [Header("Exit Cutscene Video")]

    [SerializeField]
    private VideoPlayer videoPlayer;


    [Header("Settings")]

    [SerializeField]
    private bool enableAltF4ExitCutscene = true;

    [SerializeField]
    private bool debugExitCutscene = true;

    private bool allowNormalQuit = false;
    private bool exitSequenceRunning = false;

    private bool applicationIsQuitting = false;

    private Canvas exitCanvas;

    private RawImage exitVideoImage;

    private RenderTexture exitRenderTexture;


    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }


        if (videoPlayer == null)
        {
            Debug.LogError(
                "[AltF4ExitCutscene] No VideoPlayer assigned."
            );

            return;
        }


        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;
        videoPlayer.loopPointReached -=
            OnVideoFinished;

        videoPlayer.errorReceived -=
            OnVideoError;

        videoPlayer.prepareCompleted -=
            OnVideoPrepared;

        videoPlayer.loopPointReached +=
            OnVideoFinished;

        videoPlayer.errorReceived +=
            OnVideoError;

        videoPlayer.prepareCompleted +=
            OnVideoPrepared;
    }


    private void OnEnable()
    {
        Application.wantsToQuit +=
            HandleApplicationWantsToQuit;
    }


    private void OnDisable()
    {
        Application.wantsToQuit -=
            HandleApplicationWantsToQuit;
    }

    public void AllowNormalQuit()
    {
        allowNormalQuit = true;
        Application.Quit();
    }

    private bool HandleApplicationWantsToQuit()
    {
        
        if (applicationIsQuitting)
        {
            return true;
        }

        if (allowNormalQuit)
        {
            return true;
        }



        if (!enableAltF4ExitCutscene)
        {
            return true;
        }


       
        if (exitSequenceRunning)
        {
            DebugExit(
                "Additional quit request ignored."
            );

            return false;
        }


       
        DebugExit(
            "Alt + F4 detected. Starting exit cutscene."
        );


        exitSequenceRunning = true;


        StartExitCutscene();


        return false;
    }


    private void StartExitCutscene()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Cutscene
            );
        }


        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;


        HideExistingUI();

        CreateFullscreenVideoUI();

        videoPlayer.Stop();

        videoPlayer.renderMode =
            VideoRenderMode.RenderTexture;

        videoPlayer.targetTexture =
            exitRenderTexture;

        videoPlayer.Prepare();
    }


    private void CreateFullscreenVideoUI()
    {
        GameObject canvasObject =
            new GameObject(
                "AltF4ExitCutsceneCanvas"
            );


        canvasObject.transform.SetParent(
            transform,
            false
        );


        exitCanvas =
            canvasObject.AddComponent<Canvas>();


        exitCanvas.renderMode =
            RenderMode.ScreenSpaceOverlay;


        exitCanvas.sortingOrder =
            32767;


        canvasObject.AddComponent<
            CanvasScaler>();


        canvasObject.AddComponent<
            GraphicRaycaster>();


        GameObject imageObject =
            new GameObject(
                "ExitVideo"
            );


        imageObject.transform.SetParent(
            canvasObject.transform,
            false
        );


        exitVideoImage =
            imageObject.AddComponent<RawImage>();


        RectTransform rect =
            exitVideoImage.rectTransform;


        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;


     
        exitRenderTexture =
            new RenderTexture(
                Screen.width,
                Screen.height,
                0
            );


        exitRenderTexture.name =
            "AltF4ExitVideoRenderTexture";


        exitRenderTexture.Create();


        exitVideoImage.texture =
            exitRenderTexture;
    }



    private void HideExistingUI()
    {

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(
                false
            );
        }



        Canvas[] canvases =
            FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        foreach (Canvas canvas in canvases)
        {
            if (canvas == null)
            {
                continue;
            }


            if (
                exitCanvas != null &&
                canvas == exitCanvas
            )
            {
                continue;
            }


            canvas.enabled = false;
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(
                false
            );
        }
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (!exitSequenceRunning)
        {
            return;
        }


        DebugExit(
            "Exit video prepared. Playing."
        );


        player.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        DebugExit(
            "Exit video finished. Quitting."
        );


        QuitApplication();
    }

    private void OnVideoError(
        VideoPlayer player,
        string message)
    {
        Debug.LogError(
            "[AltF4ExitCutscene] Video error: "
            + message
        );

        QuitApplication();
    }


    private void QuitApplication()
    {
        if (applicationIsQuitting)
        {
            return;
        }


        applicationIsQuitting = true;


        DebugExit(
            "Calling Application.Quit()."
        );


        Application.Quit();
    }

    private void DebugExit(string message)
    {
        if (!debugExitCutscene)
        {
            return;
        }


        Debug.Log(
            "[AltF4ExitCutscene] "
            + message
        );
    }

    private void OnDestroy()
    {
        Application.wantsToQuit -=
            HandleApplicationWantsToQuit;


        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -=
                OnVideoFinished;

            videoPlayer.errorReceived -=
                OnVideoError;

            videoPlayer.prepareCompleted -=
                OnVideoPrepared;
        }


        if (exitRenderTexture != null)
        {
            exitRenderTexture.Release();

            Destroy(exitRenderTexture);
        }
    }
}