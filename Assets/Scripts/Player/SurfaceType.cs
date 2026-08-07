using UnityEngine;

public class SurfaceType : MonoBehaviour
{
    public enum Surface
    {
        Concrete,
        Wood,
        Grass,
        Dirt,
        Metal,
        Stone,
        Carpet,
        Water,
        Gravel,
        Ground
    }

    [Header("Surface Type")]

    [SerializeField]
    private Surface surface = Surface.Concrete;

    public Surface GetSurface()
    {
        return surface;
    }
}