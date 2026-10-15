using System.Collections;
using Unity.Behavior;
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

    [Header("Minis")]
    [SerializeField] private BehaviorGraph graphAgent;

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
            FinalBossHealth bossHealth =
                finalBoss.GetComponentInChildren<FinalBossHealth>(true);
            if (bossHealth != null)
                bossHealth.HidePhaseHealthBars();

            finalBoss.SetActive(false);
            DebugSequence("Boss is inactive at scene start; phase bars hidden.");
        }
    }

    public void SetFinalBoss(GameObject boss)
    {
        finalBoss = boss;
        ResolveFinalBossReference();

        if (finalBoss != null && !sequenceStarted)
        {
            FinalBossHealth bossHealth =
                finalBoss.GetComponentInChildren<FinalBossHealth>(true);
            if (bossHealth != null)
                bossHealth.HidePhaseHealthBars();

            finalBoss.SetActive(false);
        }
    }

    public void enableMinis()
    {
        if (graphAgent != null)
        {
            graphAgent.BlackboardReference.SetVariableValue("GargAlive", true);
            DebugSequence("Minis enabled.");
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
                "[FinalBossController] Gargamel/FinalBossHealth could not be found. " +
                "The spawn sequence can be retried after the boss reference is fixed.",
                this
            );
            sequenceStarted = false;
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
            return;
        }

        // Compatibility fallback for boss prefabs where the health component
        // sits on a child but the visible root has the expected name.
        GameObject namedBoss = GameObject.Find("Gargamel");
        if (namedBoss != null) finalBoss = namedBoss;
    }

    private IEnumerator SpawnBossAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (finalBoss == null)
        {
            sequenceStarted = false;
            Debug.LogError("[FinalBossController] Boss reference was lost before spawn.", this);
            yield break;
        }

        finalBoss.SetActive(true);

        FinalBossHealth bossHealth = finalBoss.GetComponentInChildren<FinalBossHealth>(true);
        if (bossHealth != null)
        {
            bossHealth.ResetBossHealth();
            DebugSequence("Boss spawned; phase 1 health bar activated.");
        }

        DebugSequence("Gargamel spawned.");
    }

    public void ResetBossSpawnSequence()
    {
        StopAllCoroutines();
        sequenceStarted = false;

        if (finalBoss != null)
        {
            FinalBossHealth bossHealth =
                finalBoss.GetComponentInChildren<FinalBossHealth>(true);
            if (bossHealth != null)
                bossHealth.HidePhaseHealthBars();

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
