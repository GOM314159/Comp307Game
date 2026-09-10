using UnityEngine;

public class IntroSceneController : MonoBehaviour
{
    private void Start()
    {
        Debug.Log(
            "[IntroSceneController] Intro scene loaded."
        );

        if (GameFlowManager.Instance == null)
        {
            Debug.LogError(
                "[IntroSceneController] " +
                "GameFlowManager.Instance is missing."
            );

            return;
        }

        // The GameFlowManager is responsible for telling
        // this scene whether a cutscene should actually play.
        GameFlowManager.Instance.IntroSceneLoaded();
    }
}