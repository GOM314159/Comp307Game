#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Builds the Day 3 dress pass for the lobby - clearly worse than Day 2.
///
/// Day 3 stacks on top of Day 2 (GameFlowManager / LobbyDayVisuals show
/// every day up to the current one), so this only adds what is NEW:
///
///   * A piece of the ceiling has come down. The ceiling is NOT cut
///     open - there is no hole. A dark ragged patch of exposed lath shows
///     where it came from, a few laths hang from the edge, and the chunk
///     lies underneath in a spread of plaster dust, shards and a puddle.
///   * Damp stains spreading out from the patch; water running down the
///     nearest walls.
///   * The first cracks in the walls; heavier cobwebs.
///   * Plaster rubble across the room.
///   * The lights start to go: everything a little dimmer, and the light
///     nearest the collapse stutters.
///   * The room reads a touch darker and colder (a post-processing
///     volume that exists only from Day 3).
///
/// Everything it makes is "D3_*" under Day3Changes (wherever that lives
/// under the Lobby), so re-running clears only its own work - the Level
/// blocks and anything else in there are left alone. Nothing has a
/// collider or casts a shadow.
///
/// Tools > Strings and Statutes > Lobby > Build Day 3 Decay
/// </summary>
public static class LobbyDay3DecayBuilder
{
    private const string DayRootName = "Day3Changes";
    private const string Prefix = "D3_";
    private const string Tag = "[Day3Decay]";

    private const int Seed = 20803;

    // How far from the player spawn the collapse may land: close enough
    // to be seen on arrival, far enough not to sit on top of the player.
    private const float SiteMinRadius = 2.5f;
    private const float SiteMaxRadius = 9.0f;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem("Tools/Strings and Statutes/Lobby/Build Day 3 Decay")]
    public static void Build()
    {
        BuildCore(true);
    }


    [MenuItem("Tools/Strings and Statutes/Lobby/Clear Day 3 Decay")]
    public static void Clear()
    {
        GameObject lobby = LobbyDay2DecayBuilder.FindLobby();

        if (lobby == null)
        {
            return;
        }

        Transform root = LobbyDay2DecayBuilder.FindDeep(
            lobby.transform, DayRootName);

        if (root == null)
        {
            return;
        }

        LobbyDecayShared.ClearGenerated(root, Prefix);

        EditorSceneManager.MarkSceneDirty(lobby.scene);

        Debug.Log(Tag + " Cleared generated objects.");
    }


    // Kept for anyone already calling it - now builds every day.
    public static void BuildAllAndSaveBatch()
    {
        LobbyDecayShared.BuildAllAndSaveBatch();
    }


    // =========================================================
    // BUILD
    // =========================================================

    internal static bool BuildCore(bool interactive)
    {
        GameObject lobby = LobbyDay2DecayBuilder.FindLobby();

        if (lobby == null)
        {
            Report(interactive, "There is no 'Lobby' object in the open "
                + "scene. Open " + LobbyDecayShared.LobbyScenePath
                + " and run this again.");

            return false;
        }

        Undo.SetCurrentGroupName("Build Day 3 Decay");

        int group = Undo.GetCurrentGroup();

        Transform root = LobbyDecayShared.FindOrCreateDayRoot(
            lobby, DayRootName, Tag);

        LobbyDecayShared.ClearGenerated(root, Prefix);

        Physics.SyncTransforms();

        Bounds room = LobbyDay2DecayBuilder.MeasureLobby(lobby);

        if (room.size.sqrMagnitude < 1f)
        {
            Report(interactive, "Found no renderers under the Lobby to "
                + "measure.");

            return false;
        }

        Random.InitState(Seed);

        Mats mats = BuildMaterials();

        Vector3 anchor = LobbyDecayShared.Anchor(lobby, room);

        float floorY = LobbyDecayShared.FloorHeight(anchor);

        List<Bounds> keepClear = LobbyDecayShared.KeepClearZones(lobby);

        Vector3 ceilingPoint;

        Vector3 floorPoint;

        if (!FindSite(anchor, floorY, keepClear,
                out ceilingPoint, out floorPoint))
        {
            Report(interactive, "Could not find a clear patch of ceiling "
                + "with open floor beneath it within " + SiteMaxRadius
                + " m of the player spawn (" + anchor + ").");

            return false;
        }

        int made = 0;

        made += BuildCollapse(root, ceilingPoint, floorPoint, mats);

        made += BuildDampSpread(root, ceilingPoint, mats);

        GameObject rubble = LobbyDecayShared.Group(root, Prefix, "PlasterScatter");

        made += LobbyDecayShared.RubbleAround(rubble.transform, Prefix,
            "Rubble", anchor, floorY, 1.5f, 8.5f, 16, 0.06f, 0.2f,
            mats.plaster);

        made += BuildWalls(root, anchor, floorPoint, floorY, mats);

        int lights = BuildLights(root, lobby, room, ceilingPoint);

        LobbyDecayShared.BuildAtmosphere(root, Prefix, "VP_Day3_Lobby",
            50,
            -0.12f,                         // exposure
            -10f,                           // saturation
            5f,                             // contrast
            new Color(0.97f, 0.98f, 1.0f),  // a little colder
            0.05f);                         // vignette

        LobbyDecayShared.WireLayer(lobby, root.gameObject, 3);

        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(lobby.scene);

        Undo.CollapseUndoOperations(group);

        Debug.Log(Tag + " Built " + made + " object(s) under "
            + root.name + ", dimmed " + lights + " light(s). Ceiling "
            + "piece came down at " + ceilingPoint + ", landed at "
            + floorPoint + ".");

        Selection.activeGameObject = root.gameObject;

        return true;
    }


    private static void Report(bool interactive, string message)
    {
        Debug.LogError(Tag + " " + message);

        if (interactive)
        {
            EditorUtility.DisplayDialog("Day 3 Decay", message, "OK");
        }
    }


    // =========================================================
    // MATERIALS
    // =========================================================

    private class Mats
    {
        public Material plaster;
        public Material rim;
        public Material exposed;
        public Material lath;
        public Material dust;
        public Material puddle;
        public Material damp;
        public Material crack;
        public Material streak;
        public Material web;
    }


    private static Mats BuildMaterials()
    {
        LobbyDecayShared.EnsureFolders();

        Texture2D stain = LobbyDecayShared.StainTexture();

        Mats m = new Mats();

        m.plaster = LobbyDecayShared.Opaque("M_Day3_Plaster",
            new Color(0.52f, 0.50f, 0.46f), 0.04f, false);

        m.rim = LobbyDecayShared.Opaque("M_Day3_Rim",
            new Color(0.30f, 0.28f, 0.25f), 0.04f, true);

        m.exposed = LobbyDecayShared.Opaque("M_Day3_Exposed",
            new Color(0.045f, 0.038f, 0.032f), 0.05f, true);

        m.lath = LobbyDecayShared.Opaque("M_Day3_Lath",
            new Color(0.12f, 0.085f, 0.055f), 0.06f, false);

        m.puddle = LobbyDecayShared.Opaque("M_Day3_Puddle",
            new Color(0.015f, 0.020f, 0.025f), 0.95f, false);

        m.crack = LobbyDecayShared.Opaque("M_Day3_Crack",
            new Color(0.025f, 0.022f, 0.02f), 0.05f, true);

        // Soft, texture-shaped - no more square patches.
        m.dust = LobbyDecayShared.Blended("M_Day3_Dust",
            new Color(0.42f, 0.40f, 0.36f, 0.9f), stain, false);

        m.damp = LobbyDecayShared.Blended("M_Day3_Damp",
            new Color(0.10f, 0.09f, 0.075f, 0.85f), stain, true);

        m.streak = LobbyDecayShared.Blended("M_Day3_Streak",
            new Color(0.09f, 0.08f, 0.06f, 0.8f),
            LobbyDecayShared.StreakTexture(), true);

        m.web = LobbyDecayShared.Blended("M_Day3_Web",
            new Color(0.80f, 0.80f, 0.77f, 0.55f),
            LobbyDecayShared.WebTexture(), true);

        AssetDatabase.SaveAssets();

        return m;
    }


    // =========================================================
    // WHERE
    // =========================================================

    // A spot where the ceiling is a real ceiling (faces down, well above
    // head height) AND there is open floor directly beneath it, away
    // from portals, doors and spawns.
    private static bool FindSite(
        Vector3 anchor,
        float floorY,
        List<Bounds> keepClear,
        out Vector3 ceilingPoint,
        out Vector3 floorPoint)
    {
        ceilingPoint = Vector3.zero;

        floorPoint = Vector3.zero;

        Vector3 eye = new Vector3(anchor.x, floorY + 1.6f, anchor.z);

        for (int i = 0; i < 900; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);

            float radius = Random.Range(SiteMinRadius, SiteMaxRadius);

            Vector3 probe = new Vector3(
                anchor.x + Mathf.Cos(angle) * radius,
                floorY + 0.5f,
                anchor.z + Mathf.Sin(angle) * radius);

            if (!LobbyDecayShared.SameRoom(eye, probe + Vector3.up * 1.1f))
            {
                continue;
            }

            RaycastHit up;

            if (!LobbyDecayShared.CeilingAbove(probe, 20f, out up) ||
                up.point.y - floorY < 2.4f)
            {
                continue;
            }

            if (!LobbyDecayShared.OpenFloor(probe, floorY, 0.9f))
            {
                continue;
            }

            if (!LobbyDecayShared.IsClear(probe, 1.5f, keepClear))
            {
                continue;
            }

            RaycastHit down;

            if (!LobbyDecayShared.FloorBelow(
                    up.point - Vector3.up * 0.2f, floorY, out down))
            {
                continue;
            }

            ceilingPoint = up.point;

            floorPoint = down.point;

            return true;
        }

        return false;
    }


    // =========================================================
    // THE PIECE THAT CAME DOWN
    // =========================================================

    private static int BuildCollapse(
        Transform root,
        Vector3 ceilingPoint,
        Vector3 floorPoint,
        Mats mats)
    {
        GameObject group = LobbyDecayShared.Group(root, Prefix, "CeilingCollapse");

        Transform g = group.transform;

        int made = 0;

        // ---- the scar on the ceiling ----------------------------

        float[] radii = LobbyDecayShared.RaggedRadii(11, 0.38f, 0.62f);

        Mesh scarMesh = LobbyDecayShared.SaveMesh("D3_CeilingScar",
            LobbyDecayShared.PatchMesh("D3_CeilingScar", radii, 1f));

        Mesh rimMesh = LobbyDecayShared.SaveMesh("D3_CeilingScarRim",
            LobbyDecayShared.PatchMesh("D3_CeilingScarRim", radii, 1.24f));

        float scarW = Random.Range(1.5f, 1.9f);

        float scarH = scarW * Random.Range(0.7f, 0.95f);

        Quaternion down = LobbyDecayShared.Facing(
            Vector3.down, Random.Range(0f, 360f));

        // The lighter broken edge 8 mm below the ceiling, the exposed
        // lath 16 mm below so it draws on top. Nothing is removed, so
        // there is no hole.
        LobbyDecayShared.Piece(g, Prefix, "ScarRim", rimMesh,
            ceilingPoint - Vector3.up * 0.008f, down,
            new Vector3(scarW, scarH, 1f), mats.rim);

        LobbyDecayShared.Piece(g, Prefix, "ScarExposed", scarMesh,
            ceilingPoint - Vector3.up * 0.016f, down,
            new Vector3(scarW, scarH, 1f), mats.exposed);

        made += 2;

        // ---- laths still hanging from the edge ------------------

        made += HangingLaths(g, Prefix, ceilingPoint, scarW * 0.5f, scarH * 0.5f,
            Random.Range(3, 5), 0.45f, 0.85f, mats.lath);

        // ---- on the floor underneath ----------------------------

        Quaternion up = LobbyDecayShared.Facing(
            Vector3.up, Random.Range(0f, 360f));

        // Plaster dust thrown out from the impact.
        float dust = Random.Range(2.8f, 3.6f);

        LobbyDecayShared.Primitive(g, Prefix, "FloorDust",
            PrimitiveType.Quad, floorPoint + Vector3.up * 0.004f, up,
            new Vector3(dust, dust * 0.85f, 1f), mats.dust);

        made++;

        // The pipe above it let go with the ceiling.
        float puddle = Random.Range(0.55f, 0.8f);

        Mesh puddleMesh = LobbyDecayShared.SaveMesh("D3_Puddle",
            LobbyDecayShared.PatchMesh("D3_Puddle",
                LobbyDecayShared.RaggedRadii(16, 0.75f, 1.0f), 1f));

        LobbyDecayShared.Piece(g, Prefix, "FloorPuddle", puddleMesh,
            floorPoint + new Vector3(
                Random.Range(-0.4f, 0.4f), 0.008f,
                Random.Range(-0.4f, 0.4f)),
            LobbyDecayShared.Facing(Vector3.up, Random.Range(0f, 360f)),
            new Vector3(puddle * 1.4f, puddle, 1f), mats.puddle);

        made++;

        // The chunk itself.
        Mesh chunk = LobbyDecayShared.SaveMesh("D3_FallenChunk",
            LobbyDecayShared.ChunkMesh("D3_FallenChunk",
                new Vector3(0.95f, 0.2f, 0.7f)));

        LobbyDecayShared.Piece(g, Prefix, "FallenChunk", chunk,
            floorPoint + Vector3.up * 0.09f,
            Quaternion.Euler(Random.Range(-5f, 5f),
                Random.Range(0f, 360f), Random.Range(-5f, 5f)),
            Vector3.one, mats.plaster);

        made++;

        // Pieces that broke off it when it landed.
        made += LobbyDecayShared.RubbleAround(g, Prefix, "Shard",
            floorPoint, floorPoint.y, 0.55f, 1.7f, Random.Range(6, 9),
            0.10f, 0.32f, mats.plaster);

        return made;
    }


    internal static int HangingLaths(
        Transform parent,
        string prefix,
        Vector3 ceilingPoint,
        float halfW,
        float halfH,
        int count,
        float minLength,
        float maxLength,
        Material material)
    {
        int made = 0;

        for (int i = 0; i < count; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);

            Vector3 edge = ceilingPoint + new Vector3(
                Mathf.Cos(a) * halfW * 0.85f,
                -0.03f,
                Mathf.Sin(a) * halfH * 0.85f);

            float length = Random.Range(minLength, maxLength);

            // Droop inward and down, like a board snapped at its end.
            Vector3 inward = -new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));

            Vector3 dir = (inward * Random.Range(0.25f, 0.6f)
                + Vector3.down).normalized;

            Vector3 side = Vector3.Cross(dir, inward).normalized;

            Quaternion rot = Quaternion.LookRotation(dir,
                side.sqrMagnitude > 0.01f ? side : Vector3.forward);

            LobbyDecayShared.Primitive(parent, prefix, "HangingLath_" + i,
                PrimitiveType.Cube, edge + dir * (length * 0.5f), rot,
                new Vector3(0.05f, 0.014f, length), material);

            made++;
        }

        return made;
    }


    // =========================================================
    // DAMP SPREADING FROM THE PATCH
    // =========================================================

    private static int BuildDampSpread(
        Transform root,
        Vector3 ceilingPoint,
        Mats mats)
    {
        GameObject group = LobbyDecayShared.Group(root, Prefix, "DampSpread");

        int made = 0;

        for (int attempt = 0; attempt < 30 && made < 6; attempt++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);

            float r = Random.Range(0.6f, 2.4f);

            Vector3 at = ceilingPoint + new Vector3(
                Mathf.Cos(a) * r, -0.6f, Mathf.Sin(a) * r);

            RaycastHit hit;

            // Only stain where there really is ceiling.
            if (!LobbyDecayShared.CeilingAbove(at, 1.2f, out hit))
            {
                continue;
            }

            float s = Random.Range(1.0f, 2.2f);

            LobbyDecayShared.Primitive(group.transform, Prefix,
                "Damp_" + made, PrimitiveType.Quad,
                hit.point - Vector3.up * (0.020f + made * 0.002f),
                LobbyDecayShared.Facing(Vector3.down, Random.Range(0f, 360f)),
                new Vector3(s, s * Random.Range(0.65f, 1.1f), 1f),
                mats.damp);

            made++;
        }

        return made;
    }


    // =========================================================
    // WALLS - CRACKS, WATER, WEBS
    // =========================================================

    private static int BuildWalls(
        Transform root,
        Vector3 anchor,
        Vector3 collapseFloor,
        float floorY,
        Mats mats)
    {
        int made = 0;

        // ---- first cracks ---------------------------------------

        GameObject cracks = LobbyDecayShared.Group(root, Prefix, "WallCracks");

        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;

            float length = Random.Range(0.7f, 1.6f);

            if (!LobbyDecayShared.FindWall(anchor, floorY, 8f,
                    Mathf.Max(1.2f, length * 0.5f + 0.2f), 2.6f,
                    length * 0.35f, length * 0.5f, out hit))
            {
                continue;
            }

            string n = "D3_Crack_" + i;

            Mesh mesh = LobbyDecayShared.SaveMesh(n,
                LobbyDecayShared.CrackMesh(n, length,
                    Random.Range(0.018f, 0.03f), Random.Range(1, 3)));

            LobbyDecayShared.Piece(cracks.transform, Prefix, "Crack_" + i,
                mesh, hit.point + hit.normal * 0.008f,
                LobbyDecayShared.Facing(hit.normal, Random.Range(-40f, 40f)),
                Vector3.one, mats.crack);

            made++;
        }

        // ---- water running down the walls near the collapse -----

        GameObject streaks = LobbyDecayShared.Group(root, Prefix, "WaterStreaks");

        for (int i = 0; i < 3; i++)
        {
            if (Streak(streaks.transform, Prefix, "Streak_" + i,
                    collapseFloor, floorY, 5f, Random.Range(0.35f, 0.6f),
                    mats.streak))
            {
                made++;
            }
        }

        // ---- heavier cobwebs ------------------------------------

        GameObject webs = LobbyDecayShared.Group(root, Prefix, "Cobwebs");

        for (int i = 0; i < 6; i++)
        {
            RaycastHit hit;

            float s = Random.Range(0.9f, 1.6f);

            if (!LobbyDecayShared.FindWall(anchor, floorY, 9f,
                    2.0f, 2.8f, s * 0.35f, s * 0.35f, out hit))
            {
                continue;
            }

            LobbyDecayShared.Primitive(webs.transform, Prefix, "Web_" + i,
                PrimitiveType.Quad, hit.point + hit.normal * (0.03f + i * 0.002f),
                LobbyDecayShared.Facing(hit.normal, Random.Range(0f, 360f)),
                new Vector3(s, s, 1f), mats.web);

            made++;
        }

        return made;
    }


    /// <summary>
    /// A water streak from where the wall meets the ceiling, running
    /// most of the way to the floor.
    /// </summary>
    internal static bool Streak(
        Transform parent,
        string prefix,
        string name,
        Vector3 near,
        float floorY,
        float reach,
        float width,
        Material material)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            RaycastHit wall;

            if (!LobbyDecayShared.FindWall(near, floorY, reach, 1.6f, 2.3f,
                    width * 0.5f, 0.6f, out wall))
            {
                return false;
            }

            // Find the top of this wall.
            RaycastHit top;

            Vector3 up = wall.point + wall.normal * 0.06f;

            float topY = LobbyDecayShared.Cast(up, Vector3.up, 8f, out top)
                ? top.point.y
                : wall.point.y + 1.0f;

            float length = Mathf.Min(
                (topY - floorY) * Random.Range(0.55f, 0.85f),
                Random.Range(1.6f, 2.8f));

            // The wall must be there for the streak's whole run - no
            // water hanging in a doorway.
            if (!WallAlongRun(wall, topY, length))
            {
                continue;
            }

            PlaceStreak(parent, prefix, name, wall, topY, width, length,
                material);

            return true;
        }

        return false;
    }


    private static bool WallAlongRun(RaycastHit wall, float topY, float length)
    {
        float[] at = { 0.04f, 0.5f, 0.96f };

        foreach (float t in at)
        {
            Vector3 p = new Vector3(wall.point.x, topY - length * t, wall.point.z)
                + wall.normal * 0.3f;

            RaycastHit probe;

            if (!LobbyDecayShared.Cast(p, -wall.normal, 0.6f, out probe) ||
                Mathf.Abs(probe.distance - 0.3f) > 0.05f ||
                Vector3.Dot(probe.normal, wall.normal) < 0.96f)
            {
                return false;
            }
        }

        return true;
    }


    private static void PlaceStreak(
        Transform parent,
        string prefix,
        string name,
        RaycastHit wall,
        float topY,
        float width,
        float length,
        Material material)
    {
        string meshName = prefix + name;

        Mesh mesh = LobbyDecayShared.SaveMesh(meshName,
            LobbyDecayShared.StreakMesh(meshName, width, length));

        Vector3 at = new Vector3(wall.point.x, topY - 0.02f, wall.point.z)
            + wall.normal * 0.012f;

        LobbyDecayShared.Piece(parent, prefix, name, mesh, at,
            LobbyDecayShared.Facing(wall.normal, 0f), Vector3.one, material);
    }


    // =========================================================
    // LIGHTS
    // =========================================================

    // Everything 15% dimmer; the light nearest the collapse stutters.
    private static int BuildLights(
        Transform root,
        GameObject lobby,
        Bounds room,
        Vector3 ceilingPoint)
    {
        List<Light> lights = LobbyDecayShared.LobbyLights(lobby, room);

        if (lights.Count == 0)
        {
            Debug.LogWarning(Tag + " No realtime/mixed lights found in "
                + "the lobby - skipping the light decay.");

            return 0;
        }

        Light stutter = LobbyDecayShared.Nearest(lights, ceilingPoint, null);

        List<LobbyLightDecay.Entry> entries = new List<LobbyLightDecay.Entry>();

        foreach (Light l in lights)
        {
            LobbyLightDecay.Entry e = new LobbyLightDecay.Entry();

            e.light = l;

            e.intensity = 0.85f;

            if (l == stutter)
            {
                e.flicker = true;

                e.flickerDepth = 0.65f;

                e.flickerRate = 1f;
            }

            entries.Add(e);
        }

        LobbyDecayShared.WireLights(root, Prefix, entries);

        Debug.Log(Tag + " Lights: " + lights.Count + " dimmed to 85%, '"
            + stutter.name + "' flickers.");

        return lights.Count;
    }
}
#endif
