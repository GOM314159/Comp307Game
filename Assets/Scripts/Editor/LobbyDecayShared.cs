#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Shared machinery for the lobby decay builders (Days 2, 3 and 4):
/// finding the day roots, raycasting against real scenery only, keeping
/// clear of portals and doors, procedural meshes and textures, materials,
/// failing lights and the darker post-processing for later days.
///
/// Also owns the one-click "Build All Lobby Decay" menu and the batch
/// entry point that builds every day and saves the scene.
/// </summary>
internal static class LobbyDecayShared
{
    internal const string LobbyScenePath =
        "Assets/Scenes/GamePlayTest.unity";

    internal const string MaterialFolder = "Assets/Materials/DayDecay";
    internal const string MeshFolder = MaterialFolder + "/Meshes";
    internal const string TextureFolder = MaterialFolder + "/Textures";
    internal const string ProfileFolder = MaterialFolder + "/Profiles";


    // =========================================================
    // BUILD EVERYTHING
    // =========================================================

    [MenuItem("Tools/Strings and Statutes/Lobby/Build All Lobby Decay (Days 2-4)")]
    public static void BuildAll()
    {
        LobbyDay2DecayBuilder.Build();

        bool d3 = LobbyDay3DecayBuilder.BuildCore(true);

        bool d4 = d3 && LobbyDay4DecayBuilder.BuildCore(true);

        Debug.Log("[LobbyDecay] Built all days. Day 3 "
            + (d3 ? "ok" : "FAILED") + ", Day 4 "
            + (d4 ? "ok" : "FAILED") + ". Save the scene to keep it.");
    }


    // Command-line entry point. Opens the lobby scene, builds Days 2-4
    // and the city backdrop, and SAVES the scene and every asset.
    //   Unity.exe -batchmode -quit -projectPath <repo>
    //       -executeMethod LobbyDecayShared.BuildAllAndSaveBatch
    // The Unity editor must be closed on this project first.
    public static void BuildAllAndSaveBatch()
    {
        EditorSceneManager.OpenScene(LobbyScenePath);

        LobbyDay2DecayBuilder.Build();

        bool d3 = LobbyDay3DecayBuilder.BuildCore(false);

        bool d4 = d3 && LobbyDay4DecayBuilder.BuildCore(false);

        bool city = LobbyCityBackdropBuilder.BuildCore(false);

        if (!d3 || !d4 || !city)
        {
            Debug.LogError("[LobbyDecay] Batch build failed (Day 3 "
                + d3 + ", Day 4 " + d4 + ", city " + city
                + ") - scene NOT saved. See the messages above.");

            EditorApplication.Exit(1);

            return;
        }

        EditorSceneManager.SaveOpenScenes();

        AssetDatabase.SaveAssets();

        Debug.Log("[LobbyDecay] Saved " + LobbyScenePath
            + " with Days 2-4 and the city backdrop.");
    }


    // =========================================================
    // ROOTS
    // =========================================================

    /// <summary>
    /// The DayNChanges roots now live under Lobby/Changes (GameFlowManager
    /// finds them by name anywhere in the scene). Search the whole lobby,
    /// and only create one if it is genuinely missing.
    /// </summary>
    internal static Transform FindOrCreateDayRoot(
        GameObject lobby,
        string rootName,
        string logTag)
    {
        Transform root =
            LobbyDay2DecayBuilder.FindDeep(lobby.transform, rootName);

        if (root != null)
        {
            return root;
        }

        Transform parent =
            LobbyDay2DecayBuilder.FindDeep(lobby.transform, "Changes");

        if (parent == null)
        {
            parent = lobby.transform;
        }

        GameObject made = new GameObject(rootName);

        made.transform.SetParent(parent, false);

        Undo.RegisterCreatedObjectUndo(made, "Lobby Decay");

        Debug.Log(logTag + " Lobby had no " + rootName
            + ", so one was created under " + parent.name + ".");

        return made.transform;
    }


    internal static void ClearGenerated(Transform root, string prefix)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);

            if (child.name.StartsWith(prefix))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }


    internal static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "DayDecay");
        }

        if (!AssetDatabase.IsValidFolder(MeshFolder))
        {
            AssetDatabase.CreateFolder(MaterialFolder, "Meshes");
        }

        if (!AssetDatabase.IsValidFolder(TextureFolder))
        {
            AssetDatabase.CreateFolder(MaterialFolder, "Textures");
        }

        if (!AssetDatabase.IsValidFolder(ProfileFolder))
        {
            AssetDatabase.CreateFolder(MaterialFolder, "Profiles");
        }
    }


    // =========================================================
    // RAYCASTING - REAL SCENERY ONLY
    // =========================================================

    /// <summary>
    /// Like Physics.Raycast, but only ever lands on the theatre itself.
    /// Ignores the player, the day roots (the Level blocks other people
    /// keep in them), portals, doors, jumpscares, paintings and the city.
    /// </summary>
    internal static bool Cast(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out RaycastHit hit)
    {
        hit = new RaycastHit();

        if (direction.sqrMagnitude < 1e-8f || distance <= 0f)
        {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction.normalized,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;

        bool found = false;

        foreach (RaycastHit h in hits)
        {
            if (h.distance < best && IsScenery(h.collider))
            {
                best = h.distance;

                hit = h;

                found = true;
            }
        }

        return found;
    }


    internal static bool IsScenery(Collider c)
    {
        if (c == null)
        {
            return false;
        }

        if (c.GetComponentInParent<CharacterController>() != null)
        {
            return false;
        }

        if (c.attachedRigidbody != null &&
            !c.attachedRigidbody.isKinematic)
        {
            return false;
        }

        return !IsInExcludedBranch(c.transform);
    }


    internal static bool IsInExcludedBranch(Transform t)
    {
        while (t != null)
        {
            string n = t.name;

            if (n.EndsWith("Changes") ||
                n.StartsWith("LobbyCity") ||
                n == "Jumpscares" ||
                n == "LevelPortals" ||
                n.Contains("Portal") ||
                n.Contains("Door") ||
                n.StartsWith("Painting") ||
                n == "Player" ||
                t.CompareTag("Player"))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }


    // Where the player first stands - the decay is staged around it.
    internal static Vector3 Anchor(GameObject lobby, Bounds room)
    {
        Transform spawn = LobbyDay2DecayBuilder.FindDeep(
            lobby.transform, "PlayerSpawnPoint");

        if (spawn != null)
        {
            return spawn.position;
        }

        return new Vector3(room.center.x, room.min.y, room.center.z);
    }


    internal static float FloorHeight(Vector3 anchor)
    {
        RaycastHit hit;

        if (Cast(anchor + Vector3.up * 0.6f, Vector3.down, 4f, out hit))
        {
            return hit.point.y;
        }

        return anchor.y;
    }


    // A straight, unobstructed line from the player's eye - so the
    // point is in the same room, not behind a wall.
    internal static bool SameRoom(Vector3 eye, Vector3 point)
    {
        Vector3 d = point - eye;

        float dist = d.magnitude;

        if (dist < 0.05f)
        {
            return true;
        }

        RaycastHit hit;

        return !Cast(eye, d, dist - 0.05f, out hit);
    }


    // Real floor (not a seat or a stage edge) directly below a point.
    internal static bool FloorBelow(
        Vector3 from,
        float floorY,
        out RaycastHit hit)
    {
        if (!Cast(from, Vector3.down, (from.y - floorY) + 1.5f, out hit))
        {
            return false;
        }

        return hit.normal.y > 0.85f &&
               Mathf.Abs(hit.point.y - floorY) < 0.35f;
    }


    // A real, downward-facing ceiling directly above a point.
    internal static bool CeilingAbove(
        Vector3 from,
        float maxDistance,
        out RaycastHit hit)
    {
        if (!Cast(from, Vector3.up, maxDistance, out hit))
        {
            return false;
        }

        return Vector3.Dot(hit.normal, Vector3.down) > 0.85f;
    }


    /// <summary>
    /// A patch of flat floor: the centre and every point on a ring
    /// around it land on floor at the same height. Rejects the edges
    /// of steps, rugs over drops, and the feet of seats.
    /// </summary>
    internal static bool OpenFloor(
        Vector3 centre,
        float floorY,
        float radius)
    {
        RaycastHit hit;

        if (!FloorBelow(centre + Vector3.up * 1.2f, floorY, out hit))
        {
            return false;
        }

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;

            Vector3 p = centre + new Vector3(
                Mathf.Cos(a) * radius, 1.2f, Mathf.Sin(a) * radius);

            RaycastHit ring;

            if (!FloorBelow(p, floorY, out ring) ||
                Mathf.Abs(ring.point.y - hit.point.y) > 0.08f)
            {
                return false;
            }
        }

        return true;
    }


    /// <summary>
    /// A flat stretch of wall the player can see: a ray from somewhere
    /// near the anchor hits a vertical surface, and rays either side of
    /// the hit (halfWidth across, halfHeight up and down) hit the same
    /// plane - so a stain or crack never hangs off a corner or a doorway.
    /// </summary>
    internal static bool FindWall(
        Vector3 anchor,
        float floorY,
        float reach,
        float heightLow,
        float heightHigh,
        float halfWidth,
        float halfHeight,
        out RaycastHit hit)
    {
        Vector3 eye = new Vector3(anchor.x, floorY + 1.6f, anchor.z);

        for (int i = 0; i < 120; i++)
        {
            Vector3 origin = new Vector3(
                anchor.x + Random.Range(-reach, reach),
                floorY + Random.Range(heightLow, heightHigh),
                anchor.z + Random.Range(-reach, reach));

            if (!SameRoom(eye, origin))
            {
                continue;
            }

            float a = Random.Range(0f, Mathf.PI * 2f);

            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));

            if (!Cast(origin, dir, 30f, out hit))
            {
                continue;
            }

            if (Mathf.Abs(hit.normal.y) > 0.25f || hit.distance < 0.4f)
            {
                continue;
            }

            // The wall must stand on the same floor as the player - not
            // rise from a stage or a raised dais - so "1.5 m up" really
            // is 1.5 m above the floor in front of it.
            RaycastHit below;

            if (!Cast(hit.point + hit.normal * 0.25f, Vector3.down,
                    (hit.point.y - floorY) + 1.0f, out below) ||
                Mathf.Abs(below.point.y - floorY) > 0.3f)
            {
                continue;
            }

            if (IsFlat(hit, halfWidth, halfHeight))
            {
                return true;
            }
        }

        hit = new RaycastHit();

        return false;
    }


    internal static bool IsFlat(
        RaycastHit hit,
        float halfWidth,
        float halfHeight)
    {
        Vector3 n = hit.normal;

        Vector3 across = Vector3.Cross(Vector3.up, n).normalized;

        Vector3[] offsets =
        {
            across * halfWidth,
            -across * halfWidth,
            Vector3.up * halfHeight,
            Vector3.down * halfHeight
        };

        foreach (Vector3 o in offsets)
        {
            RaycastHit probe;

            if (!Cast(hit.point + n * 0.3f + o, -n, 0.6f, out probe))
            {
                return false;
            }

            if (Mathf.Abs(probe.distance - 0.3f) > 0.05f ||
                Vector3.Dot(probe.normal, n) < 0.96f)
            {
                return false;
            }
        }

        return true;
    }


    // =========================================================
    // KEEP-CLEAR ZONES
    // =========================================================

    /// <summary>
    /// Everything the player has to be able to reach or use: spawns,
    /// portals, doors, jumpscare triggers. Debris that blocks must stay
    /// well away from all of them.
    /// </summary>
    internal static List<Bounds> KeepClearZones(GameObject lobby)
    {
        List<Bounds> zones = new List<Bounds>();

        foreach (GameObject root in lobby.scene.GetRootGameObjects())
        {
            foreach (Transform t in
                     root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;

                bool important =
                    n.Contains("Portal") ||
                    n.Contains("Door") ||
                    n.Contains("Spawn") ||
                    n.Contains("Jumpscare") ||
                    n == "TriggerVolume";

                if (!important)
                {
                    continue;
                }

                zones.Add(BoundsOf(t));
            }
        }

        return zones;
    }


    private static Bounds BoundsOf(Transform t)
    {
        Bounds b = new Bounds(t.position, Vector3.one * 1.0f);

        foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
        {
            b.Encapsulate(r.bounds);
        }

        foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
        {
            b.Encapsulate(c.bounds);
        }

        return b;
    }


    // Horizontal distance from the point to every zone is at least
    // `clearance`. Zones bigger than a room (a whole-building prefab
    // that happens to be called "...Door") are ignored.
    internal static bool IsClear(
        Vector3 point,
        float clearance,
        List<Bounds> zones)
    {
        foreach (Bounds b in zones)
        {
            if (b.size.x > 25f || b.size.z > 25f)
            {
                continue;
            }

            Vector3 closest = b.ClosestPoint(point);

            Vector2 d = new Vector2(
                closest.x - point.x, closest.z - point.z);

            if (d.magnitude < clearance)
            {
                return false;
            }
        }

        return true;
    }


    // =========================================================
    // ORIENTATION
    // =========================================================

    /// <summary>
    /// Rotation for anything built facing -Z (a Quad, or the meshes
    /// below) so its visible face points along `normal` - out of a
    /// wall, down from a ceiling, up from a floor.
    /// </summary>
    internal static Quaternion Facing(Vector3 normal, float roll)
    {
        Vector3 hint = Mathf.Abs(normal.y) > 0.9f
            ? Vector3.forward
            : Vector3.up;

        return Quaternion.LookRotation(-normal, hint)
            * Quaternion.Euler(0f, 0f, roll);
    }


    // =========================================================
    // OBJECTS
    // =========================================================

    internal static GameObject Group(
        Transform parent,
        string prefix,
        string name)
    {
        GameObject go = new GameObject(prefix + name);

        go.transform.SetParent(parent, false);

        go.transform.localPosition = Vector3.zero;

        go.transform.localRotation = Quaternion.identity;

        go.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(go, "Lobby Decay");

        return go;
    }


    internal static GameObject Piece(
        Transform parent,
        string prefix,
        string name,
        Mesh mesh,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material)
    {
        GameObject go = new GameObject(prefix + name);

        go.transform.SetParent(parent, true);

        go.transform.SetPositionAndRotation(position, rotation);

        go.transform.localScale = scale;

        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer r = go.AddComponent<MeshRenderer>();

        r.sharedMaterial = material;

        Dress(r);

        go.isStatic = true;

        Undo.RegisterCreatedObjectUndo(go, "Lobby Decay");

        return go;
    }


    internal static GameObject Primitive(
        Transform parent,
        string prefix,
        string name,
        PrimitiveType shape,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(shape);

        go.name = prefix + name;

        Collider c = go.GetComponent<Collider>();

        if (c != null)
        {
            // Dressing must never get in the player's way.
            Object.DestroyImmediate(c);
        }

        go.transform.SetParent(parent, true);

        go.transform.SetPositionAndRotation(position, rotation);

        go.transform.localScale = scale;

        Renderer r = go.GetComponent<Renderer>();

        r.sharedMaterial = material;

        Dress(r);

        go.isStatic = true;

        Undo.RegisterCreatedObjectUndo(go, "Lobby Decay");

        return go;
    }


    // No shadows cast or received: this is surface dressing, and the
    // lobby's lights are already near their shadow budget.
    internal static void Dress(Renderer r)
    {
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        r.receiveShadows = false;
    }


    // Scatter of small broken plaster pieces on open floor around a
    // point. Returns how many landed.
    internal static int RubbleAround(
        Transform parent,
        string prefix,
        string name,
        Vector3 centre,
        float floorY,
        float innerRadius,
        float outerRadius,
        int count,
        float minSize,
        float maxSize,
        Material material)
    {
        int made = 0;

        for (int attempt = 0; attempt < count * 6 && made < count; attempt++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);

            float r = Mathf.Lerp(innerRadius, outerRadius,
                Mathf.Pow(Random.value, 1.6f));

            Vector3 above = centre + new Vector3(
                Mathf.Cos(a) * r, 1.2f, Mathf.Sin(a) * r);

            above.y = floorY + 1.2f;

            RaycastHit hit;

            if (!FloorBelow(above, floorY, out hit))
            {
                continue;
            }

            float s = Random.Range(minSize, maxSize);

            string meshName = prefix + name + "_" + made;

            Mesh mesh = SaveMesh(meshName, ChunkMesh(meshName,
                new Vector3(s, s * Random.Range(0.35f, 0.6f),
                            s * Random.Range(0.6f, 1.1f))));

            Piece(parent, prefix, name + "_" + made, mesh,
                hit.point + Vector3.up * (s * 0.18f),
                Quaternion.Euler(
                    Random.Range(-12f, 12f),
                    Random.Range(0f, 360f),
                    Random.Range(-12f, 12f)),
                Vector3.one, material);

            made++;
        }

        return made;
    }


    // =========================================================
    // MESHES
    // =========================================================

    internal static float[] RaggedRadii(int sides, float min, float max)
    {
        float[] radii = new float[sides];

        for (int i = 0; i < sides; i++)
        {
            radii[i] = Random.Range(min, max);
        }

        return radii;
    }


    // A ragged flat polygon in the XY plane, facing -Z like a Quad.
    // radii gives each corner's distance from the centre, so a scar and
    // its rim can share an outline.
    internal static Mesh PatchMesh(string name, float[] radii, float scale)
    {
        int sides = radii.Length;

        Vector3[] v = new Vector3[sides + 1];

        Vector3[] n = new Vector3[sides + 1];

        Vector2[] uv = new Vector2[sides + 1];

        v[0] = Vector3.zero;

        n[0] = Vector3.back;

        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < sides; i++)
        {
            float a = (i / (float)sides) * Mathf.PI * 2f;

            float r = radii[i] * scale;

            v[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);

            n[i + 1] = Vector3.back;

            uv[i + 1] = new Vector2(
                0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r);
        }

        int[] t = new int[sides * 3];

        for (int i = 0; i < sides; i++)
        {
            t[i * 3] = 0;
            t[i * 3 + 1] = ((i + 1) % sides) + 1;
            t[i * 3 + 2] = i + 1;
        }

        Mesh mesh = new Mesh();

        mesh.name = name;

        mesh.vertices = v;

        mesh.normals = n;

        mesh.uv = uv;

        mesh.triangles = t;

        mesh.RecalculateBounds();

        return mesh;
    }


    // A broken slab: a box with every corner pushed around, flat shaded
    // so the faces catch the light like fractured plaster.
    internal static Mesh ChunkMesh(string name, Vector3 size)
    {
        Vector3[] c = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            float x = ((i & 1) == 0) ? -0.5f : 0.5f;

            float y = (((i >> 1) & 1) == 0) ? -0.5f : 0.5f;

            float z = (((i >> 2) & 1) == 0) ? -0.5f : 0.5f;

            c[i] = new Vector3(
                x * size.x * (1f + Random.Range(-0.28f, 0.28f)),
                y * size.y * (1f + Random.Range(-0.35f, 0.35f)),
                z * size.z * (1f + Random.Range(-0.28f, 0.28f)));

            // Top corners wander more - a broken top, a flat underside.
            if (y > 0f)
            {
                c[i].y += Random.Range(0f, size.y * 0.35f);
            }
        }

        int[][] faces =
        {
            new[] { 0, 2, 6, 4 },
            new[] { 1, 5, 7, 3 },
            new[] { 0, 4, 5, 1 },
            new[] { 2, 3, 7, 6 },
            new[] { 0, 1, 3, 2 },
            new[] { 4, 6, 7, 5 }
        };

        List<Vector3> verts = new List<Vector3>();

        List<int> tris = new List<int>();

        List<Vector2> uvs = new List<Vector2>();

        foreach (int[] f in faces)
        {
            Vector3 a = c[f[0]];
            Vector3 b = c[f[1]];
            Vector3 d = c[f[2]];
            Vector3 e = c[f[3]];

            Vector3 centre = (a + b + d + e) * 0.25f;

            // Order the corners so the face points away from the middle
            // of the slab, whichever way round they came.
            bool flip = Vector3.Dot(Vector3.Cross(b - a, d - a), centre) < 0f;

            int s = verts.Count;

            verts.Add(a);
            verts.Add(b);
            verts.Add(d);
            verts.Add(e);

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            if (!flip)
            {
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 3);
            }
            else
            {
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 1);
                tris.Add(s); tris.Add(s + 3); tris.Add(s + 2);
            }
        }

        Mesh mesh = new Mesh();

        mesh.name = name;

        mesh.SetVertices(verts);

        mesh.SetUVs(0, uvs);

        mesh.SetTriangles(tris, 0);

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();

        return mesh;
    }


    /// <summary>
    /// A crack: a jagged, tapering ribbon that wanders from the origin
    /// in roughly one direction, with a few short branches. Flat in the
    /// XY plane facing -Z, 1 unit = 1 metre.
    /// </summary>
    internal static Mesh CrackMesh(
        string name,
        float length,
        float width,
        int branches)
    {
        List<Vector3> verts = new List<Vector3>();

        List<int> tris = new List<int>();

        float heading = Random.Range(-115f, -65f) * Mathf.Deg2Rad;

        List<Vector2> spine = Wander(Vector2.zero, heading, length,
            Random.Range(9, 15), 34f);

        Ribbon(spine, width, verts, tris);

        for (int b = 0; b < branches; b++)
        {
            int at = Random.Range(2, spine.Count - 2);

            Vector2 dir = spine[at + 1] - spine[at];

            float baseHeading = Mathf.Atan2(dir.y, dir.x)
                + (Random.value < 0.5f ? -1f : 1f)
                  * Random.Range(30f, 65f) * Mathf.Deg2Rad;

            List<Vector2> twig = Wander(spine[at], baseHeading,
                length * Random.Range(0.18f, 0.38f),
                Random.Range(4, 7), 30f);

            Ribbon(twig, width * 0.6f, verts, tris);
        }

        // Centre it, so it is placed (and rotated) about its middle and
        // a flatness check of +-length/2 really covers the whole crack.
        Vector3 min = verts[0], max = verts[0];

        foreach (Vector3 v in verts)
        {
            min = Vector3.Min(min, v);

            max = Vector3.Max(max, v);
        }

        Vector3 middle = (min + max) * 0.5f;

        for (int i = 0; i < verts.Count; i++)
        {
            verts[i] -= middle;
        }

        return FinishFlat(name, verts, tris);
    }


    private static List<Vector2> Wander(
        Vector2 start,
        float heading,
        float length,
        int segments,
        float jitterDegrees)
    {
        List<Vector2> points = new List<Vector2>();

        points.Add(start);

        Vector2 p = start;

        float step = length / segments;

        for (int i = 0; i < segments; i++)
        {
            float h = heading + Random.Range(-jitterDegrees, jitterDegrees)
                * Mathf.Deg2Rad;

            p += new Vector2(Mathf.Cos(h), Mathf.Sin(h))
                * step * Random.Range(0.7f, 1.3f);

            points.Add(p);
        }

        return points;
    }


    // Tapers from full width at the start to a hairline at the end.
    private static void Ribbon(
        List<Vector2> spine,
        float width,
        List<Vector3> verts,
        List<int> tris)
    {
        int start = verts.Count;

        for (int i = 0; i < spine.Count; i++)
        {
            Vector2 dir = i < spine.Count - 1
                ? spine[i + 1] - spine[i]
                : spine[i] - spine[i - 1];

            dir.Normalize();

            Vector2 side = new Vector2(-dir.y, dir.x);

            float t = i / (float)(spine.Count - 1);

            float w = width * Mathf.Lerp(1f, 0.12f, t)
                * Random.Range(0.6f, 1.25f) * 0.5f;

            Vector2 left = spine[i] + side * w;

            Vector2 right = spine[i] - side * w;

            verts.Add(new Vector3(left.x, left.y, 0f));

            verts.Add(new Vector3(right.x, right.y, 0f));
        }

        for (int i = 0; i < spine.Count - 1; i++)
        {
            int l0 = start + i * 2;
            int r0 = l0 + 1;
            int l1 = l0 + 2;
            int r1 = l0 + 3;

            AddFacingBack(verts, tris, l0, r0, l1);
            AddFacingBack(verts, tris, r0, r1, l1);
        }
    }


    /// <summary>
    /// A water streak running down a wall: wide where it seeps out at
    /// the top, narrowing as it runs. Top edge at y = 0, runs down to
    /// y = -length. UV v runs 0 at the top to 1 at the bottom.
    /// </summary>
    internal static Mesh StreakMesh(string name, float width, float length)
    {
        List<Vector3> verts = new List<Vector3>();

        List<Vector2> uvs = new List<Vector2>();

        List<int> tris = new List<int>();

        int rows = 6;

        for (int i = 0; i <= rows; i++)
        {
            float t = i / (float)rows;

            float w = width * Mathf.Lerp(1f, 0.55f, t) * 0.5f;

            float y = -length * t;

            verts.Add(new Vector3(-w, y, 0f));
            verts.Add(new Vector3(w, y, 0f));

            uvs.Add(new Vector2(0f, 1f - t));
            uvs.Add(new Vector2(1f, 1f - t));
        }

        for (int i = 0; i < rows; i++)
        {
            int a = i * 2;

            AddFacingBack(verts, tris, a, a + 1, a + 2);
            AddFacingBack(verts, tris, a + 1, a + 3, a + 2);
        }

        Mesh mesh = FinishFlat(name, verts, tris);

        mesh.SetUVs(0, uvs);

        return mesh;
    }


    // Adds a triangle wound so its front face points to -Z, the same
    // way a Unity Quad faces.
    private static void AddFacingBack(
        List<Vector3> verts,
        List<int> tris,
        int a,
        int b,
        int c)
    {
        Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);

        tris.Add(a);

        if (n.z <= 0f)
        {
            tris.Add(b);
            tris.Add(c);
        }
        else
        {
            tris.Add(c);
            tris.Add(b);
        }
    }


    private static Mesh FinishFlat(
        string name,
        List<Vector3> verts,
        List<int> tris)
    {
        Vector3[] normals = new Vector3[verts.Count];

        Vector2[] uvs = new Vector2[verts.Count];

        for (int i = 0; i < verts.Count; i++)
        {
            normals[i] = Vector3.back;

            uvs[i] = new Vector2(verts[i].x, verts[i].y);
        }

        Mesh mesh = new Mesh();

        mesh.name = name;

        mesh.SetVertices(verts);

        mesh.normals = normals;

        mesh.uv = uvs;

        mesh.SetTriangles(tris, 0);

        mesh.RecalculateBounds();

        return mesh;
    }


    // Procedural meshes only live in the scene if they are saved as
    // assets, so write each one out and use the saved copy.
    internal static Mesh SaveMesh(string name, Mesh mesh)
    {
        EnsureFolders();

        string path = MeshFolder + "/" + name + ".asset";

        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);

            Object.DestroyImmediate(mesh);

            EditorUtility.SetDirty(existing);

            return existing;
        }

        AssetDatabase.CreateAsset(mesh, path);

        return mesh;
    }


    // =========================================================
    // MATERIALS
    // =========================================================

    internal static Material Opaque(
        string name,
        Color colour,
        float smoothness,
        bool twoSided)
    {
        EnsureFolders();

        Material m = LobbyDay2DecayBuilder.GetOrCreateOpaque(
            name, colour, smoothness);

        // Flat patches are seen from odd angles; render both faces so a
        // wrong winding can never make one vanish.
        if (twoSided && m.HasProperty("_Cull"))
        {
            m.SetFloat("_Cull", 0f);

            EditorUtility.SetDirty(m);
        }

        return m;
    }


    /// <summary>
    /// Soft, alpha-blended, texture-shaped: stains, dust, cobwebs,
    /// water streaks. Re-applies its settings every build, so it also
    /// upgrades an older flat-colour material of the same name.
    /// </summary>
    internal static Material Blended(
        string name,
        Color colour,
        Texture2D texture,
        bool twoSided)
    {
        EnsureFolders();

        string path = MaterialFolder + "/" + name + ".mat";

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (m == null)
        {
            m = new Material(LobbyDay2DecayBuilder.LitShader());

            AssetDatabase.CreateAsset(m, path);
        }

        MakeBlended(m, colour, texture, twoSided);

        return m;
    }


    internal static void MakeBlended(
        Material m,
        Color colour,
        Texture2D texture,
        bool twoSided)
    {
        LobbyDay2DecayBuilder.SetBaseColour(m, colour);

        SetTexture(m, texture);

        SetIf(m, "_Surface", 1f);
        SetIf(m, "_Blend", 0f);
        SetIf(m, "_AlphaClip", 0f);
        SetIf(m, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetIf(m, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetIf(m, "_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        SetIf(m, "_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetIf(m, "_ZWrite", 0f);
        SetIf(m, "_Smoothness", 0.0f);
        SetIf(m, "_Cull", twoSided ? 0f : 2f);

        m.SetShaderPassEnabled("ShadowCaster", false);

        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        m.DisableKeyword("_ALPHATEST_ON");

        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        m.SetOverrideTag("RenderType", "Transparent");

        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(m);
    }


    /// <summary>
    /// Hard-edged shapes cut out of a texture's alpha: peeling paint.
    /// Opaque queue, so no sorting problems against the walls.
    /// </summary>
    internal static void MakeCutout(
        Material m,
        Color colour,
        Texture2D texture,
        float smoothness)
    {
        LobbyDay2DecayBuilder.SetBaseColour(m, colour);

        SetTexture(m, texture);

        SetIf(m, "_Surface", 0f);
        SetIf(m, "_AlphaClip", 1f);
        SetIf(m, "_Cutoff", 0.5f);
        SetIf(m, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        SetIf(m, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        SetIf(m, "_ZWrite", 1f);
        SetIf(m, "_Smoothness", smoothness);
        SetIf(m, "_Cull", 0f);

        m.SetShaderPassEnabled("ShadowCaster", false);

        m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

        m.EnableKeyword("_ALPHATEST_ON");

        m.SetOverrideTag("RenderType", "TransparentCutout");

        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

        EditorUtility.SetDirty(m);
    }


    private static void SetIf(Material m, string property, float value)
    {
        if (m.HasProperty(property))
        {
            m.SetFloat(property, value);
        }
    }


    private static void SetTexture(Material m, Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        if (m.HasProperty("_BaseMap"))
        {
            m.SetTexture("_BaseMap", texture);
        }

        if (m.HasProperty("_MainTex"))
        {
            m.SetTexture("_MainTex", texture);
        }
    }


    // =========================================================
    // PROCEDURAL TEXTURES
    // =========================================================
    // Generated once into Assets/Materials/DayDecay/Textures and reused.
    // Delete a PNG to have it regenerated. White RGB, shape in alpha -
    // the material colour does the tinting, so one texture serves
    // several materials.

    internal static Texture2D StainTexture()
    {
        return GetTexture("T_Decay_Stain", 256, 11, (x, y, rng) =>
        {
            float dx = x - 0.5f, dy = y - 0.5f;

            float d = Mathf.Sqrt(dx * dx + dy * dy);

            float a = Mathf.Atan2(dy, dx);

            float edge = 0.36f
                + 0.07f * Mathf.PerlinNoise(
                    rng + Mathf.Cos(a) * 1.4f, rng + Mathf.Sin(a) * 1.4f)
                + 0.04f * Mathf.PerlinNoise(rng * 2f + a * 2.2f, 5.1f);

            float body = 1f - Smooth(edge * 0.35f, edge, d);

            float mottle = Fbm(x * 6f + rng, y * 6f - rng);

            // The darker tide-mark where the water stopped spreading.
            float tide = Mathf.Exp(-Mathf.Pow((d - edge * 0.97f) / 0.018f, 2f));

            float alpha = Mathf.Clamp01(body * (0.35f + 0.5f * mottle)
                + tide * 0.55f);

            float shade = 0.82f + 0.18f * mottle;

            return new Color(shade, shade, shade, alpha);
        });
    }


    internal static Texture2D WebTexture()
    {
        return GetTexture("T_Decay_Web", 512, 23, (x, y, rng) =>
        {
            float dx = x - 0.5f, dy = y - 0.5f;

            float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

            if (d > 1f)
            {
                return new Color(1f, 1f, 1f, 0f);
            }

            float a = Mathf.Atan2(dy, dx);

            if (a < 0f)
            {
                a += Mathf.PI * 2f;
            }

            const int spokes = 13;

            float sector = Mathf.PI * 2f / spokes;

            float wobble = 0.12f * (Mathf.PerlinNoise(rng, d * 3f) - 0.5f);

            float local = Mathf.Repeat(a + wobble, sector);

            float frac = local / sector;

            // Distance to the nearest spoke, in texture units.
            float toSpoke = Mathf.Min(local, sector - local) * d * 0.5f;

            float spoke = Mathf.Exp(-Mathf.Pow(toSpoke / 0.0018f, 2f));

            // The spiral: rings that sag between spokes.
            float sag = 1f - 0.11f * Mathf.Sin(frac * Mathf.PI);

            float ringSpace = 0.072f;

            float rr = d * 0.5f / sag;

            float toRing = Mathf.Abs(rr - Mathf.Round(rr / ringSpace) * ringSpace);

            float ring = rr < ringSpace * 0.6f
                ? 0f
                : Mathf.Exp(-Mathf.Pow(toRing / 0.0016f, 2f));

            float threads = Mathf.Max(spoke, ring * 0.85f);

            // Broken, sagging old web: tear out patches and fade to the rim.
            float torn = Mathf.PerlinNoise(x * 7f + rng, y * 7f + rng);

            float keep = Smooth(0.30f, 0.42f, torn);

            float fade = 1f - Smooth(0.7f, 1f, d);

            return new Color(1f, 1f, 1f,
                Mathf.Clamp01(threads * keep * fade));
        });
    }


    internal static Texture2D StreakTexture()
    {
        return GetTexture("T_Decay_Streak", 256, 37, (x, y, rng) =>
        {
            // v = 1 at the top of the streak, 0 at the bottom.
            float down = 1f - y;

            float alpha = 0f;

            // Seep line along the top.
            alpha += 0.55f * Mathf.Exp(-Mathf.Pow(down / 0.08f, 2f))
                * (0.6f + 0.4f * Mathf.PerlinNoise(x * 9f + rng, 1.3f));

            // Several runs of different lengths.
            for (int i = 0; i < 5; i++)
            {
                float cx = 0.15f + 0.7f * Mathf.PerlinNoise(rng + i * 3.7f, 0.5f);

                float len = 0.35f + 0.65f * Mathf.PerlinNoise(rng + i * 1.9f, 7.7f);

                float meander = 0.03f * (Mathf.PerlinNoise(rng + i, down * 4f) - 0.5f);

                float w = 0.045f * (1f - down * 0.6f) + 0.01f;

                float across = Mathf.Exp(-Mathf.Pow((x - cx - meander) / w, 2f));

                float along = down < len
                    ? Mathf.Pow(1f - down / len, 0.6f)
                    : 0f;

                alpha = Mathf.Max(alpha, across * along * 0.8f);
            }

            float mottle = 0.75f + 0.25f * Fbm(x * 5f, y * 9f + rng);

            return new Color(mottle, mottle, mottle, Mathf.Clamp01(alpha));
        });
    }


    internal static Texture2D PeelTexture()
    {
        return GetTexture("T_Decay_Peel", 256, 53, (x, y, rng) =>
        {
            float dx = x - 0.5f, dy = y - 0.5f;

            float d = Mathf.Sqrt(dx * dx + dy * dy);

            float a = Mathf.Atan2(dy, dx);

            float edge = 0.3f
                + 0.12f * Mathf.PerlinNoise(
                    rng + Mathf.Cos(a) * 2.1f, rng + Mathf.Sin(a) * 2.1f)
                + 0.05f * (Mathf.PerlinNoise(rng + a * 6f, 2.2f) - 0.5f);

            if (d > edge)
            {
                return new Color(1f, 1f, 1f, 0f);
            }

            // Lighter curled paint near the edge, darker bare plaster
            // in the middle.
            float t = d / edge;

            float shade = Mathf.Lerp(0.62f, 1f, Smooth(0.55f, 0.92f, t))
                * (0.88f + 0.12f * Fbm(x * 8f + rng, y * 8f));

            return new Color(shade, shade * 0.97f, shade * 0.92f, 1f);
        });
    }


    private static float Smooth(float e0, float e1, float v)
    {
        float t = Mathf.Clamp01((v - e0) / (e1 - e0));

        return t * t * (3f - 2f * t);
    }


    private static float Fbm(float x, float y)
    {
        float sum = 0f, amp = 0.5f, freq = 1f;

        for (int i = 0; i < 4; i++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq, y * freq);

            amp *= 0.5f;

            freq *= 2.03f;
        }

        return sum / 0.9375f;
    }


    private delegate Color Pixel(float x, float y, float seed);


    private static Texture2D GetTexture(
        string name,
        int size,
        int seed,
        Pixel pixel)
    {
        EnsureFolders();

        string path = TextureFolder + "/" + name + ".png";

        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (existing != null)
        {
            return existing;
        }

        float rng = seed * 1.618f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = pixel(
                    (x + 0.5f) / size, (y + 0.5f) / size, rng);
            }
        }

        tex.SetPixels(pixels);

        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());

        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }


    // =========================================================
    // LIGHTS
    // =========================================================

    /// <summary>
    /// Realtime and mixed lights lighting the lobby: not directional,
    /// not fully baked (a baked light cannot be dimmed at runtime), not
    /// inside the day roots, jumpscares or the player.
    /// </summary>
    internal static List<Light> LobbyLights(GameObject lobby, Bounds room)
    {
        List<Light> found = new List<Light>();

        Bounds area = room;

        area.Expand(4f);

        foreach (GameObject root in lobby.scene.GetRootGameObjects())
        {
            foreach (Light l in root.GetComponentsInChildren<Light>(true))
            {
                if (l.type == LightType.Directional)
                {
                    continue;
                }

                if (l.lightmapBakeType == LightmapBakeType.Baked)
                {
                    continue;
                }

                if (!area.Contains(l.transform.position))
                {
                    continue;
                }

                if (IsInExcludedBranch(l.transform) ||
                    l.GetComponentInParent<CharacterController>() != null)
                {
                    continue;
                }

                found.Add(l);
            }
        }

        return found;
    }


    internal static Light Nearest(
        List<Light> lights,
        Vector3 point,
        ICollection<Light> skip)
    {
        Light best = null;

        float bestDist = float.MaxValue;

        foreach (Light l in lights)
        {
            if (skip != null && skip.Contains(l))
            {
                continue;
            }

            float d = (l.transform.position - point).sqrMagnitude;

            if (d < bestDist)
            {
                bestDist = d;

                best = l;
            }
        }

        return best;
    }


    internal static LobbyLightDecay WireLights(
        Transform root,
        string prefix,
        List<LobbyLightDecay.Entry> entries)
    {
        GameObject go = new GameObject(prefix + "LightDecay");

        go.transform.SetParent(root, false);

        Undo.RegisterCreatedObjectUndo(go, "Lobby Decay");

        LobbyLightDecay decay = go.AddComponent<LobbyLightDecay>();

        decay.Configure(entries);

        EditorUtility.SetDirty(decay);

        return decay;
    }


    // =========================================================
    // ATMOSPHERE (POST-PROCESSING)
    // =========================================================

    /// <summary>
    /// A global Volume inside the day root, so it only exists on that day
    /// and later. Global is safe here: levels load as their own scenes,
    /// and the day root lives only in the lobby scene.
    ///
    /// Values are set RELATIVE to the scene's existing global volume
    /// (read at build time), so the lobby gets darker than whatever it
    /// already looks like rather than being reset to a fixed look.
    /// </summary>
    internal static Volume BuildAtmosphere(
        Transform root,
        string prefix,
        string profileName,
        int priority,
        float exposureDelta,
        float saturationDelta,
        float contrastDelta,
        Color filterTint,
        float vignetteDelta)
    {
        EnsureFolders();

        float exposure = 0f, saturation = 0f, contrast = 0f;

        Color filter = Color.white;

        float vignette = 0f, vignetteSmooth = 0.4f;

        bool vignetteSmoothSet = false;

        VolumeProfile baseline = BaselineProfile(root);

        ColorAdjustments baseColour;

        if (baseline != null && baseline.TryGet(out baseColour))
        {
            if (baseColour.postExposure.overrideState)
                exposure = baseColour.postExposure.value;

            if (baseColour.saturation.overrideState)
                saturation = baseColour.saturation.value;

            if (baseColour.contrast.overrideState)
                contrast = baseColour.contrast.value;

            if (baseColour.colorFilter.overrideState)
                filter = baseColour.colorFilter.value;
        }

        Vignette baseVignette;

        if (baseline != null && baseline.TryGet(out baseVignette))
        {
            if (baseVignette.intensity.overrideState)
                vignette = baseVignette.intensity.value;

            if (baseVignette.smoothness.overrideState)
            {
                vignetteSmooth = baseVignette.smoothness.value;

                vignetteSmoothSet = true;
            }
        }

        string path = ProfileFolder + "/" + profileName + ".asset";

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            AssetDatabase.CreateAsset(profile, path);
        }

        ColorAdjustments colour = GetOrAdd<ColorAdjustments>(profile);

        colour.postExposure.Override(exposure + exposureDelta);

        colour.saturation.Override(
            Mathf.Clamp(saturation + saturationDelta, -100f, 100f));

        colour.contrast.Override(
            Mathf.Clamp(contrast + contrastDelta, -100f, 100f));

        colour.colorFilter.Override(new Color(
            filter.r * filterTint.r,
            filter.g * filterTint.g,
            filter.b * filterTint.b,
            1f));

        Vignette vig = GetOrAdd<Vignette>(profile);

        vig.intensity.Override(Mathf.Clamp01(vignette + vignetteDelta));

        if (!vignetteSmoothSet)
        {
            vig.smoothness.Override(vignetteSmooth);
        }

        EditorUtility.SetDirty(colour);

        EditorUtility.SetDirty(vig);

        EditorUtility.SetDirty(profile);

        AssetDatabase.SaveAssets();

        GameObject go = new GameObject(prefix + "Atmosphere");

        go.transform.SetParent(root, false);

        Undo.RegisterCreatedObjectUndo(go, "Lobby Decay");

        Volume volume = go.AddComponent<Volume>();

        volume.isGlobal = true;

        volume.priority = priority;

        volume.weight = 1f;

        volume.sharedProfile = profile;

        Debug.Log("[LobbyDecay] " + profileName + ": exposure "
            + (exposure + exposureDelta).ToString("0.00")
            + ", saturation " + (saturation + saturationDelta).ToString("0")
            + ", vignette " + Mathf.Clamp01(vignette + vignetteDelta).ToString("0.00")
            + (baseline != null
                ? " (relative to " + baseline.name + ")"
                : " (no scene volume found - absolute)") + ".");

        return volume;
    }


    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T existing;

        if (profile.TryGet(out existing))
        {
            return existing;
        }

        T added = profile.Add<T>(false);

        added.name = typeof(T).Name;

        added.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;

        AssetDatabase.AddObjectToAsset(added, profile);

        return added;
    }


    // The highest-priority global volume that is NOT one of ours.
    private static VolumeProfile BaselineProfile(Transform dayRoot)
    {
        VolumeProfile best = null;

        float bestPriority = float.MinValue;

        foreach (GameObject root in dayRoot.gameObject.scene.GetRootGameObjects())
        {
            foreach (Volume v in root.GetComponentsInChildren<Volume>(true))
            {
                if (!v.isGlobal || v.sharedProfile == null)
                {
                    continue;
                }

                if (IsInExcludedBranch(v.transform))
                {
                    continue;
                }

                if (v.priority > bestPriority)
                {
                    bestPriority = v.priority;

                    best = v.sharedProfile;
                }
            }
        }

        return best;
    }


    // =========================================================
    // TOGGLE WIRING
    // =========================================================

    /// <summary>
    /// Registers an add-only layer with LobbyDayVisuals (for its
    /// Preview/Day N menu and for Day 2's switch-offs). At runtime
    /// GameFlowManager also switches the DayNChanges roots by name;
    /// both agree, so neither fights the other.
    /// </summary>
    internal static void WireLayer(
        GameObject lobby,
        GameObject dayRoot,
        int day)
    {
        LobbyDayVisuals visuals = lobby.GetComponent<LobbyDayVisuals>();

        if (visuals == null)
        {
            visuals = Undo.AddComponent<LobbyDayVisuals>(lobby);
        }

        Undo.RecordObject(visuals, "Lobby Decay");

        SerializedObject so = new SerializedObject(visuals);

        SerializedProperty layers = so.FindProperty("layers");

        SerializedProperty layer = null;

        for (int i = 0; i < layers.arraySize; i++)
        {
            SerializedProperty candidate = layers.GetArrayElementAtIndex(i);

            if (candidate.FindPropertyRelative("fromDay").intValue == day)
            {
                layer = candidate;

                break;
            }
        }

        if (layer == null)
        {
            layers.InsertArrayElementAtIndex(layers.arraySize);

            layer = layers.GetArrayElementAtIndex(layers.arraySize - 1);

            layer.FindPropertyRelative("fromDay").intValue = day;

            layer.FindPropertyRelative("switchOff").ClearArray();
        }

        layer.FindPropertyRelative("addRoot").objectReferenceValue = dayRoot;

        so.ApplyModifiedProperties();
    }
}
#endif
