using System.Collections;
using UnityEngine;

/// <summary>
/// Starts the final boss entrance cue and activates Gargamel after it.
/// This component is scene-local, so its Gargamel reference may safely be
/// assigned in the Inspector.
/// </summary>
public class FinalBossController : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private GameObject finalBoss;

    [Header("Spawn Cue")]
    [SerializeField] private AudioClip bossSpawnCue;
    [SerializeField] private bool useCueLengthAsDelay = true;
    [Min(0f)] [SerializeField] private float spawnDelay = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugSequence = true;

    private bool sequenceStarted;

    private void Awake()
    {
        ResolveFinalBossReference();

        if (finalBoss != null)
        {
            finalBoss.SetActive(false);
        }
    }

    public void SetFinalBoss(GameObject boss)
    {
        finalBoss = boss;
        ResolveFinalBossReference();

        if (finalBoss != null && !sequenceStarted)
        {
            finalBoss.SetActive(false);
        }
    }

    public void BeginBossSpawnSequence()
    {
        if (sequenceStarted)
        {
            return;
        }

        sequenceStarted = true;

        ResolveFinalBossReference();

        if (finalBoss == null)
        {
            Debug.LogError(
                "[FinalBossController] Gargamel/FinalBossHealth could not be found."
            );
            return;
        }

        if (bossSpawnCue != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(bossSpawnCue);
        }

        float delay = spawnDelay;

        if (useCueLengthAsDelay && bossSpawnCue != null)
        {
            delay = bossSpawnCue.length + spawnDelay;
        }

        StartCoroutine(SpawnBossAfterDelay(delay));
    }

    private void ResolveFinalBossReference()
    {
        if (finalBoss != null)
        {
            return;
        }

        FinalBossHealth health =
            FindAnyObjectByType<FinalBossHealth>(
                FindObjectsInactive.Include
            );

        if (health != null)
        {
            finalBoss = health.gameObject;
        }
    }

    private IEnumerator SpawnBossAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (finalBoss == null)
        {
            yield break;
        }

        finalBoss.SetActive(true);

        DebugSequence("Gargamel spawned.");
    }

    public void ResetBossSpawnSequence()
    {
        StopAllCoroutines();
        sequenceStarted = false;

        if (finalBoss != null)
        {
            finalBoss.SetActive(false);
        }
    }

    private void DebugSequence(string message)
    {
        if (!debugSequence)
        {
            return;
        }

        Debug.Log("[FinalBossController] " + message, this);
    }
}
