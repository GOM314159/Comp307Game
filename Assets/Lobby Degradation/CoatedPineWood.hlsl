#ifndef COATED_PINE_WOOD_INCLUDED
#define COATED_PINE_WOOD_INCLUDED

// =============================================================
// Procedural coated pine - the wood itself
// =============================================================
//
// Wood is not a picture, it is a solid. A trunk lays down one
// growth ring per year as a cylinder around its own axis, and a
// plank is whatever those cylinders look like where the saw went
// through them. So the whole thing is built as a 3D field
// evaluated at the object-space position: cut the plank at any
// angle, bevel its edge, drill a hole in it, and the grain still
// runs correctly through the cut. A scanned 2D tile cannot do
// that - it repeats, it seams, and its edges show the face
// texture smeared down the side.
//
// Everything below is derived from that one idea.
// =============================================================


// -------------------------------------------------------------
// Hashing and noise
// -------------------------------------------------------------

float CP_Hash11(float n)
{
    return frac(sin(n) * 43758.5453123);
}

float3 CP_Hash31(float n)
{
    return frac(sin(float3(n, n + 1.7, n + 3.3))
                * float3(43758.5453, 22578.1459, 19642.3490));
}

// Value noise. Cheap, and wood wants smooth blobby variation
// rather than the sharper look of gradient noise.
float CP_ValueNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);

    // Quintic fade - continuous second derivative, so the
    // surface gradient we take later has no visible creases.
    f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float n = i.x + i.y * 157.0 + i.z * 113.0;

    float a = CP_Hash11(n +   0.0);
    float b = CP_Hash11(n +   1.0);
    float c = CP_Hash11(n + 157.0);
    float d = CP_Hash11(n + 158.0);
    float e = CP_Hash11(n + 113.0);
    float g = CP_Hash11(n + 114.0);
    float h = CP_Hash11(n + 270.0);
    float k = CP_Hash11(n + 271.0);

    float x0 = lerp(a, b, f.x);
    float x1 = lerp(c, d, f.x);
    float x2 = lerp(e, g, f.x);
    float x3 = lerp(h, k, f.x);

    return lerp(lerp(x0, x1, f.y),
                lerp(x2, x3, f.y), f.z);
}

float CP_FBM(float3 p, int octaves, float lacunarity, float gain)
{
    float sum = 0.0;
    float amp = 0.5;
    float norm = 0.0;

    [loop]
    for (int i = 0; i < octaves; i++)
    {
        sum  += amp * CP_ValueNoise(p);
        norm += amp;
        p    *= lacunarity;
        amp  *= gain;
    }

    return sum / max(norm, 1e-4);
}


// -------------------------------------------------------------
// Parameters, bundled so the height field can be re-evaluated
// at offset positions when we take its gradient.
// -------------------------------------------------------------

struct CP_WoodParams
{
    float2 trunkOffset;     // where the trunk axis sits, in the
                            // plank's YZ plane. Near zero gives
                            // quarter-sawn stripes, far away
                            // gives flat-sawn cathedrals.
    float  ringDensity;     // rings per metre
    float  lateWidth;       // fraction of the year that is latewood
    float  earlyCurve;      // how the earlywood ramps into it

    float  warpAmount;      // rings are not circles
    float  warpScaleAlong;
    float  warpScaleAcross;

    float  fibreStrength;
    float  fibreAlong;      // fibre runs down the grain, so it is
    float  fibreAcross;     // sampled at very different rates on
                            // the two axes - that anisotropy is
                            // what makes it read as wood

    int    knotCount;
    float  knotSeed;
    float  knotSize;
    float  knotSpread;
    float  knotRingDensity;
    float  knotBlend;

    float  lateRelief;
    float  fibreRelief;
};


// -------------------------------------------------------------
// The ring field
// -------------------------------------------------------------
//
// Returns the ring coordinate (whole numbers are year
// boundaries) and, separately, how much of a knot is here.
//
// The grain runs along object-space X. The trunk axis is
// parallel to it, displaced in YZ.
// -------------------------------------------------------------

float CP_RingCoord(
    float3 p,
    CP_WoodParams w,
    out float knotMask)
{
    float2 rel = p.yz - w.trunkOffset;

    float r = length(rel);

    // Rings wander. Low frequency along the length (the trunk
    // leaned as it grew), higher frequency across it.
    float3 warpP = float3(p.x * w.warpScaleAlong,
                          rel.x * w.warpScaleAcross,
                          rel.y * w.warpScaleAcross);

    r += (CP_FBM(warpP, 4, 2.0, 0.5) - 0.5) * w.warpAmount;

    float ring = r * w.ringDensity;

    knotMask = 0.0;

    // A knot is a branch: a limb that grew out of the trunk with
    // rings of its own. Near one, the ring field stops being
    // concentric about the trunk and becomes concentric about
    // the branch. Blending between the two fields is the whole
    // trick - it produces the swirl and the pinch for free.
    [loop]
    for (int i = 0; i < w.knotCount; i++)
    {
        float fi = (float)i + w.knotSeed;

        float3 h = CP_Hash31(fi * 17.13);

        float3 kpos = float3(
            (h.x * 2.0 - 1.0) * w.knotSpread,
            w.trunkOffset.x + (h.y * 2.0 - 1.0) * 0.25,
            w.trunkOffset.y + (h.z * 2.0 - 1.0) * 0.25);

        float d = length(p - kpos) / max(w.knotSize, 1e-3);

        float infl = exp(-d * d * 2.0);

        float knotRing = length(p - kpos) * w.knotRingDensity;

        ring = lerp(ring, knotRing,
                    saturate(infl * w.knotBlend));

        knotMask = max(knotMask, infl);
    }

    return ring;
}


// -------------------------------------------------------------
// Earlywood / latewood
// -------------------------------------------------------------
//
// A growth year is not a sine wave. Spring growth is fast, pale
// and wide; late-summer growth is slow, dense and dark, and it
// stops abruptly at the year boundary. So the profile is a slow
// ramp into a hard edge - asymmetric on purpose. Pine shows this
// more starkly than most timbers, which is exactly why pine
// grain reads so strongly.
// -------------------------------------------------------------

float CP_LateWood(float ring, CP_WoodParams w)
{
    float t = frac(ring);

    float ramp = pow(saturate(t), max(w.earlyCurve, 0.01));

    float late = smoothstep(1.0 - w.lateWidth, 1.0, t);

    return saturate(max(late, ramp * 0.35));
}


// -------------------------------------------------------------
// Fibre
// -------------------------------------------------------------

float CP_Fibre(float3 p, CP_WoodParams w)
{
    float3 fp = float3(p.x * w.fibreAlong,
                       p.y * w.fibreAcross,
                       p.z * w.fibreAcross);

    return CP_FBM(fp, 3, 2.17, 0.55);
}


// -------------------------------------------------------------
// Height - what the surface relief is made of.
// Sampled three times per pixel so we can take its gradient.
// -------------------------------------------------------------

float CP_Height(float3 p, CP_WoodParams w)
{
    float knotMask;

    float ring = CP_RingCoord(p, w, knotMask);

    float late = CP_LateWood(ring, w);

    float fibre = CP_Fibre(p, w);

    // Latewood is denser, so it stands slightly proud of the
    // softer earlywood once a surface has been worn or sanded.
    return late * w.lateRelief
         + (fibre - 0.5) * w.fibreRelief;
}


// -------------------------------------------------------------
// Scratches - these live in the varnish, not the timber
// -------------------------------------------------------------
//
// Deliberately kept out of the albedo. A scratch in a coated
// surface does not change the colour of the wood underneath it;
// it changes how the coat reflects. Putting scratches into the
// base colour is the single most common way this material gets
// faked badly.
// -------------------------------------------------------------

float CP_Scratches(float3 p, float density, float sharpness)
{
    // Two crossing families of long thin marks, from noise
    // stretched hard along different axes.
    float a = CP_FBM(float3(p.x * density * 0.05,
                            p.y * density,
                            p.z * density * 1.7), 2, 2.0, 0.5);

    float b = CP_FBM(float3(p.x * density * 1.3,
                            p.y * density,
                            p.z * density * 0.04), 2, 2.0, 0.5);

    float lines = max(
        1.0 - abs(a - 0.5) * sharpness,
        1.0 - abs(b - 0.5) * sharpness);

    return saturate(lines);
}

#endif // COATED_PINE_WOOD_INCLUDED
