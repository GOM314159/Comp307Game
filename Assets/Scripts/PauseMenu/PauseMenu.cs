using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            container.SetActive(true);
            Time.timeScale = 0;
        }
    }

    public void ContinueButton()
    {
        container.SetActive(false);
        Time.timeScale = 1.0f;
    }

    public void MainMenuButton()
    {

    }

    public void ExitButton()
    {

    }
}
