#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Builds the Day 4 dress pass for the lobby - the building is failing.
///
/// Stacks on top of Days 2 and 3, so everything here is NEW on Day 4 and
/// a clear step up from Day 3:
///
///   * A major collapse: a big section of ceiling has given way, a
///     ceiling beam has snapped and come down at an angle with one end
///     still up in the ceiling and the other on the floor, exposed joists
///     show in the opening, cables hang out of it, and the floor beneath
///     is a pile of slabs and rubble. Again NO hole is cut - the dark
///     opening is dressing on the existing ceiling.
///     This is the one pile the player cannot walk through: an invisible
///     box collider (plus a NavMesh obstacle) covers it, and the site is
///     chosen well clear of every portal, door, spawn and jumpscare.
///   * A second, smaller fall elsewhere in the room.
///   * Water everywhere: big damp patches across the ceiling, water
///     running down the walls, standing puddles on the floor.
///   * The walls are going: long branching cracks, two places where the
///     plaster has burst off the wall, heavy peeling.
///   * Thick cobwebs, including sheets hanging from the ceiling.
///   * The lights are dying: everything dimmer again, the light over the
///     collapse is dead, and two more stutter hard.
///   * The room is markedly darker, colder and more washed-out.
///
/// Everything is "D4_*" under Day4Changes, so re-running clears only its
/// own work. Run Day 3 first - Day 4 keeps its collapse away from Day 3's.
///
/// Tools > Strings and Statutes > Lobby > Build Day 4 Decay
/// </summary>
public static class LobbyDay4DecayBuilder
{
    private const string DayRootName = "Day4Changes";
    private const string Prefix = "D4_";
    private const string Tag = "[Day4Decay]";

    private const int Seed = 20804;

    private const float SiteMinRadius = 3.0f;
    private const float SiteMaxRadius = 11.0f;

    // The main pile blocks movement, so it stays this far (horizontally)
    // from anything the player must reach.
    private const float BlockingClearance = 2.5f;

    private const float MinDistanceFromDay3 = 4.5f;


    // =========================================================
    // MENU
    // =========================================================

    [MenuItem("Tools/Strings and Statutes/Lobby/Build Day 4 Decay")]
    public static void Build()
    {
        BuildCore(true);
    }


    [MenuItem("Tools/Strings and Statutes/Lobby/Clear Day 4 Decay")]
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

        Undo.SetCurrentGroupName("Build Day 4 Decay");

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

        List<Vector3> avoid = new List<Vector3>();

        Vector3 day3Site;

        bool haveDay3 = FindDay3Site(lobby, out day3Site);

        if (haveDay3)
        {
            avoid.Add(day3Site);
        }
        else
        {
            Debug.LogWarning(Tag + " No Day 3 collapse found - build Day 3 "
                + "first so the two falls don't land on each other.");
        }

        // ---- the main collapse ----------------------------------

        Vector3 ceiling, foot, beamTop;

        if (!FindMainSite(anchor, floorY, keepClear, avoid,
                out ceiling, out beamTop, out foot))
        {
            Report(interactive, "Could not find a stretch of ceiling with "
                + "enough open floor beneath it for the main collapse, "
                + "within " + SiteMaxRadius + " m of the player spawn and "
                + BlockingClearance + " m clear of portals and doors.");

            return false;
        }

        int made = 0;

        made += BuildMainCollapse(root, ceiling, beamTop, foot, floorY, mats);

        avoid.Add(new Vector3(ceiling.x, floorY, ceiling.z));

        // ---- the second, smaller fall ---------------------------

        Vector3 smallCeiling, smallFloor;

        if (FindSmallSite(anchor, floorY, keepClear, avoid,
                out smallCeiling, out smallFloor))
        {
            made += BuildSmallFall(root, smallCeiling, smallFloor, mats);
        }
        else
        {
            Debug.LogWarning(Tag + " No room for the second, smaller "
                + "fall - skipped.");
        }

        // ---- water, walls, webs ---------------------------------

        Vector3 mainFloor = new Vector3(ceiling.x, floorY, ceiling.z);

        made += BuildWater(root, anchor, mainFloor, ceiling, floorY, mats);

        made += BuildWalls(root, anchor, floorY, mats);

        made += BuildWebs(root, anchor, floorY, mats);

        // ---- lights and atmosphere ------------------------------

        int lights = BuildLights(root, lobby, room, anchor, ceiling);

        LobbyDecayShared.BuildAtmosphere(root, Prefix, "VP_Day4_Lobby",
            60,
            -0.32f,                         // exposure
            -26f,                           // saturation
            12f,                            // contrast
            new Color(0.90f, 0.95f, 0.97f), // cold, sickly
            0.13f);                         // vignette

        LobbyDecayShared.WireLayer(lobby, root.gameObject, 4);

        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(lobby.scene);

        Undo.CollapseUndoOperations(group);

        Debug.Log(Tag + " Built " + made + " object(s) under " + root.name
            + ", " + lights + " light(s) affected. Main collapse at "
            + ceiling + ", beam foot at " + foot + ".");

        Selection.activeGameObject = root.gameObject;

        return true;
    }


    private static void Report(bool interactive, string message)
    {
        Debug.LogError(Tag + " " + message);

        if (interactive)
        {
            EditorUtility.DisplayDialog("Day 4 Decay", message, "OK");
        }
    }


    // =========================================================
    // MATERIALS
    // =========================================================

    private class Mats
    {
        public Material plaster;
        public Material plasterDark;
        public Material rim;
        public Material exposed;
        public Material joist;
        public Material beam;
        public Material lath;
        public Material cable;
        public Material brick;
        public Material dust;
        public Material puddle;
        public Material wetFloor;
        public Material damp;
        public Material crack;
        public Material streak;
        public Material peel;
        public Material web;
    }


    private static Mats BuildMaterials()
    {
        LobbyDecayShared.EnsureFolders();

        Texture2D stain = LobbyDecayShared.StainTexture();

        Mats m = new Mats();

        // Shared with Day 3 where the stuff is the same stuff.
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

        // New on Day 4.
        m.plasterDark = LobbyDecayShared.Opaque("M_Day4_PlasterWet",
            new Color(0.34f, 0.32f, 0.29f), 0.25f, false);

        m.joist = LobbyDecayShared.Opaque("M_Day4_Joist",
            new Color(0.06f, 0.042f, 0.03f), 0.05f, false);

        m.beam = LobbyDecayShared.Opaque("M_Day4_Beam",
            new Color(0.15f, 0.10f, 0.065f), 0.08f, false);

        m.cable = LobbyDecayShared.Opaque("M_Day4_Cable",
            new Color(0.02f, 0.02f, 0.02f), 0.35f, false);

        m.brick = LobbyDecayShared.Opaque("M_Day4_Brick",
            new Color(0.16f, 0.075f, 0.055f), 0.05f, true);

        m.dust = LobbyDecayShared.Blended("M_Day3_Dust",
            new Color(0.42f, 0.40f, 0.36f, 0.9f), stain, false);

        m.wetFloor = LobbyDecayShared.Blended("M_Day4_WetFloor",
            new Color(0.03f, 0.03f, 0.03f, 0.75f), stain, false);

        m.damp = LobbyDecayShared.Blended("M_Day4_Damp",
            new Color(0.075f, 0.07f, 0.055f, 0.95f), stain, true);

        m.streak = LobbyDecayShared.Blended("M_Day4_Streak",
            new Color(0.07f, 0.065f, 0.045f, 0.95f),
            LobbyDecayShared.StreakTexture(), true);

        m.web = LobbyDecayShared.Blended("M_Day4_Web",
            new Color(0.80f, 0.80f, 0.77f, 0.7f),
            LobbyDecayShared.WebTexture(), true);

        m.peel = LobbyDecayShared.Opaque("M_Day4_Peel",
            new Color(0.40f, 0.37f, 0.33f), 0.05f, true);

        LobbyDecayShared.MakeCutout(m.peel,
            new Color(0.46f, 0.43f, 0.38f),
            LobbyDecayShared.PeelTexture(), 0.05f);

        AssetDatabase.SaveAssets();

        return m;
    }


    // =========================================================
    // WHERE
    // =========================================================

    private static bool FindDay3Site(GameObject lobby, out Vector3 site)
    {
        site = Vector3.zero;

        Transform scar = LobbyDay2DecayBuilder.FindDeep(
            lobby.transform, "D3_ScarExposed");

        if (scar == null)
        {
            return false;
        }

        site = scar.position;

        return true;
    }


    private static bool FarFromAll(
        Vector3 p,
        List<Vector3> avoid,
        float distance)
    {
        foreach (Vector3 a in avoid)
        {
            Vector2 d = new Vector2(a.x - p.x, a.z - p.z);

            if (d.magnitude < distance)
            {
                return false;
            }
        }

        return true;
    }


    /// <summary>
    /// The main collapse needs: a real ceiling at least 2.6 m up, open
    /// floor under the pile AND under where the beam lands, the whole
    /// footprint well clear of everything the player must reach, and
    /// distance from Day 3's fall.
    /// </summary>
    private static bool FindMainSite(
        Vector3 anchor,
        float floorY,
        List<Bounds> keepClear,
        List<Vector3> avoid,
        out Vector3 ceiling,
        out Vector3 beamTop,
        out Vector3 foot)
    {
        ceiling = beamTop = foot = Vector3.zero;

        Vector3 eye = new Vector3(anchor.x, floorY + 1.6f, anchor.z);

        for (int i = 0; i < 1500; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);

            float radius = Random.Range(SiteMinRadius, SiteMaxRadius);

            Vector3 probe = new Vector3(
                anchor.x + Mathf.Cos(angle) * radius,
                floorY + 0.5f,
                anchor.z + Mathf.Sin(angle) * radius);

            if (!FarFromAll(probe, avoid, MinDistanceFromDay3))
            {
                continue;
            }

            if (!LobbyDecayShared.SameRoom(eye, probe + Vector3.up * 1.1f))
            {
                continue;
            }

            RaycastHit up;

            if (!LobbyDecayShared.CeilingAbove(probe, 20f, out up))
            {
                continue;
            }

            float height = up.point.y - floorY;

            if (height < 2.6f || height > 9f)
            {
                continue;
            }

            if (!LobbyDecayShared.OpenFloor(probe, floorY, 1.4f))
            {
                continue;
            }

            // The beam comes down steeply (about 60 degrees) so it can
            // never be walked up like a ramp.
            float a2 = Random.Range(0f, Mathf.PI * 2f);

            Vector3 dir = new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2));

            Vector3 top = up.point + dir * 1.0f - Vector3.up * 0.05f;

            RaycastHit topCheck;

            if (!LobbyDecayShared.CeilingAbove(
                    top - Vector3.up * 0.5f, 1.0f, out topCheck))
            {
                continue;
            }

            float run = height * 0.58f;

            Vector3 footProbe = new Vector3(
                top.x - dir.x * run, floorY + 0.5f, top.z - dir.z * run);

            if (!LobbyDecayShared.OpenFloor(footProbe, floorY, 0.8f))
            {
                continue;
            }

            Vector3 mid = (probe + footProbe) * 0.5f;

            if (!LobbyDecayShared.IsClear(probe, BlockingClearance, keepClear) ||
                !LobbyDecayShared.IsClear(footProbe, BlockingClearance, keepClear) ||
                !LobbyDecayShared.IsClear(mid, BlockingClearance, keepClear))
            {
                continue;
            }

            // Nothing in the way of the beam itself.
            RaycastHit block;

            Vector3 footPoint = new Vector3(footProbe.x, floorY + 0.15f, footProbe.z);

            if (LobbyDecayShared.Cast(top - Vector3.up * 0.3f,
                    footPoint - (top - Vector3.up * 0.3f),
                    Vector3.Distance(top - Vector3.up * 0.3f, footPoint) - 0.1f,
                    out block))
            {
                continue;
            }

            ceiling = up.point;

            beamTop = new Vector3(top.x, topCheck.point.y + 0.05f, top.z);

            foot = new Vector3(footProbe.x, floorY, footProbe.z);

            return true;
        }

        return false;
    }


    private static bool FindSmallSite(
        Vector3 anchor,
        float floorY,
        List<Bounds> keepClear,
        List<Vector3> avoid,
        out Vector3 ceiling,
        out Vector3 floor)
    {
        ceiling = floor = Vector3.zero;

        Vector3 eye = new Vector3(anchor.x, floorY + 1.6f, anchor.z);

        for (int i = 0; i < 900; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);

            float radius = Random.Range(2.0f, SiteMaxRadius);

            Vector3 probe = new Vector3(
                anchor.x + Mathf.Cos(angle) * radius,
                floorY + 0.5f,
                anchor.z + Mathf.Sin(angle) * radius);

            if (!FarFromAll(probe, avoid, 3.5f) ||
                !LobbyDecayShared.SameRoom(eye, probe + Vector3.up * 1.1f) ||
                !LobbyDecayShared.IsClear(probe, 1.2f, keepClear) ||
                !LobbyDecayShared.OpenFloor(probe, floorY, 0.7f))
            {
                continue;
            }

            RaycastHit up;

            if (!LobbyDecayShared.CeilingAbove(probe, 20f, out up) ||
                up.point.y - floorY < 2.3f)
            {
                continue;
            }

            ceiling = up.point;

            floor = new Vector3(probe.x, floorY, probe.z);

            return true;
        }

        return false;
    }


    // =========================================================
    // THE MAIN COLLAPSE
    // =========================================================

    private static int BuildMainCollapse(
        Transform root,
        Vector3 ceiling,
        Vector3 beamTop,
        Vector3 foot,
        float floorY,
        Mats mats)
    {
        GameObject group = LobbyDecayShared.Group(root, Prefix, "MajorCollapse");

        Transform g = group.transform;

        int made = 0;

        Vector3 floorCentre = new Vector3(ceiling.x, floorY, ceiling.z);

        Vector3 run = foot - new Vector3(beamTop.x, floorY, beamTop.z);

        Vector3 dir = run.sqrMagnitude > 0.01f ? run.normalized : Vector3.forward;

        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // ---- the opening ----------------------------------------

        float[] radii = LobbyDecayShared.RaggedRadii(15, 0.36f, 0.66f);

        Mesh scar = LobbyDecayShared.SaveMesh("D4_MajorScar",
            LobbyDecayShared.PatchMesh("D4_MajorScar", radii, 1f));

        Mesh rim = LobbyDecayShared.SaveMesh("D4_MajorScarRim",
            LobbyDecayShared.PatchMesh("D4_MajorScarRim", radii, 1.2f));

        float w = Random.Range(3.0f, 3.6f);

        float h = w * Random.Range(0.72f, 0.88f);

        // Long axis of the opening runs along the beam.
        Quaternion down = LobbyDecayShared.Facing(Vector3.down, 0f)
            * Quaternion.Euler(0f, 0f, -yaw + 90f);

        LobbyDecayShared.Piece(g, Prefix, "ScarRim", rim,
            ceiling - Vector3.up * 0.008f, down,
            new Vector3(w, h, 1f), mats.rim);

        LobbyDecayShared.Piece(g, Prefix, "ScarExposed", scar,
            ceiling - Vector3.up * 0.016f, down,
            new Vector3(w, h, 1f), mats.exposed);

        made += 2;

        // Joists across the opening - gives it depth from below.
        Vector3 across = Vector3.Cross(Vector3.up, dir).normalized;

        for (int i = -1; i <= 1; i++)
        {
            Vector3 at = ceiling - Vector3.up * 0.075f
                + dir * (i * h * 0.24f);

            LobbyDecayShared.Primitive(g, Prefix, "Joist_" + (i + 1),
                PrimitiveType.Cube, at,
                Quaternion.LookRotation(across, Vector3.up),
                new Vector3(0.11f, 0.12f, w * Random.Range(0.55f, 0.75f)),
                mats.joist);

            made++;
        }

        made += LobbyDay3DecayBuilder.HangingLaths(g, Prefix, ceiling,
            w * 0.5f, h * 0.5f, Random.Range(6, 9), 0.5f, 1.2f, mats.lath);

        // ---- the beam -------------------------------------------

        Vector3 footTop = foot + Vector3.up * 0.12f;

        Vector3 beamDir = (footTop - beamTop).normalized;

        float beamLength = Vector3.Distance(beamTop, footTop) + 0.35f;

        Vector3 beamCentre = beamTop + beamDir * (beamLength * 0.5f - 0.35f);

        LobbyDecayShared.Primitive(g, Prefix, "BrokenBeam",
            PrimitiveType.Cube, beamCentre,
            Quaternion.LookRotation(beamDir, Vector3.up)
                * Quaternion.Euler(0f, 0f, Random.Range(-6f, 6f)),
            new Vector3(0.2f, 0.26f, beamLength), mats.beam);

        made++;

        // Splinters where it snapped.
        for (int i = 0; i < 4; i++)
        {
            Vector3 at = footTop - beamDir * Random.Range(0.05f, 0.25f)
                + Random.insideUnitSphere * 0.08f;

            Vector3 splinterDir = (beamDir + Random.insideUnitSphere * 0.5f).normalized;

            float len = Random.Range(0.18f, 0.45f);

            LobbyDecayShared.Primitive(g, Prefix, "Splinter_" + i,
                PrimitiveType.Cube, at + splinterDir * (len * 0.5f),
                Quaternion.LookRotation(splinterDir, Vector3.up),
                new Vector3(0.025f, 0.02f, len), mats.beam);

            made++;
        }

        // ---- cables hanging out of the ceiling ------------------

        for (int i = 0; i < 3; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);

            Vector3 from = ceiling + new Vector3(
                Mathf.Cos(a) * w * 0.25f, -0.02f, Mathf.Sin(a) * h * 0.25f);

            float len = Random.Range(0.9f, 1.8f);

            Vector3 hang = (Vector3.down + Random.insideUnitSphere * 0.15f).normalized;

            LobbyDecayShared.Primitive(g, Prefix, "Cable_" + i,
                PrimitiveType.Cylinder, from + hang * (len * 0.5f),
                Quaternion.FromToRotation(Vector3.up, -hang),
                new Vector3(0.014f, len * 0.5f, 0.014f), mats.cable);

            made++;

            // The last one still has its fitting on the end.
            if (i == 2)
            {
                LobbyDecayShared.Primitive(g, Prefix, "DeadFitting",
                    PrimitiveType.Cylinder, from + hang * (len + 0.05f),
                    Quaternion.FromToRotation(Vector3.up, -hang),
                    new Vector3(0.16f, 0.05f, 0.16f), mats.cable);

                made++;
            }
        }

        // ---- the pile on the floor ------------------------------

        float dust = Random.Range(4.4f, 5.2f);

        LobbyDecayShared.Primitive(g, Prefix, "FloorDust",
            PrimitiveType.Quad,
            Vector3.Lerp(floorCentre, foot, 0.4f) + Vector3.up * 0.004f,
            LobbyDecayShared.Facing(Vector3.up, Random.Range(0f, 360f)),
            new Vector3(dust, dust * 0.8f, 1f), mats.dust);

        made++;

        Vector3[] slabSizes =
        {
            new Vector3(1.3f, 0.22f, 0.9f),
            new Vector3(0.95f, 0.2f, 0.75f),
            new Vector3(0.7f, 0.18f, 0.5f),
            new Vector3(0.6f, 0.16f, 0.45f)
        };

        for (int i = 0; i < slabSizes.Length; i++)
        {
            Vector3 at = Vector3.Lerp(floorCentre, foot, Random.Range(0f, 0.55f))
                + new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));

            string n = "D4_Slab_" + i;

            Mesh mesh = LobbyDecayShared.SaveMesh(n,
                LobbyDecayShared.ChunkMesh(n, slabSizes[i]));

            // Big pieces land flat; later ones lean on what is there.
            float tilt = i < 2 ? 4f : 22f;

            LobbyDecayShared.Piece(g, Prefix, "Slab_" + i, mesh,
                at + Vector3.up * (slabSizes[i].y * 0.5f + (i < 2 ? 0f : 0.12f)),
                Quaternion.Euler(Random.Range(-tilt, tilt),
                    Random.Range(0f, 360f), Random.Range(-tilt, tilt)),
                Vector3.one, i % 2 == 0 ? mats.plaster : mats.plasterDark);

            made++;
        }

        made += LobbyDecayShared.RubbleAround(g, Prefix, "Shard",
            floorCentre, floorY, 0.4f, 2.4f, 18, 0.08f, 0.34f, mats.plaster);

        made += LobbyDecayShared.RubbleAround(g, Prefix, "Grit",
            floorCentre, floorY, 1.2f, 3.6f, 14, 0.04f, 0.1f, mats.plasterDark);

        // ---- the one thing that blocks --------------------------

        BuildBlocker(g, floorCentre, foot, floorY);

        return made;
    }


    // An invisible box over the pile and the foot of the beam, so the
    // player walks around the collapse instead of through it. Upright,
    // so it can never be climbed. Carves the NavMesh too, in case
    // anything navigates the lobby.
    private static void BuildBlocker(
        Transform parent,
        Vector3 pileCentre,
        Vector3 foot,
        float floorY)
    {
        Vector3 a = new Vector3(pileCentre.x, floorY, pileCentre.z);

        Vector3 b = new Vector3(foot.x, floorY, foot.z);

        Vector3 along = b - a;

        float length = along.magnitude + 1.6f;

        Vector3 dir = along.sqrMagnitude > 0.01f ? along.normalized : Vector3.forward;

        GameObject go = new GameObject(Prefix + "DebrisBlocker");

        go.transform.SetParent(parent, true);

        go.transform.SetPositionAndRotation(
            (a + b) * 0.5f + Vector3.up * 1.1f,
            Quaternion.LookRotation(dir, Vector3.up));

        go.transform.localScale = Vector3.one;

        BoxCollider box = go.AddComponent<BoxCollider>();

        box.size = new Vector3(1.7f, 2.2f, length);

        NavMeshObstacle obstacle = go.AddComponent<NavMeshObstacle>();

        obstacle.shape = NavMeshObstacleShape.Box;

        obstacle.size = box.size;

        obstacle.carving = true;

        go.isStatic = true;

        Undo.RegisterCreatedObjectUndo(go, "Day 4 Decay");
    }


    // =========================================================
    // THE SECOND, SMALLER FALL
    // =========================================================

    private static int BuildSmallFall(
        Transform root,
        Vector3 ceiling,
        Vector3 floor,
        Mats mats)
    {
        GameObject group = LobbyDecayShared.Group(root, Prefix, "SecondFall");

        Transform g = group.transform;

        float[] radii = LobbyDecayShared.RaggedRadii(10, 0.38f, 0.62f);

        Mesh scar = LobbyDecayShared.SaveMesh("D4_SmallScar",
            LobbyDecayShared.PatchMesh("D4_SmallScar", radii, 1f));

        Mesh rim = LobbyDecayShared.SaveMesh("D4_SmallScarRim",
            LobbyDecayShared.PatchMesh("D4_SmallScarRim", radii, 1.25f));

        float w = Random.Range(1.1f, 1.4f);

        Quaternion down = LobbyDecayShared.Facing(
            Vector3.down, Random.Range(0f, 360f));

        LobbyDecayShared.Piece(g, Prefix, "ScarRim", rim,
            ceiling - Vector3.up * 0.008f, down,
            new Vector3(w, w * 0.8f, 1f), mats.rim);

        LobbyDecayShared.Piece(g, Prefix, "ScarExposed", scar,
            ceiling - Vector3.up * 0.016f, down,
            new Vector3(w, w * 0.8f, 1f), mats.exposed);

        int made = 2;

        made += LobbyDay3DecayBuilder.HangingLaths(g, Prefix, ceiling,
            w * 0.5f, w * 0.4f, Random.Range(2, 4), 0.35f, 0.7f, mats.lath);

        float dust = Random.Range(2.0f, 2.6f);

        LobbyDecayShared.Primitive(g, Prefix, "FloorDust",
            PrimitiveType.Quad, floor + Vector3.up * 0.004f,
            LobbyDecayShared.Facing(Vector3.up, Random.Range(0f, 360f)),
            new Vector3(dust, dust * 0.85f, 1f), mats.dust);

        made++;

        made += LobbyDecayShared.RubbleAround(g, Prefix, "SmallFall",
            floor, floor.y, 0.1f, 1.3f, 9, 0.08f, 0.3f, mats.plaster);

        return made;
    }


    // =========================================================
    // WATER
    // =========================================================

    private static int BuildWater(
        Transform root,
        Vector3 anchor,
        Vector3 collapseFloor,
        Vector3 collapseCeiling,
        float floorY,
        Mats mats)
    {
        int made = 0;

        // ---- big damp patches across the ceiling ----------------

        GameObject damp = LobbyDecayShared.Group(root, Prefix, "CeilingDamp");

        for (int attempt = 0; attempt < 60 && made < 8; attempt++)
        {
            Vector3 centre = attempt % 2 == 0 ? collapseCeiling : anchor;

            float a = Random.Range(0f, Mathf.PI * 2f);

            float r = Random.Range(1.0f, 6.0f);

            Vector3 probe = new Vector3(
                centre.x + Mathf.Cos(a) * r,
                floorY + 1.5f,
                centre.z + Mathf.Sin(a) * r);

            RaycastHit hit;

            if (!LobbyDecayShared.CeilingAbove(probe, 15f, out hit))
            {
                continue;
            }

            float s = Random.Range(1.6f, 3.2f);

            LobbyDecayShared.Primitive(damp.transform, Prefix,
                "Damp_" + made, PrimitiveType.Quad,
                hit.point - Vector3.up * (0.022f + made * 0.002f),
                LobbyDecayShared.Facing(Vector3.down, Random.Range(0f, 360f)),
                new Vector3(s, s * Random.Range(0.6f, 1.0f), 1f), mats.damp);

            made++;
        }

        // ---- water running down the walls -----------------------

        GameObject streaks = LobbyDecayShared.Group(root, Prefix, "WaterStreaks");

        int streakCount = 0;

        for (int i = 0; i < 6; i++)
        {
            Vector3 near = i < 3 ? collapseFloor : anchor;

            if (LobbyDay3DecayBuilder.Streak(streaks.transform, Prefix,
                    "Streak_" + i, near, floorY, 7f,
                    Random.Range(0.45f, 0.9f), mats.streak))
            {
                streakCount++;
            }
        }

        made += streakCount;

        // ---- standing water -------------------------------------

        GameObject pools = LobbyDecayShared.Group(root, Prefix, "Flooding");

        int pooled = 0;

        for (int attempt = 0; attempt < 40 && pooled < 3; attempt++)
        {
            Vector3 centre = pooled == 0 ? collapseFloor : anchor;

            float a = Random.Range(0f, Mathf.PI * 2f);

            float r = pooled == 0 ? Random.Range(1.4f, 2.4f) : Random.Range(2.0f, 7.0f);

            Vector3 at = new Vector3(
                centre.x + Mathf.Cos(a) * r, floorY, centre.z + Mathf.Sin(a) * r);

            float size = Random.Range(1.3f, 2.3f);

            if (!LobbyDecayShared.OpenFloor(at, floorY, size * 0.45f))
            {
                continue;
            }

            string n = "D4_Pool_" + pooled;

            Mesh mesh = LobbyDecayShared.SaveMesh(n,
                LobbyDecayShared.PatchMesh(n,
                    LobbyDecayShared.RaggedRadii(18, 0.7f, 1.0f), 1f));

            Quaternion up = LobbyDecayShared.Facing(Vector3.up, Random.Range(0f, 360f));

            // A wet, darkened rim under the pool, the pool on top.
            LobbyDecayShared.Primitive(pools.transform, Prefix,
                "WetEdge_" + pooled, PrimitiveType.Quad,
                at + Vector3.up * 0.006f, up,
                new Vector3(size * 1.5f, size * 1.2f, 1f), mats.wetFloor);

            LobbyDecayShared.Piece(pools.transform, Prefix, "Pool_" + pooled,
                mesh, at + Vector3.up * 0.01f, up,
                new Vector3(size * 0.5f, size * 0.38f, 1f), mats.puddle);

            pooled++;
        }

        made += pooled * 2;

        return made;
    }


    // =========================================================
    // WALLS
    // =========================================================

    private static int BuildWalls(
        Transform root,
        Vector3 anchor,
        float floorY,
        Mats mats)
    {
        int made = 0;

        // ---- long, branching cracks -----------------------------

        GameObject cracks = LobbyDecayShared.Group(root, Prefix, "WallCracks");

        for (int i = 0; i < 9; i++)
        {
            RaycastHit hit;

            float length = Random.Range(1.4f, 3.0f);

            // Keep the whole (centred) crack above the skirting.
            float low = Mathf.Max(1.0f, length * 0.5f + 0.2f);

            if (!LobbyDecayShared.FindWall(anchor, floorY, 10f,
                    low, Mathf.Max(low + 0.2f, 2.8f),
                    length * 0.35f, length * 0.5f, out hit))
            {
                continue;
            }

            string n = "D4_Crack_" + i;

            Mesh mesh = LobbyDecayShared.SaveMesh(n,
                LobbyDecayShared.CrackMesh(n, length,
                    Random.Range(0.03f, 0.05f), Random.Range(2, 5)));

            LobbyDecayShared.Piece(cracks.transform, Prefix, "Crack_" + i,
                mesh, hit.point + hit.normal * 0.009f,
                LobbyDecayShared.Facing(hit.normal, Random.Range(-50f, 50f)),
                Vector3.one, mats.crack);

            made++;
        }

        // ---- plaster burst off the wall -------------------------

        GameObject breaches = LobbyDecayShared.Group(root, Prefix, "WallBreaches");

        for (int i = 0; i < 2; i++)
        {
            RaycastHit hit;

            float s = Random.Range(0.8f, 1.4f);

            if (!LobbyDecayShared.FindWall(anchor, floorY, 10f,
                    0.7f, 1.5f, s * 0.65f, s * 0.6f, out hit))
            {
                continue;
            }

            float[] radii = LobbyDecayShared.RaggedRadii(12, 0.34f, 0.62f);

            string n = "D4_Breach_" + i;

            Mesh hole = LobbyDecayShared.SaveMesh(n,
                LobbyDecayShared.PatchMesh(n, radii, 1f));

            Mesh edge = LobbyDecayShared.SaveMesh(n + "_Rim",
                LobbyDecayShared.PatchMesh(n + "_Rim", radii, 1.22f));

            Quaternion facing = LobbyDecayShared.Facing(hit.normal, Random.Range(0f, 360f));

            LobbyDecayShared.Piece(breaches.transform, Prefix, "BreachRim_" + i,
                edge, hit.point + hit.normal * 0.008f, facing,
                new Vector3(s, s * 0.85f, 1f), mats.rim);

            LobbyDecayShared.Piece(breaches.transform, Prefix, "BreachBrick_" + i,
                hole, hit.point + hit.normal * 0.016f, facing,
                new Vector3(s, s * 0.85f, 1f), mats.brick);

            made += 2;

            // What fell out of it lies at the foot of the wall.
            Vector3 foot = new Vector3(hit.point.x, floorY, hit.point.z)
                + hit.normal * 0.45f;

            made += LobbyDecayShared.RubbleAround(breaches.transform, Prefix,
                "BreachRubble_" + i, foot, floorY, 0.0f, 0.6f, 7,
                0.06f, 0.22f, mats.plaster);
        }

        // ---- heavy peeling --------------------------------------

        GameObject peel = LobbyDecayShared.Group(root, Prefix, "HeavyPeel");

        for (int i = 0; i < 7; i++)
        {
            RaycastHit hit;

            float s = Random.Range(1.0f, 2.0f);

            if (!LobbyDecayShared.FindWall(anchor, floorY, 10f,
                    0.8f, 2.4f, s * 0.4f, s * 0.4f, out hit))
            {
                continue;
            }

            LobbyDecayShared.Primitive(peel.transform, Prefix, "Peel_" + i,
                PrimitiveType.Quad, hit.point + hit.normal * (0.02f + i * 0.001f),
                LobbyDecayShared.Facing(hit.normal, Random.Range(0f, 360f)),
                new Vector3(s, s * Random.Range(0.7f, 1.2f), 1f), mats.peel);

            made++;
        }

        return made;
    }


    // =========================================================
    // COBWEBS
    // =========================================================

    private static int BuildWebs(
        Transform root,
        Vector3 anchor,
        float floorY,
        Mats mats)
    {
        GameObject webs = LobbyDecayShared.Group(root, Prefix, "Cobwebs");

        int made = 0;

        for (int i = 0; i < 10; i++)
        {
            RaycastHit hit;

            float s = Random.Range(1.4f, 2.4f);

            if (!LobbyDecayShared.FindWall(anchor, floorY, 11f,
                    1.9f, 2.9f, s * 0.3f, s * 0.3f, out hit))
            {
                continue;
            }

            LobbyDecayShared.Primitive(webs.transform, Prefix, "Web_" + i,
                PrimitiveType.Quad, hit.point + hit.normal * (0.035f + i * 0.002f),
                LobbyDecayShared.Facing(hit.normal, Random.Range(0f, 360f)),
                new Vector3(s, s, 1f), mats.web);

            made++;
        }

        // Sheets hanging from the ceiling just off the walls.
        for (int i = 0; i < 4; i++)
        {
            RaycastHit wall;

            if (!LobbyDecayShared.FindWall(anchor, floorY, 10f,
                    1.8f, 2.6f, 0.4f, 0.3f, out wall))
            {
                continue;
            }

            Vector3 under = wall.point + wall.normal * Random.Range(0.3f, 0.7f);

            RaycastHit ceiling;

            if (!LobbyDecayShared.CeilingAbove(under, 10f, out ceiling))
            {
                continue;
            }

            float tall = Mathf.Min(Random.Range(1.0f, 1.7f),
                (ceiling.point.y - floorY) - 2.1f);

            if (tall < 0.5f)
            {
                continue;
            }

            float wide = Random.Range(0.8f, 1.4f);

            Vector3 across = Vector3.Cross(Vector3.up, wall.normal).normalized;

            Quaternion turn = Quaternion.AngleAxis(Random.Range(-25f, 25f), Vector3.up);

            LobbyDecayShared.Primitive(webs.transform, Prefix, "Sheet_" + i,
                PrimitiveType.Quad,
                ceiling.point - Vector3.up * (tall * 0.5f + 0.01f),
                LobbyDecayShared.Facing(turn * wall.normal, 0f),
                new Vector3(wide, tall, 1f), mats.web);

            made++;
        }

        return made;
    }


    // =========================================================
    // LIGHTS
    // =========================================================

    // Everything dimmer again (stacks with Day 3: about 60% overall),
    // the light over the collapse is dead, two more stutter hard.
    private static int BuildLights(
        Transform root,
        GameObject lobby,
        Bounds room,
        Vector3 anchor,
        Vector3 collapse)
    {
        List<Light> lights = LobbyDecayShared.LobbyLights(lobby, room);

        if (lights.Count == 0)
        {
            Debug.LogWarning(Tag + " No realtime/mixed lights found in "
                + "the lobby - skipping the light decay.");

            return 0;
        }

        HashSet<Light> taken = new HashSet<Light>();

        // Leave Day 3's stuttering light to Day 3.
        Light day3Stutter = Day3Stutter(lobby);

        if (day3Stutter != null)
        {
            taken.Add(day3Stutter);
        }

        Light dead = null;

        // Never kill the only light, or the lobby goes black.
        if (lights.Count > 2)
        {
            dead = LobbyDecayShared.Nearest(lights, collapse, taken);

            if (dead != null)
            {
                taken.Add(dead);
            }
        }

        Light stutterA = LobbyDecayShared.Nearest(lights, anchor, taken);

        if (stutterA != null)
        {
            taken.Add(stutterA);
        }

        Light stutterB = LobbyDecayShared.Nearest(lights, anchor, taken);

        List<LobbyLightDecay.Entry> entries = new List<LobbyLightDecay.Entry>();

        foreach (Light l in lights)
        {
            LobbyLightDecay.Entry e = new LobbyLightDecay.Entry();

            e.light = l;

            e.intensity = l == dead ? 0f : 0.7f;

            if (l == stutterA || l == stutterB)
            {
                e.flicker = true;

                e.flickerDepth = 0.9f;

                e.flickerRate = l == stutterA ? 1.6f : 1.25f;
            }

            entries.Add(e);
        }

        LobbyDecayShared.WireLights(root, Prefix, entries);

        Debug.Log(Tag + " Lights: " + lights.Count + " dimmed again"
            + (dead != null ? ", '" + dead.name + "' dead" : "")
            + (stutterA != null ? ", '" + stutterA.name + "'" : "")
            + (stutterB != null ? " and '" + stutterB.name + "'" : "")
            + " stutter.");

        return lights.Count;
    }


    private static Light Day3Stutter(GameObject lobby)
    {
        Transform t = LobbyDay2DecayBuilder.FindDeep(
            lobby.transform, "D3_LightDecay");

        if (t == null)
        {
            return null;
        }

        LobbyLightDecay decay = t.GetComponent<LobbyLightDecay>();

        if (decay == null)
        {
            return null;
        }

        SerializedObject so = new SerializedObject(decay);

        SerializedProperty list = so.FindProperty("lights");

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(i);

            if (e.FindPropertyRelative("flicker").boolValue)
            {
                return e.FindPropertyRelative("light").objectReferenceValue as Light;
            }
        }

        return null;
    }
}
#endif
