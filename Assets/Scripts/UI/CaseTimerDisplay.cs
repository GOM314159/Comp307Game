using UnityEngine; using TMPro;
public class CaseTimerDisplay : MonoBehaviour {
 [SerializeField] private GameObject timerRoot; [SerializeField] private TMP_Text timerText; [SerializeField] private bool hideWhenTimerStopped = true;
 private void Awake(){ if(timerText==null) timerText=GetComponentInChildren<TMP_Text>(true); Hide(); }
 private void Update(){ CaseManager c=CaseManager.Instance; if(c==null||!c.HasTimer()||(hideWhenTimerStopped&&!c.IsTimerRunning())){Hide();return;} if(timerRoot!=null&&timerRoot!=gameObject&&!timerRoot.activeSelf)timerRoot.SetActive(true); else if(timerText!=null&&!timerText.gameObject.activeSelf)timerText.gameObject.SetActive(true); float t=Mathf.Max(0f,c.GetTimeRemaining()); int sec=Mathf.CeilToInt(t); if(timerText!=null)timerText.text=(sec/60).ToString("00")+":"+(sec%60).ToString("00"); }
 private void Hide(){if(timerRoot!=null&&timerRoot!=gameObject){if(timerRoot.activeSelf)timerRoot.SetActive(false);}else if(timerText!=null&&timerText.gameObject.activeSelf)timerText.gameObject.SetActive(false);}
}
