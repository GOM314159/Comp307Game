using UnityEngine;

public class VashinInteractable : WorldInteractable
{
    //Vashin VN from Ep1c_Gorvenator
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private bool restartAudioOnInteract = true;

    private bool audioWasPlayingBeforePause;

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void Interact()
    {
        if (!CanInteract())
            return;

        base.Interact();

        if (audioSource == null)
        {
            Debug.LogWarning("[VashinInteractable] No AudioSource is assigned or attached.", this);
            return;
        }

        if (audioClip != null && audioSource.clip != audioClip)
            audioSource.clip = audioClip;

        if (audioSource.clip == null)
        {
            Debug.LogWarning("[VashinInteractable] No audio clip is assigned.", this);
            return;
        }

        if (restartAudioOnInteract)
            audioSource.Stop();

        audioSource.Play();
    }

    public void SetAudioPaused(bool paused)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            return;

        if (paused)
        {
            audioWasPlayingBeforePause = audioSource.isPlaying;
            if (audioWasPlayingBeforePause)
                audioSource.Pause();
        }
        else if (audioWasPlayingBeforePause)
        {
            audioSource.UnPause();
            audioWasPlayingBeforePause = false;
        }
    }
}
