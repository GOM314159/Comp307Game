using UnityEngine;
public class MainMenuButton:MonoBehaviour { public void OpenMainMenu(){if(GameFlowManager.Instance!=null)GameFlowManager.Instance.OpenMainMenu();else Debug.LogError("[MainMenuButton] GameFlowManager.Instance is missing.",this);} }
