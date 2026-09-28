using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Makes a portal surface an actual window into the space behind its
/// linked portal, rather than a picture of one.
///
/// The idea in one line: if your eye were standing behind the far
/// portal, in the same relative position it currently occupies in
/// front of this one, what would it see? Render that, and paint it
/// onto this surface at the pixel's own screen position. The image
/// then lines up with the hole from every angle, for free, because
/// it was rendered with this camera's own projection.
///
/// Put this on the portal surface. Point 'linkedPortal' at the other
/// one. The other one should point back.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public class PortalSurfaceRenderer : MonoBehaviour
{
    // =========================================================
    // LINK
    // =========================================================

    [Header("Link")]

    [Tooltip("The portal you come out of. Set the other portal's " +
             "link back to this one.")]
    public PortalSurfaceRenderer linkedPortal;

    [Tooltip("Leave empty to use Camera.main.")]
    public Camera playerCamera;

    [Tooltip("Portals normally face each other, so going through " +
             "turns you around. Off if your two portals already " +
             "face the same way.")]
    public bool flipThroughPortal = true;


    // =========================================================
    // QUALITY
    // =========================================================

    [Header("Quality")]

    [Tooltip("A portal seen through a portal seen through a " +
             "portal. Each level costs a full render of the scene.")]
    [Range(1, 5)]
    public int recursionLimit = 2;

    [Range(0.25f, 1f)]
    public float resolutionScale = 1f;

    [Tooltip("Render the view in the Scene window too. Handy while " +
             "placing portals, expensive to leave on.")]
    public bool previewInSceneView = false;


    // =========================================================
    // CLIPPING
    // =========================================================

    [Header("Clipping")]

    [Tooltip("Pushes the clip plane back slightly so the far " +
             "portal's own frame is not sliced by it.")]
    public float nearClipOffset = 0.05f;

    [Tooltip("Below this distance the oblique matrix goes " +
             "numerically unstable, so it is skipped.")]
    public float nearClipLimit = 0.2f;


    // =========================================================
    // INTERNAL
    // =========================================================

    private MeshRenderer surface;

    private Camera portalCamera;

    private RenderTexture viewTexture;

    private MaterialPropertyBlock block;

    // Rendering a portal camera raises beginCameraRendering again.
    // Without this the first portal would drag the whole chain in
    // recursively and never come back.
    private static bool isRenderingPortals;

    private static readonly int PortalTexId =
        Shader.PropertyToID("_PortalTex");

    private static readonly int HasViewId =
        Shader.PropertyToID("_HasView");


    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        surface = GetComponent<MeshRenderer>();

        block = new MaterialPropertyBlock();

        RenderPipelineManager.beginCameraRendering
            += OnBeginCameraRendering;
    }


    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering
            -= OnBeginCameraRendering;

        ReleaseResources();
    }


    // =========================================================
    // ENTRY POINT
    // =========================================================

    private void OnBeginCameraRendering(
        ScriptableRenderContext context,
        Camera camera)
    {
        if (isRenderingPortals)
        {
            return;
        }

        if (linkedPortal == null || surface == null)
        {
            SetHasView(false);

            return;
        }

        bool isGame = camera.cameraType == CameraType.Game;

        bool isScene = camera.cameraType == CameraType.SceneView;

        if (!isGame && !(isScene && previewInSceneView))
        {
            return;
        }

        Camera viewer = ResolveViewer(camera, isScene);

        if (viewer == null || camera == portalCamera)
        {
            return;
        }

        // Nothing to do if this portal is not on screen.
        if (!IsVisibleFrom(camera))
        {
            return;
        }

        isRenderingPortals = true;

        try
        {
            RenderViewChain(camera);
        }
        finally
        {
            isRenderingPortals = false;

            // Never leave the surface hidden, whatever went wrong.
            if (surface != null)
            {
                surface.enabled = true;
            }
        }
    }


    private Camera ResolveViewer(Camera camera, bool isScene)
    {
        if (isScene)
        {
            return camera;
        }

        if (playerCamera != null)
        {
            return playerCamera;
        }

        return Camera.main;
    }


    // =========================================================
    // THE CHAIN
    // =========================================================

    private void RenderViewChain(Camera viewer)
    {
        EnsureTexture(viewer);

        EnsureCamera(viewer);

        Matrix4x4 flip = flipThroughPortal
            ? Matrix4x4.TRS(Vector3.zero,
                            Quaternion.Euler(0f, 180f, 0f),
                            Vector3.one)
            : Matrix4x4.identity;

        // One step through the portal is: leave this portal's local
        // space, turn around, and re-enter the world through the
        // linked portal. Applying that repeatedly walks the camera
        // further and further down the chain of reflections.
        Matrix4x4 step =
            linkedPortal.transform.localToWorldMatrix
            * flip
            * transform.worldToLocalMatrix;

        int levels = Mathf.Max(1, recursionLimit);

        Vector3[] positions = new Vector3[levels];

        Quaternion[] rotations = new Quaternion[levels];

        Matrix4x4 m = viewer.transform.localToWorldMatrix;

        for (int i = 0; i < levels; i++)
        {
            m = step * m;

            // Filled back to front: the deepest view has to exist
            // before the one in front of it can show it.
            int index = levels - i - 1;

            positions[index] = m.GetColumn(3);

            rotations[index] = m.rotation;
        }

        // The innermost render must not see this portal, or it would
        // sample the texture it is in the middle of writing.
        surface.enabled = false;

        for (int i = 0; i < levels; i++)
        {
            portalCamera.transform.SetPositionAndRotation(
                positions[i], rotations[i]);

            ApplyObliqueClip(viewer);

            Submit();

            // From here on the portal may appear in its own view,
            // showing the level we just finished. That is the
            // recursion.
            surface.enabled = true;
        }

        SetHasView(true);
    }


    // =========================================================
    // OBLIQUE NEAR PLANE
    // =========================================================
    //
    // The virtual camera sits behind the linked portal, so anything
    // between it and that portal - the wall, the frame, a prop - is
    // in the way of a view it should not obstruct. Rather than move
    // the camera, the near plane is skewed onto the portal's own
    // plane, so that geometry is clipped away exactly at the
    // opening.
    // =========================================================

    private void ApplyObliqueClip(Camera viewer)
    {
        Transform plane = linkedPortal.transform;

        // Which side of the portal the camera is on decides the sign
        // of the clip normal; get it wrong and it clips the half you
        // wanted to keep.
        int side = System.Math.Sign(
            Vector3.Dot(plane.forward,
                plane.position - portalCamera.transform.position));

        Vector3 camSpacePos =
            portalCamera.worldToCameraMatrix
                .MultiplyPoint(plane.position);

        Vector3 camSpaceNormal =
            portalCamera.worldToCameraMatrix
                .MultiplyVector(plane.forward) * side;

        float camSpaceDistance =
            -Vector3.Dot(camSpacePos, camSpaceNormal)
            + nearClipOffset;

        if (Mathf.Abs(camSpaceDistance) > nearClipLimit)
        {
            Vector4 clipPlane = new Vector4(
                camSpaceNormal.x,
                camSpaceNormal.y,
                camSpaceNormal.z,
                camSpaceDistance);

            portalCamera.projectionMatrix =
                viewer.CalculateObliqueMatrix(clipPlane);
        }
        else
        {
            portalCamera.projectionMatrix = viewer.projectionMatrix;
        }
    }


    // =========================================================
    // SUBMIT
    // =========================================================

    private void Submit()
    {
        UniversalRenderPipeline.SingleCameraRequest request =
            new UniversalRenderPipeline.SingleCameraRequest();

        request.destination = viewTexture;

        if (RenderPipeline.SupportsRenderRequest(
                portalCamera, request))
        {
            RenderPipeline.SubmitRenderRequest(
                portalCamera, request);
        }
    }


    // =========================================================
    // RESOURCES
    // =========================================================

    private void EnsureTexture(Camera viewer)
    {
        int w = Mathf.Max(16,
            Mathf.RoundToInt(viewer.pixelWidth * resolutionScale));

        int h = Mathf.Max(16,
            Mathf.RoundToInt(viewer.pixelHeight * resolutionScale));

        if (viewTexture != null &&
            viewTexture.width == w &&
            viewTexture.height == h)
        {
            return;
        }

        if (viewTexture != null)
        {
            viewTexture.Release();

            DestroyImmediate(viewTexture);
        }

        viewTexture = new RenderTexture(w, h, 24,
            RenderTextureFormat.DefaultHDR);

        viewTexture.name = name + " view";

        viewTexture.Create();

        SetHasView(true);
    }


    private void EnsureCamera(Camera viewer)
    {
        if (portalCamera == null)
        {
            GameObject go = new GameObject(name + " portal camera");

            go.hideFlags = HideFlags.HideAndDontSave;

            portalCamera = go.AddComponent<Camera>();

            // Driven entirely by SubmitRenderRequest, so it must not
            // render itself as part of the normal loop.
            portalCamera.enabled = false;

            UniversalAdditionalCameraData data =
                portalCamera.GetUniversalAdditionalCameraData();

            data.renderType = CameraRenderType.Base;

            data.renderShadows = true;
        }

        portalCamera.CopyFrom(viewer);

        portalCamera.enabled = false;

        portalCamera.targetTexture = viewTexture;

        // A portal should not show itself from the inside out.
        portalCamera.cullingMask = viewer.cullingMask;
    }


    private void SetHasView(bool has)
    {
        if (surface == null || block == null)
        {
            return;
        }

        surface.GetPropertyBlock(block);

        if (has && viewTexture != null)
        {
            block.SetTexture(PortalTexId, viewTexture);
        }

        block.SetFloat(HasViewId, has ? 1f : 0f);

        surface.SetPropertyBlock(block);
    }


    private void ReleaseResources()
    {
        SetHasView(false);

        if (viewTexture != null)
        {
            viewTexture.Release();

            DestroyImmediate(viewTexture);

            viewTexture = null;
        }

        if (portalCamera != null)
        {
            DestroyImmediate(portalCamera.gameObject);

            portalCamera = null;
        }
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private bool IsVisibleFrom(Camera camera)
    {
        Plane[] planes =
            GeometryUtility.CalculateFrustumPlanes(camera);

        return GeometryUtility.TestPlanesAABB(planes, surface.bounds);
    }


    /// <summary>
    /// Where a point in front of this portal ends up behind the
    /// linked one. Use this to move the player through.
    /// </summary>
    public Vector3 TransformThrough(Vector3 worldPosition)
    {
        Matrix4x4 flip = flipThroughPortal
            ? Matrix4x4.TRS(Vector3.zero,
                            Quaternion.Euler(0f, 180f, 0f),
                            Vector3.one)
            : Matrix4x4.identity;

        Matrix4x4 step =
            linkedPortal.transform.localToWorldMatrix
            * flip
            * transform.worldToLocalMatrix;

        return step.MultiplyPoint(worldPosition);
    }


    public Quaternion RotateThrough(Quaternion worldRotation)
    {
        Matrix4x4 flip = flipThroughPortal
            ? Matrix4x4.TRS(Vector3.zero,
                            Quaternion.Euler(0f, 180f, 0f),
                            Vector3.one)
            : Matrix4x4.identity;

        Matrix4x4 step =
            linkedPortal.transform.localToWorldMatrix
            * flip
            * transform.worldToLocalMatrix;

        return step.rotation * worldRotation;
    }
}
