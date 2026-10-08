#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Builds the Day 2 dress pass for the lobby.
///
/// Everything it makes goes under Lobby/Day2Changes and is named with a
/// "D2_" prefix, so re-running the tool clears its own work and leaves
/// anything you hand-placed alone.
///
/// Placement is done with raycasts against the theatre's existing
/// colliders, so props sit on the real floor, walls and ceiling rather
/// than at guessed coordinates. Re-run it after moving anything.
///
/// Tools > Strings and Statutes > Lobby > Build Day 2 Decay
/// </summary>
public static class LobbyDay2DecayBuilder
{
    private const string LobbyName = "Lobby";
    private const string DayRootName = "Day2Changes";
    private const string Prefix = "D2_";
    private const string LobbyScenePath =
        "Assets/Scenes/GamePlayTest.unity";
    private const string MaterialFolder = "Assets/Materials/DayDecay";

    private const int Seed = 20802;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem("Tools/Strings and Statutes/Lobby/Build Day 2 Decay")]
    public static void Build()
    {
        GameObject lobby = FindLobby();

        if (lobby == null)
        {
            bool open = EditorUtility.DisplayDialog(
                "Lobby not found",
                "There is no '" + LobbyName + "' object in the "
                + "scene that is currently open.\n\nOpen now: "
                + DescribeOpenScenes()
                + "\n\nThe lobby lives in " + LobbyScenePath
                + ". Open it and run this again.",
                "Open that scene", "Cancel");

            if (open)
            {
                if (EditorSceneManager
                        .SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(LobbyScenePath);

                    Debug.Log("[Day2Decay] Opened " + LobbyScenePath
                        + ". Run the builder again.");
                }
            }

            return;
        }

        // The day roots moved under Lobby/Changes when main was merged,
        // so a direct-child Find no longer sees them. Search deep.
        Transform dayRoot =
            FindDeep(lobby.transform, DayRootName);

        if (dayRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Day2Changes not found",
                "The Lobby has no child called '" + DayRootName + "'.",
                "OK");

            return;
        }

        Undo.SetCurrentGroupName("Build Day 2 Decay");

        int group = Undo.GetCurrentGroup();

        ClearGenerated(dayRoot);

        Physics.SyncTransforms();

        Bounds room = MeasureLobby(lobby);

        if (room.size.sqrMagnitude < 1f)
        {
            EditorUtility.DisplayDialog(
                "Could not measure the lobby",
                "Found no renderers under the Lobby to measure.",
                "OK");

            return;
        }

        Random.InitState(Seed);

        DecayMaterials mats = BuildMaterials();

        int made = 0;

        made += BuildFloorDebris(dayRoot, room, mats);
        made += BuildCeilingDamp(dayRoot, room, mats);
        made += BuildWallPeel(dayRoot, room, mats);
        made += BuildCobwebs(dayRoot, room, mats);

        List<GameObject> switchOff =
            BuildGrimeReplacements(lobby, dayRoot, mats);

        WireComponent(lobby, dayRoot.gameObject, switchOff);

        EditorSceneManager.MarkSceneDirty(
            lobby.scene);

        Undo.CollapseUndoOperations(group);

        Debug.Log(
            "[Day2Decay] Built " + made + " additive object(s) and "
            + switchOff.Count + " replacement(s) under "
            + LobbyName + "/" + DayRootName
            + ". Lobby bounds: centre " + room.center
            + ", size " + room.size + ".");

        Selection.activeGameObject = dayRoot.gameObject;
    }


    // Command-line entry point. Opens the lobby scene, builds the
    // Day 2 decay, and SAVES the scene and materials.
    //   Unity.exe -batchmode -quit -projectPath <repo>
    //       -executeMethod LobbyDay2DecayBuilder.BuildAndSaveBatch
    // The Unity editor must be closed on this project first.
    public static void BuildAndSaveBatch()
    {
        EditorSceneManager.OpenScene(LobbyScenePath);

        Build();

        GameObject lobby = FindLobby();

        Transform dayRoot = lobby != null
            ? FindDeep(lobby.transform, DayRootName) : null;

        if (dayRoot == null || dayRoot.childCount == 0)
        {
            Debug.LogError("[Day2Decay] Batch build produced nothing.");

            EditorApplication.Exit(1);

            return;
        }

        EditorSceneManager.SaveOpenScenes();

        AssetDatabase.SaveAssets();

        Debug.Log("[Day2Decay] Saved " + LobbyScenePath + " with "
            + dayRoot.childCount + " day-2 group(s).");
    }


    [MenuItem("Tools/Strings and Statutes/Lobby/Clear Day 2 Decay")]
    public static void Clear()
    {
        GameObject lobby = FindLobby();

        if (lobby == null)
        {
            return;
        }

        Transform dayRoot = FindDeep(lobby.transform, DayRootName);

        if (dayRoot == null)
        {
            return;
        }

        ClearGenerated(dayRoot);

        EditorSceneManager.MarkSceneDirty(lobby.scene);

        Debug.Log("[Day2Decay] Cleared generated objects.");
    }


    // =========================================================
    // FINDING THE LOBBY
    // =========================================================

    // GameObject.Find only returns ACTIVE objects and only searches
    // the active scene, which is why "lobby is not present" shows up
    // the moment anything is loaded additively or the root has been
    // toggled off. Walk every loaded scene instead, inactive
    // objects included.
    internal static GameObject FindLobby()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (!scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                if (root.name == LobbyName)
                {
                    return root;
                }

                Transform nested =
                    FindDeep(root.transform, LobbyName);

                if (nested != null)
                {
                    return nested.gameObject;
                }
            }
        }

        return null;
    }


    internal static Transform FindDeep(Transform parent, string target)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == target)
            {
                return child;
            }

            Transform found = FindDeep(child, target);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }


    private static string DescribeOpenScenes()
    {
        string list = "";

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (i > 0)
            {
                list += ", ";
            }

            list += string.IsNullOrEmpty(scene.name)
                ? "(untitled)"
                : scene.name;
        }

        return string.IsNullOrEmpty(list) ? "(nothing)" : list;
    }


    // =========================================================
    // HOUSEKEEPING
    // =========================================================

    private static void ClearGenerated(Transform dayRoot)
    {
        for (int i = dayRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = dayRoot.GetChild(i);

            if (child.name.StartsWith(Prefix))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }


    // Everything the theatre renders, minus the day roots themselves.
    internal static Bounds MeasureLobby(GameObject lobby)
    {
        Bounds b = new Bounds();

        bool any = false;

        Renderer[] renderers =
            lobby.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (IsInsideADayRoot(r.transform))
            {
                continue;
            }

            if (!any)
            {
                b = r.bounds;

                any = true;
            }
            else
            {
                b.Encapsulate(r.bounds);
            }
        }

        return any ? b : new Bounds();
    }


    internal static bool IsInsideADayRoot(Transform t)
    {
        while (t != null)
        {
            // Day2Changes..Day4Changes, their parent "Changes" and
            // BossChanges - none of it is the room itself.
            if (t.name.EndsWith("Changes"))
            {
                return true;
            }

            // The city seen through the window is scenery, not part
            // of the room - keep it out of the room measurement.
            if (t.name.StartsWith("LobbyCity"))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }


    // =========================================================
    // MATERIALS
    // =========================================================

    private class DecayMaterials
    {
        public Material dust;
        public Material damp;
        public Material peel;
        public Material web;
    }


    private static DecayMaterials BuildMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder(
                "Assets/Materials", "DayDecay");
        }

        DecayMaterials m = new DecayMaterials();

        m.dust = GetOrCreateOpaque(
            "M_Day2_Dust",
            new Color(0.34f, 0.31f, 0.27f),
            0.02f);

        m.damp = GetOrCreateOpaque(
            "M_Day2_Damp",
            new Color(0.17f, 0.16f, 0.14f),
            0.10f);

        m.peel = GetOrCreateOpaque(
            "M_Day2_Peel",
            new Color(0.40f, 0.37f, 0.33f),
            0.05f);

        m.web = GetOrCreateTransparent(
            "M_Day2_Cobweb",
            new Color(0.78f, 0.78f, 0.75f, 0.22f));

        // Give the flat-colour squares real shapes: a soft water stain,
        // an actual web, and paint that peels in ragged patches. Applied
        // every build, so materials made by an older run are upgraded.
        LobbyDecayShared.EnsureFolders();

        LobbyDecayShared.MakeBlended(m.damp,
            new Color(0.17f, 0.16f, 0.14f, 0.75f),
            LobbyDecayShared.StainTexture(), true);

        LobbyDecayShared.MakeBlended(m.web,
            new Color(0.78f, 0.78f, 0.75f, 0.45f),
            LobbyDecayShared.WebTexture(), true);

        LobbyDecayShared.MakeCutout(m.peel,
            new Color(0.40f, 0.37f, 0.33f),
            LobbyDecayShared.PeelTexture(), 0.05f);

        AssetDatabase.SaveAssets();

        return m;
    }


    internal static Shader LitShader()
    {
        Shader s = Shader.Find(
            "Universal Render Pipeline/Lit");

        if (s == null)
        {
            s = Shader.Find("Standard");
        }

        return s;
    }


    internal static void SetBaseColour(Material m, Color colour)
    {
        if (m.HasProperty("_BaseColor"))
        {
            m.SetColor("_BaseColor", colour);
        }
        else if (m.HasProperty("_Color"))
        {
            m.SetColor("_Color", colour);
        }
    }


    internal static Color GetBaseColour(Material m)
    {
        if (m.HasProperty("_BaseColor"))
        {
            return m.GetColor("_BaseColor");
        }

        if (m.HasProperty("_Color"))
        {
            return m.GetColor("_Color");
        }

        return Color.white;
    }


    internal static Material GetOrCreateOpaque(
        string name,
        Color colour,
        float smoothness)
    {
        string path = MaterialFolder + "/" + name + ".mat";

        Material existing =
            AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            return existing;
        }

        Material m = new Material(LitShader());

        SetBaseColour(m, colour);

        if (m.HasProperty("_Smoothness"))
        {
            m.SetFloat("_Smoothness", smoothness);
        }

        if (m.HasProperty("_Glossiness"))
        {
            m.SetFloat("_Glossiness", smoothness);
        }

        AssetDatabase.CreateAsset(m, path);

        return m;
    }


    internal static Material GetOrCreateTransparent(
        string name,
        Color colour)
    {
        string path = MaterialFolder + "/" + name + ".mat";

        Material existing =
            AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            return existing;
        }

        Material m = new Material(LitShader());

        SetBaseColour(m, colour);

        // URP transparent setup.
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
        }

        if (m.HasProperty("_Blend"))
        {
            m.SetFloat("_Blend", 0f);
        }

        if (m.HasProperty("_SrcBlend"))
        {
            m.SetFloat("_SrcBlend",
                (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (m.HasProperty("_DstBlend"))
        {
            m.SetFloat("_DstBlend",
                (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (m.HasProperty("_ZWrite"))
        {
            m.SetFloat("_ZWrite", 0f);
        }

        if (m.HasProperty("_Smoothness"))
        {
            m.SetFloat("_Smoothness", 0.0f);
        }

        m.SetShaderPassEnabled("ShadowCaster", false);

        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        m.DisableKeyword("_ALPHATEST_ON");

        m.SetOverrideTag("RenderType", "Transparent");

        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        AssetDatabase.CreateAsset(m, path);

        return m;
    }


    // Darkened, rougher copy of a lobby material - keeps the original
    // textures so the grime reads as the same surface, just neglected.
    private static Material GetOrCreateGrimeVariant(Material source)
    {
        string name =
            "M_Day2_" + Sanitise(source.name) + "_Grimy";

        string path = MaterialFolder + "/" + name + ".mat";

        Material existing =
            AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            return existing;
        }

        Material m = new Material(source);

        Color tint = new Color(0.72f, 0.70f, 0.66f, 1f);

        Color c = GetBaseColour(m);

        SetBaseColour(m,
            new Color(c.r * tint.r,
                      c.g * tint.g,
                      c.b * tint.b,
                      c.a));

        if (m.HasProperty("_Smoothness"))
        {
            m.SetFloat("_Smoothness",
                Mathf.Clamp01(
                    m.GetFloat("_Smoothness") * 0.45f));
        }

        AssetDatabase.CreateAsset(m, path);

        return m;
    }


    private static string Sanitise(string s)
    {
        return s.Replace(" ", "")
                .Replace("/", "")
                .Replace("&", "And")
                .Replace("(", "")
                .Replace(")", "");
    }


    // =========================================================
    // PRIMITIVE HELPER
    // =========================================================

    private static GameObject MakePiece(
        Transform parent,
        PrimitiveType shape,
        string name,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(shape);

        go.name = Prefix + name;

        Collider c = go.GetComponent<Collider>();

        if (c != null)
        {
            // Dressing must never get in the player's way.
            UnityEngine.Object.DestroyImmediate(c);
        }

        go.transform.SetParent(parent, true);

        go.transform.SetPositionAndRotation(position, rotation);

        go.transform.localScale = scale;

        Renderer r = go.GetComponent<Renderer>();

        r.sharedMaterial = material;

        r.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        go.isStatic = true;

        Undo.RegisterCreatedObjectUndo(go, "Day 2 Decay");

        return go;
    }


    // =========================================================
    // FLOOR DEBRIS
    // =========================================================

    private static int BuildFloorDebris(
        Transform dayRoot,
        Bounds room,
        DecayMaterials mats)
    {
        GameObject group = MakeGroup(dayRoot, "Debris");

        int made = 0;

        int attempts = 0;

        // Hug the edges of the room - rubbish collects against walls,
        // and the middle is where the player walks.
        while (made < 18 && attempts < 400)
        {
            attempts++;

            Vector3 probe = EdgeBiasedPoint(room);

            RaycastHit hit;

            if (!LobbyDecayShared.Cast(
                    probe,
                    Vector3.down,
                    room.size.y * 1.5f,
                    out hit))
            {
                continue;
            }

            // Only lie things on near-level surfaces.
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.85f)
            {
                continue;
            }

            float s = Random.Range(0.10f, 0.42f);

            MakePiece(
                group.transform,
                Random.value < 0.6f
                    ? PrimitiveType.Cube
                    : PrimitiveType.Sphere,
                "Debris_" + made,
                hit.point + Vector3.up * (s * 0.12f),
                Quaternion.Euler(
                    Random.Range(-6f, 6f),
                    Random.Range(0f, 360f),
                    Random.Range(-6f, 6f)),
                new Vector3(s, s * Random.Range(0.10f, 0.22f), s),
                mats.dust);

            made++;
        }

        return made;
    }


    private static Vector3 EdgeBiasedPoint(Bounds room)
    {
        // Pick a point in the room, pushed toward one of the walls.
        float x = Random.Range(room.min.x, room.max.x);

        float z = Random.Range(room.min.z, room.max.z);

        if (Random.value < 0.5f)
        {
            x = Random.value < 0.5f
                ? Mathf.Lerp(room.min.x, room.center.x,
                    Random.Range(0f, 0.22f))
                : Mathf.Lerp(room.max.x, room.center.x,
                    Random.Range(0f, 0.22f));
        }
        else
        {
            z = Random.value < 0.5f
                ? Mathf.Lerp(room.min.z, room.center.z,
                    Random.Range(0f, 0.22f))
                : Mathf.Lerp(room.max.z, room.center.z,
                    Random.Range(0f, 0.22f));
        }

        return new Vector3(x, room.max.y - 0.05f, z);
    }


    // =========================================================
    // CEILING DAMP
    // =========================================================

    private static int BuildCeilingDamp(
        Transform dayRoot,
        Bounds room,
        DecayMaterials mats)
    {
        GameObject group = MakeGroup(dayRoot, "CeilingDamp");

        int made = 0;

        int attempts = 0;

        while (made < 5 && attempts < 200)
        {
            attempts++;

            Vector3 probe = new Vector3(
                Random.Range(room.min.x, room.max.x),
                room.min.y + room.size.y * 0.25f,
                Random.Range(room.min.z, room.max.z));

            RaycastHit hit;

            if (!LobbyDecayShared.Cast(
                    probe,
                    Vector3.up,
                    room.size.y * 1.5f,
                    out hit))
            {
                continue;
            }

            if (Vector3.Dot(hit.normal, Vector3.down) < 0.85f)
            {
                continue;
            }

            float s = Random.Range(0.9f, 2.3f);

            MakePiece(
                group.transform,
                PrimitiveType.Quad,
                "Damp_" + made,
                hit.point - Vector3.up * 0.012f,
                Quaternion.LookRotation(Vector3.up)
                    * Quaternion.Euler(0f, 0f,
                        Random.Range(0f, 360f)),
                new Vector3(s, s * Random.Range(0.6f, 1.1f), 1f),
                mats.damp);

            made++;
        }

        return made;
    }


    // =========================================================
    // PEELING WALLS
    // =========================================================

    private static int BuildWallPeel(
        Transform dayRoot,
        Bounds room,
        DecayMaterials mats)
    {
        GameObject group = MakeGroup(dayRoot, "WallPeel");

        int made = 0;

        int attempts = 0;

        Vector3[] dirs =
        {
            Vector3.forward, Vector3.back,
            Vector3.left,    Vector3.right
        };

        while (made < 7 && attempts < 300)
        {
            attempts++;

            Vector3 from = new Vector3(
                Mathf.Lerp(room.min.x, room.max.x,
                    Random.Range(0.2f, 0.8f)),
                Mathf.Lerp(room.min.y, room.max.y,
                    Random.Range(0.25f, 0.70f)),
                Mathf.Lerp(room.min.z, room.max.z,
                    Random.Range(0.2f, 0.8f)));

            Vector3 dir = dirs[Random.Range(0, dirs.Length)];

            RaycastHit hit;

            if (!LobbyDecayShared.Cast(
                    from,
                    dir,
                    Mathf.Max(room.size.x, room.size.z),
                    out hit))
            {
                continue;
            }

            // Vertical surfaces only.
            if (Mathf.Abs(
                    Vector3.Dot(hit.normal, Vector3.up)) > 0.30f)
            {
                continue;
            }

            float w = Random.Range(0.35f, 1.0f);

            float h = Random.Range(0.5f, 1.6f);

            MakePiece(
                group.transform,
                PrimitiveType.Quad,
                "Peel_" + made,
                hit.point + hit.normal * 0.015f,
                LobbyDecayShared.Facing(hit.normal,
                    Random.Range(0f, 360f)),
                new Vector3(w, h, 1f),
                mats.peel);

            made++;
        }

        return made;
    }


    // =========================================================
    // COBWEBS
    // =========================================================

    private static int BuildCobwebs(
        Transform dayRoot,
        Bounds room,
        DecayMaterials mats)
    {
        GameObject group = MakeGroup(dayRoot, "Cobwebs");

        int made = 0;

        int attempts = 0;

        Vector3[] dirs =
        {
            Vector3.forward, Vector3.back,
            Vector3.left,    Vector3.right
        };

        while (made < 8 && attempts < 300)
        {
            attempts++;

            Vector3 from = new Vector3(
                Mathf.Lerp(room.min.x, room.max.x,
                    Random.Range(0.1f, 0.9f)),
                Mathf.Lerp(room.min.y, room.max.y,
                    Random.Range(0.72f, 0.92f)),
                Mathf.Lerp(room.min.z, room.max.z,
                    Random.Range(0.1f, 0.9f)));

            Vector3 dir = dirs[Random.Range(0, dirs.Length)];

            RaycastHit hit;

            if (!LobbyDecayShared.Cast(
                    from,
                    dir,
                    Mathf.Max(room.size.x, room.size.z),
                    out hit))
            {
                continue;
            }

            if (Mathf.Abs(
                    Vector3.Dot(hit.normal, Vector3.up)) > 0.30f)
            {
                continue;
            }

            float s = Random.Range(0.45f, 1.1f);

            // Tilted square reads as a corner web from most angles.
            MakePiece(
                group.transform,
                PrimitiveType.Quad,
                "Web_" + made,
                hit.point + hit.normal * 0.02f,
                LobbyDecayShared.Facing(hit.normal,
                    Random.Range(0f, 360f)),
                new Vector3(s, s, 1f),
                mats.web);

            made++;
        }

        return made;
    }


    private static GameObject MakeGroup(
        Transform parent,
        string name)
    {
        GameObject go = new GameObject(Prefix + name);

        go.transform.SetParent(parent, false);

        go.transform.localPosition = Vector3.zero;

        go.transform.localRotation = Quaternion.identity;

        go.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(go, "Day 2 Decay");

        return go;
    }


    // =========================================================
    // GRIME - REPLACEMENT PASS
    // =========================================================

    // Materials whose surfaces read as "the floor and the walls the
    // player is closest to". These get a grimy twin.
    private const int MaxGrimeReplacements = 12;


    private static readonly string[] GrimeTargets =
    {
        "Carpet",
        "LobbyFloor",
        "LobbyFront"
    };


    private static List<GameObject> BuildGrimeReplacements(
        GameObject lobby,
        Transform dayRoot,
        DecayMaterials mats)
    {
        GameObject group = MakeGroup(dayRoot, "Grime");

        List<GameObject> switchOff = new List<GameObject>();

        Renderer[] renderers =
            lobby.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (IsInsideADayRoot(r.transform))
            {
                continue;
            }

            if (switchOff.Count >= MaxGrimeReplacements)
            {
                break;
            }

            if (!UsesAGrimeTarget(r))
            {
                continue;
            }

            // Only swap plain scenery. Anything with behaviour, a
            // light, or children stays exactly as it is.
            if (r.GetComponent<MonoBehaviour>() != null ||
                r.GetComponent<Light>() != null ||
                r.transform.childCount > 0)
            {
                continue;
            }

            MeshFilter mf = r.GetComponent<MeshFilter>();

            if (mf == null || mf.sharedMesh == null)
            {
                continue;
            }

            GameObject twin = new GameObject(
                Prefix + "Grimy_" + r.gameObject.name);

            twin.transform.SetParent(group.transform, false);

            twin.transform.SetPositionAndRotation(
                r.transform.position,
                r.transform.rotation);

            twin.transform.localScale =
                r.transform.lossyScale;

            MeshFilter twinFilter =
                twin.AddComponent<MeshFilter>();

            twinFilter.sharedMesh = mf.sharedMesh;

            MeshRenderer twinRenderer =
                twin.AddComponent<MeshRenderer>();

            Material[] source = r.sharedMaterials;

            Material[] swapped = new Material[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                swapped[i] = source[i] == null
                    ? null
                    : GetOrCreateGrimeVariant(source[i]);
            }

            twinRenderer.sharedMaterials = swapped;

            twinRenderer.shadowCastingMode =
                r.shadowCastingMode;

            // The original is about to be switched off, so its
            // collider goes with it - carry a matching one over or
            // the player walks through the floor.
            CopyCollider(r.gameObject, twin);

            twin.isStatic = r.gameObject.isStatic;

            Undo.RegisterCreatedObjectUndo(twin, "Day 2 Decay");

            switchOff.Add(r.gameObject);
        }

        return switchOff;
    }


    private static bool UsesAGrimeTarget(Renderer r)
    {
        foreach (Material m in r.sharedMaterials)
        {
            if (m == null)
            {
                continue;
            }

            foreach (string target in GrimeTargets)
            {
                if (m.name.Replace(" (Instance)", "")
                     .Equals(target,
                         System.StringComparison
                             .OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }


    private static void CopyCollider(
        GameObject from,
        GameObject to)
    {
        MeshCollider mc = from.GetComponent<MeshCollider>();

        if (mc != null)
        {
            MeshCollider copy = to.AddComponent<MeshCollider>();

            copy.sharedMesh = mc.sharedMesh;

            copy.convex = mc.convex;

            copy.sharedMaterial = mc.sharedMaterial;

            return;
        }

        BoxCollider bc = from.GetComponent<BoxCollider>();

        if (bc != null)
        {
            BoxCollider copy = to.AddComponent<BoxCollider>();

            copy.center = bc.center;

            copy.size = bc.size;

            copy.sharedMaterial = bc.sharedMaterial;
        }
    }


    // =========================================================
    // WIRE UP THE TOGGLE COMPONENT
    // =========================================================

    private static void WireComponent(
        GameObject lobby,
        GameObject dayRoot,
        List<GameObject> switchOff)
    {
        LobbyDayVisuals visuals =
            lobby.GetComponent<LobbyDayVisuals>();

        if (visuals == null)
        {
            visuals = Undo.AddComponent<LobbyDayVisuals>(lobby);
        }

        Undo.RecordObject(visuals, "Day 2 Decay");

        SerializedObject so = new SerializedObject(visuals);

        SerializedProperty layers = so.FindProperty("layers");

        // Find or create the day 2 layer.
        SerializedProperty layer = null;

        for (int i = 0; i < layers.arraySize; i++)
        {
            SerializedProperty candidate =
                layers.GetArrayElementAtIndex(i);

            if (candidate.FindPropertyRelative("fromDay")
                    .intValue == 2)
            {
                layer = candidate;

                break;
            }
        }

        if (layer == null)
        {
            layers.InsertArrayElementAtIndex(layers.arraySize);

            layer = layers.GetArrayElementAtIndex(
                layers.arraySize - 1);

            layer.FindPropertyRelative("fromDay").intValue = 2;
        }

        layer.FindPropertyRelative("addRoot")
             .objectReferenceValue = dayRoot;

        SerializedProperty list =
            layer.FindPropertyRelative("switchOff");

        list.ClearArray();

        for (int i = 0; i < switchOff.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);

            list.GetArrayElementAtIndex(i)
                .objectReferenceValue = switchOff[i];
        }

        so.ApplyModifiedProperties();
    }
}
#endif
