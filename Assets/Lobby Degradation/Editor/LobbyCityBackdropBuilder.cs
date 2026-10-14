#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Places the city model in the distance outside the lobby window and
/// darkens it so it sits in the same night as the theatre.
///
///   * Finds the window (the lobby's glass) and which way is outside.
///   * Puts Assets/Models/LobbyCity/LobbyCity.fbx there, scaled down so
///     it reads as a far-off skyline.
///   * Swaps every material for a dark, cool-toned copy (textures kept)
///     and leaves only a faint warm glow in the lit windows.
///   * Turns off shadows, light probes and reflection probes on it, so
///     it costs the lobby nothing in shadow budget.
///
/// It lives at Lobby/LobbyCityBackdrop and is NOT tied to a day - it is
/// simply the view. Everything about it can be adjusted afterwards by
/// moving/scaling that one object.
///
/// Tools > Strings and Statutes > Lobby > Build City Backdrop
/// </summary>
public static class LobbyCityBackdropBuilder
{
    private const string LobbyName = "Lobby";
    private const string CityName = "LobbyCityBackdrop";
    private const string LobbyScenePath =
        "Assets/Scenes/GamePlayTest.unity";

    private const string ModelPath =
        "Assets/Models/LobbyCity/LobbyCity.fbx";
    private const string TextureFolder =
        "Assets/Models/LobbyCity/Textures";

    private const string MaterialRoot = "Assets/Materials/DayDecay";
    private const string MaterialFolder =
        "Assets/Materials/DayDecay/City";

    // The model is a full-size city (towers ~90m). Scaled to this it
    // reads as a skyline across a wide street.
    private const float CityScale = 0.30f;

    // Metres between the window and the nearest part of the buildings.
    private const float GapBeyondWindow = 26f;

    // How far below the lobby floor the street sits.
    private const float StreetDrop = 0.8f;

    // Cool, near-black tint multiplied over every surface.
    private static readonly Color Tint =
        new Color(0.14f, 0.15f, 0.19f, 1f);

    // The faint warm glow left in lit windows.
    private static readonly Color Glow =
        new Color(1.0f, 0.72f, 0.40f, 1f) * 0.45f;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem("Tools/Strings and Statutes/Lobby/Build City Backdrop")]
    public static void Build()
    {
        BuildCore(true);
    }


    [MenuItem("Tools/Strings and Statutes/Lobby/Clear City Backdrop")]
    public static void Clear()
    {
        GameObject lobby = LobbyDay2DecayBuilder.FindLobby();

        if (lobby == null)
        {
            return;
        }

        RemoveExisting(lobby);

        EditorSceneManager.MarkSceneDirty(lobby.scene);

        Debug.Log("[City] Removed the city backdrop.");
    }


    // Command-line entry point - builds just the city and saves.
    public static void BuildAndSaveBatch()
    {
        EditorSceneManager.OpenScene(LobbyScenePath);

        if (!BuildCore(false))
        {
            EditorApplication.Exit(1);

            return;
        }

        EditorSceneManager.SaveOpenScenes();

        AssetDatabase.SaveAssets();

        Debug.Log("[City] Saved " + LobbyScenePath + ".");
    }


    // =========================================================
    // BUILD
    // =========================================================

    internal static bool BuildCore(bool interactive)
    {
        GameObject lobby = LobbyDay2DecayBuilder.FindLobby();

        if (lobby == null)
        {
            if (interactive)
            {
                bool open = EditorUtility.DisplayDialog(
                    "Lobby not found",
                    "There is no '" + LobbyName + "' object in the "
                    + "scene that is currently open.\n\nThe lobby "
                    + "lives in " + LobbyScenePath + ". Open it and "
                    + "run this again.",
                    "Open that scene", "Cancel");

                if (open &&
                    EditorSceneManager
                        .SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(LobbyScenePath);

                    Debug.Log("[City] Opened " + LobbyScenePath
                        + ". Run the builder again.");
                }
            }
            else
            {
                Debug.LogError("[City] No '" + LobbyName
                    + "' object found.");
            }

            return false;
        }

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);

        if (prefab == null)
        {
            Debug.LogError("[City] Model not found at " + ModelPath
                + ". Copy the converted city FBX there first.");

            return false;
        }

        ConfigureImporter();

        Undo.SetCurrentGroupName("Build City Backdrop");

        int group = Undo.GetCurrentGroup();

        RemoveExisting(lobby);

        Physics.SyncTransforms();

        Bounds room = LobbyDay2DecayBuilder.MeasureLobby(lobby);

        Vector3 focus = FindFocus(lobby, room);

        Vector3 windowCentre;

        Vector3 outward;

        FindWindow(lobby, room, focus, out windowCentre, out outward);

        // ---- instance -------------------------------------------

        GameObject city =
            (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        city.name = CityName;

        Undo.RegisterCreatedObjectUndo(city, "Build City Backdrop");

        city.transform.SetParent(lobby.transform, false);

        city.transform.position = Vector3.zero;

        city.transform.rotation = Quaternion.identity;

        float parentScale = Mathf.Max(
            0.0001f, lobby.transform.lossyScale.x);

        city.transform.localScale =
            Vector3.one * (CityScale / parentScale);

        // ---- measure, then move into place ----------------------

        Renderer[] renderers =
            city.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogError("[City] The model has no renderers.");

            return false;
        }

        Bounds cluster = new Bounds();

        bool anyCluster = false;

        Bounds ground = new Bounds();

        bool anyGround = false;

        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;

            // The street is a huge, flat sheet; the buildings are not.
            bool isGround = b.size.y < 0.5f &&
                Mathf.Max(b.size.x, b.size.z) > 20f;

            if (isGround)
            {
                if (!anyGround)
                {
                    ground = b;

                    anyGround = true;
                }
                else
                {
                    ground.Encapsulate(b);
                }
            }
            else
            {
                if (!anyCluster)
                {
                    cluster = b;

                    anyCluster = true;
                }
                else
                {
                    cluster.Encapsulate(b);
                }
            }
        }

        if (!anyCluster)
        {
            cluster = ground;
        }

        float clusterRadius =
            Mathf.Max(cluster.size.x, cluster.size.z) * 0.5f;

        Vector3 targetCentre = windowCentre
            + outward * (clusterRadius + GapBeyondWindow);

        float streetY = anyGround ? ground.max.y : cluster.min.y;

        float targetStreetY = focus.y - StreetDrop;

        Vector3 move = new Vector3(
            targetCentre.x - cluster.center.x,
            targetStreetY - streetY,
            targetCentre.z - cluster.center.z);

        city.transform.position += move;

        // ---- dark materials, no shadow cost ---------------------

        EnsureFolder();

        Dictionary<Material, Material> cache =
            new Dictionary<Material, Material>();

        foreach (Renderer r in renderers)
        {
            Material[] src = r.sharedMaterials;

            Material[] dst = new Material[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] == null)
                {
                    continue;
                }

                Material dark;

                if (!cache.TryGetValue(src[i], out dark))
                {
                    dark = MakeDark(src[i]);

                    cache[src[i]] = dark;
                }

                dst[i] = dark;
            }

            r.sharedMaterials = dst;

            r.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            r.receiveShadows = false;

            r.lightProbeUsage =
                UnityEngine.Rendering.LightProbeUsage.Off;

            r.reflectionProbeUsage =
                UnityEngine.Rendering.ReflectionProbeUsage.Off;

            GameObjectUtility.SetStaticEditorFlags(
                r.gameObject, StaticEditorFlags.BatchingStatic);
        }

        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(lobby.scene);

        Undo.CollapseUndoOperations(group);

        WarnIfClipped(city, focus);

        Debug.Log("[City] Placed " + renderers.Length + " renderer(s) "
            + "at scale " + CityScale + ", window " + windowCentre
            + ", outside is " + outward + ". Cluster centre "
            + cluster.center + " moved by " + move + ". Move or scale "
            + CityName + " to taste.");

        Selection.activeGameObject = city;

        return true;
    }


    private static void RemoveExisting(GameObject lobby)
    {
        for (int i = lobby.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = lobby.transform.GetChild(i);

            if (child.name == CityName)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }


    private static void ConfigureImporter()
    {
        ModelImporter mi =
            AssetImporter.GetAtPath(ModelPath) as ModelImporter;

        if (mi == null)
        {
            return;
        }

        bool dirty = false;

        if (mi.importCameras)
        {
            mi.importCameras = false;

            dirty = true;
        }

        if (mi.importLights)
        {
            mi.importLights = false;

            dirty = true;
        }

        if (mi.importBlendShapes)
        {
            mi.importBlendShapes = false;

            dirty = true;
        }

        if (mi.addCollider)
        {
            mi.addCollider = false;

            dirty = true;
        }

        if (mi.isReadable)
        {
            mi.isReadable = false;

            dirty = true;
        }

        if (dirty)
        {
            mi.SaveAndReimport();
        }
    }


    // =========================================================
    // WHERE IS THE WINDOW
    // =========================================================

    private static Vector3 FindFocus(GameObject lobby, Bounds room)
    {
        Transform spawn = LobbyDay2DecayBuilder.FindDeep(
            lobby.transform, "PlayerSpawnPoint");

        if (spawn != null)
        {
            return spawn.position;
        }

        return new Vector3(
            room.center.x, room.min.y, room.center.z);
    }


    // The lobby's glass is a thin pane. The thin horizontal axis is
    // the window's normal; "outside" is the side away from the spawn.
    private static void FindWindow(
        GameObject lobby,
        Bounds room,
        Vector3 focus,
        out Vector3 centre,
        out Vector3 outward)
    {
        Renderer best = null;

        float bestScore = -1f;

        Renderer[] all = lobby.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in all)
        {
            if (LobbyDay2DecayBuilder.IsInsideADayRoot(r.transform))
            {
                continue;
            }

            if (!HasMaterialNamed(r, "glass", "window"))
            {
                continue;
            }

            Vector3 s = r.bounds.size;

            // A pane is thin in x or z.
            if (Mathf.Min(s.x, s.z) > 0.6f)
            {
                continue;
            }

            float area = s.y * Mathf.Max(s.x, s.z);

            float dist = Vector3.Distance(r.bounds.center, focus);

            float score = area / (1f + dist * 0.1f);

            if (score > bestScore)
            {
                bestScore = score;

                best = r;
            }
        }

        if (best != null)
        {
            Bounds b = best.bounds;

            centre = b.center;

            bool alongX = b.size.x < b.size.z;

            float sign;

            if (alongX)
            {
                sign = (b.center.x >= focus.x) ? 1f : -1f;

                outward = new Vector3(sign, 0f, 0f);
            }
            else
            {
                sign = (b.center.z >= focus.z) ? 1f : -1f;

                outward = new Vector3(0f, 0f, sign);
            }

            Debug.Log("[City] Window: '" + best.name + "' at "
                + b.center + ", size " + b.size + ".");

            return;
        }

        // Second choice: whatever the model calls the outside.
        foreach (Renderer r in all)
        {
            if (LobbyDay2DecayBuilder.IsInsideADayRoot(r.transform))
            {
                continue;
            }

            if (HasMaterialNamed(r, "outside"))
            {
                centre = r.bounds.center;

                Vector3 flat = centre - focus;

                flat.y = 0f;

                outward = flat.sqrMagnitude > 0.01f
                    ? flat.normalized : Vector3.forward;

                Debug.LogWarning("[City] No glass found; using '"
                    + r.name + "' as the outside.");

                return;
            }
        }

        // Last resort.
        centre = new Vector3(room.center.x, room.min.y, room.max.z);

        outward = Vector3.forward;

        Debug.LogWarning("[City] Could not find the window or the "
            + "outside. Placed the city on the +Z side of the room - "
            + "move " + CityName + " to the right side.");
    }


    private static bool HasMaterialNamed(
        Renderer r, params string[] needles)
    {
        foreach (Material m in r.sharedMaterials)
        {
            if (m == null)
            {
                continue;
            }

            string n = m.name.ToLowerInvariant();

            foreach (string needle in needles)
            {
                if (n.Contains(needle))
                {
                    return true;
                }
            }
        }

        return false;
    }


    private static void WarnIfClipped(GameObject city, Vector3 focus)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        Bounds b = new Bounds(city.transform.position, Vector3.zero);

        foreach (Renderer r in
                 city.GetComponentsInChildren<Renderer>(true))
        {
            b.Encapsulate(r.bounds);
        }

        float farthest = Vector3.Distance(focus, b.center)
            + Mathf.Max(b.extents.x, b.extents.z);

        if (cam.farClipPlane < farthest)
        {
            Debug.LogWarning("[City] The main camera's far clip plane ("
                + cam.farClipPlane + ") is shorter than the farthest "
                + "part of the city (" + Mathf.RoundToInt(farthest)
                + "m), so the far edge may be cut off. Raise the far "
                + "clip or scale " + CityName + " down.");
        }
    }


    // =========================================================
    // MATERIALS
    // =========================================================

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(MaterialRoot))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "DayDecay");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder(MaterialRoot, "City");
        }
    }


    private static string Sanitise(string s)
    {
        char[] bad = System.IO.Path.GetInvalidFileNameChars();

        foreach (char c in bad)
        {
            s = s.Replace(c, '_');
        }

        return s.Replace(' ', '_');
    }


    // A near-black, cool-toned copy of a city material. Textures are
    // kept so the surfaces still read as concrete, glass and metal.
    private static Material MakeDark(Material src)
    {
        string baseName = Sanitise(src.name);

        string path = MaterialFolder + "/Dark_" + baseName + ".mat";

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (m == null)
        {
            m = new Material(LobbyDay2DecayBuilder.LitShader());

            AssetDatabase.CreateAsset(m, path);
        }

        string lower = src.name.ToLowerInvariant();

        bool isLight = lower.StartsWith("light");

        bool isGlass = lower.Contains("window") ||
                       lower.Contains("glass");

        bool isMetal = lower.Contains("metal") || lower.Contains("iron");

        // Textures from the source, or - because the FBX does not
        // always carry them - the concrete set by name.
        Texture baseTex = null;

        Texture normalTex = null;

        if (src.HasProperty("_BaseMap"))
        {
            baseTex = src.GetTexture("_BaseMap");
        }

        if (baseTex == null && src.HasProperty("_MainTex"))
        {
            baseTex = src.GetTexture("_MainTex");
        }

        if (src.HasProperty("_BumpMap"))
        {
            normalTex = src.GetTexture("_BumpMap");
        }

        if (baseTex == null && lower.StartsWith("concrete"))
        {
            baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TextureFolder
                + "/189_concrete_bare_PBR_texture-seamless.png");

            normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TextureFolder
                + "/189_concrete_bare_PBR_texture-seamless-Normal-map.png");
        }

        Color colour = LobbyDay2DecayBuilder.GetBaseColour(src);

        if (isGlass)
        {
            LobbyDay2DecayBuilder.SetBaseColour(
                m, new Color(0.02f, 0.03f, 0.045f, 1f));

            baseTex = null;
        }
        else if (isLight)
        {
            LobbyDay2DecayBuilder.SetBaseColour(
                m, new Color(0.03f, 0.03f, 0.035f, 1f));

            baseTex = null;
        }
        else
        {
            LobbyDay2DecayBuilder.SetBaseColour(
                m, new Color(
                    colour.r * Tint.r,
                    colour.g * Tint.g,
                    colour.b * Tint.b,
                    1f));
        }

        if (m.HasProperty("_BaseMap"))
        {
            m.SetTexture("_BaseMap", baseTex);
        }

        if (normalTex != null && m.HasProperty("_BumpMap"))
        {
            m.SetTexture("_BumpMap", normalTex);

            m.EnableKeyword("_NORMALMAP");
        }
        else
        {
            m.DisableKeyword("_NORMALMAP");
        }

        float smoothness = isGlass ? 0.92f : (isMetal ? 0.30f : 0.12f);

        if (m.HasProperty("_Smoothness"))
        {
            m.SetFloat("_Smoothness", smoothness);
        }

        if (m.HasProperty("_Metallic"))
        {
            m.SetFloat("_Metallic",
                isGlass ? 0.3f : (isMetal ? 0.5f : 0f));
        }

        if (isLight)
        {
            m.EnableKeyword("_EMISSION");

            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", Glow);
            }
        }
        else
        {
            m.DisableKeyword("_EMISSION");

            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", Color.black);
            }
        }

        EditorUtility.SetDirty(m);

        return m;
    }
}
#endif
