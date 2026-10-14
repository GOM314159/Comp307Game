using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Makes the lobby's lights fail as the days pass.
///
/// One of these sits inside each DayNChanges root. The roots are switched
/// on and off by GameFlowManager / LobbyDayVisuals, so this turns on with
/// its day and off again before it - no day logic of its own.
///
/// Several can be active at once (Day 4 shows Day 3 too) and they STACK:
/// a light's brightness is its original brightness times every active
/// day's multiplier, so Day 3 at 0.85 and Day 4 at 0.7 gives 0.6. When the
/// last one switches off, every light goes back to exactly what it was.
///
/// Only intensity is touched - never enabled, colour or range - so
/// nothing else that controls these lights is fought with.
/// </summary>
[DisallowMultipleComponent]
public class LobbyLightDecay : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public Light light;

        [Tooltip("Brightness multiplier from this day onward. "
                 + "0 = the light has died.")]
        [Range(0f, 1f)]
        public float intensity = 1f;

        [Tooltip("Stutters on and off in bursts.")]
        public bool flicker = false;

        [Tooltip("How far the light drops during a stutter.")]
        [Range(0f, 1f)]
        public float flickerDepth = 0.7f;

        [Tooltip("Higher = more frequent, faster stutters.")]
        [Min(0.05f)]
        public float flickerRate = 1f;
    }


    [SerializeField]
    private List<Entry> lights = new List<Entry>();


    // Every light any active day has touched, with its untouched
    // brightness, and every active day.
    private static readonly Dictionary<Light, float> original =
        new Dictionary<Light, float>();

    private static readonly List<LobbyLightDecay> active =
        new List<LobbyLightDecay>();


    // Statics survive play-mode entry when domain reload is off.
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        original.Clear();
        active.Clear();
    }


    // =========================================================
    // EDITOR SET-UP
    // =========================================================

    /// <summary>Used by the lobby decay builders.</summary>
    public void Configure(List<Entry> entries)
    {
        lights = entries ?? new List<Entry>();
    }


    public int LightCount
    {
        get { return lights.Count; }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        if (!active.Contains(this))
        {
            active.Add(this);
        }

        foreach (Entry e in lights)
        {
            if (e == null || e.light == null)
            {
                continue;
            }

            if (!original.ContainsKey(e.light))
            {
                original.Add(e.light, e.light.intensity);
            }

            e.light.intensity = Evaluate(e.light, Time.time);
        }
    }


    private void OnDisable()
    {
        active.Remove(this);

        foreach (Entry e in lights)
        {
            if (e == null || e.light == null)
            {
                continue;
            }

            if (IsClaimed(e.light))
            {
                e.light.intensity = Evaluate(e.light, Time.time);

                continue;
            }

            // No active day wants this light any more - put it back.
            float before;

            if (original.TryGetValue(e.light, out before))
            {
                e.light.intensity = before;

                original.Remove(e.light);
            }
        }
    }


    private void LateUpdate()
    {
        float t = Time.time;

        // Only flickering lights change frame to frame. Every day that
        // shares a light computes the same value for it, so writing it
        // more than once is harmless.
        foreach (Entry e in lights)
        {
            if (e != null && e.light != null && e.flicker)
            {
                e.light.intensity = Evaluate(e.light, t);
            }
        }
    }


    // =========================================================
    // MATHS
    // =========================================================

    private static bool IsClaimed(Light light)
    {
        foreach (LobbyLightDecay day in active)
        {
            foreach (Entry e in day.lights)
            {
                if (e != null && e.light == light)
                {
                    return true;
                }
            }
        }

        return false;
    }


    private static float Evaluate(Light light, float time)
    {
        float before;

        if (!original.TryGetValue(light, out before))
        {
            return light.intensity;
        }

        float multiplier = 1f;

        float flicker = 1f;

        foreach (LobbyLightDecay day in active)
        {
            foreach (Entry e in day.lights)
            {
                if (e == null || e.light != light)
                {
                    continue;
                }

                multiplier *= e.intensity;

                if (e.flicker)
                {
                    flicker = Mathf.Min(
                        flicker, Flicker(light, e, time));
                }
            }
        }

        return before * multiplier * flicker;
    }


    // Mostly steady with a faint electrical hum, then every so often a
    // burst of hard dropouts - the stutter of a failing ballast, not a
    // candle.
    private static float Flicker(Light light, Entry e, float time)
    {
        float seed =
    (Mathf.Abs(light.GetEntityId().GetHashCode()) % 997) * 0.731f;

        float t = time * e.flickerRate;

        float hum = 0.93f + 0.07f * Mathf.PerlinNoise(seed, t * 6f);

        // Slow gate: decides WHEN a burst happens.
        float gate = Mathf.PerlinNoise(seed + 13.1f, t * 0.35f);

        if (gate < 0.6f)
        {
            return hum;
        }

        // Fast chatter inside the burst.
        float chatter = Mathf.PerlinNoise(seed + 41.7f, t * 22f);

        return chatter > 0.48f
            ? hum
            : hum * (1f - e.flickerDepth);
    }
}
