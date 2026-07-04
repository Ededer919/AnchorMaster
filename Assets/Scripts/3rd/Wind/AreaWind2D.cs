using UnityEngine;

/// <summary>
/// Area wind source. Produces uniform wind across a rectangular area (like a wind wall).
/// Supports timed duration, strength variation within a range, fade in/out, and edge falloff.
/// The area follows the GameObject's position and rotation.
/// Auto-registers with WindSystem2D on enable.
/// </summary>
[AddComponentMenu("Wind2D/Area Wind 2D")]
public class AreaWind2D : MonoBehaviour, IWindSource2D
{
    [Header("Wind Area")]
    [Range(0.1f, 50f)] public float width = 10f;
    [Range(0.1f, 50f)] public float height = 2f;

    [Header("Wind Direction")]
    public Vector2 direction = new Vector2(1f, 0f);

    [Header("Wind Strength")]
    [Range(0f, 50f)] public float minStrength = 3f;
    [Range(0f, 50f)] public float maxStrength = 8f;
    [Range(0.01f, 2f)] public float variationFrequency = 0.5f;

    [Header("Duration")]
    public float duration = 5f;
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.5f;
    public bool loop = false;

    [Header("Edge Falloff")]
    [Range(0f, 0.5f)] public float edgeFalloff = 0.1f;

    private float noiseSeed;
    private float startTime;

    private void Awake()
    {
        noiseSeed = Random.Range(0f, 1000f);
    }

    private void OnEnable()
    {
        startTime = Time.time;
        WindSystem2D.EnsureExists().RegisterWindSource(this);
    }

    private void Start()
    {
        if (WindSystem2D.Instance != null)
            WindSystem2D.Instance.RegisterWindSource(this);
    }

    private void OnDisable()
    {
        WindSystem2D.Instance?.UnregisterWindSource(this);
    }

    private void Update()
    {
        float elapsed = Time.time - startTime;
        if (elapsed >= duration)
        {
            if (loop)
                startTime = Time.time;
            else
                enabled = false;
        }
    }

    /// <summary>
    /// Calculates wind force at the given position.
    /// Wind is uniform within the rectangle, with optional smooth falloff at edges.
    /// Strength varies between minStrength and maxStrength using Perlin noise.
    /// Fade in/out is applied based on elapsed time within the duration.
    /// </summary>
    public Vector2 GetForceAt(Vector2 position, float time)
    {
        float elapsed = time - startTime;
        if (elapsed >= duration)
            return Vector2.zero;

        // Transform position to local space (accounts for rotation)
        Vector2 localPos = transform.InverseTransformPoint(position);

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        // Outside rectangle
        if (Mathf.Abs(localPos.x) > halfW || Mathf.Abs(localPos.y) > halfH)
            return Vector2.zero;

        // Edge falloff (smoothstep)
        float falloff = 1f;
        if (edgeFalloff > 0f)
        {
            float edgeX = halfW * edgeFalloff;
            float edgeY = halfH * edgeFalloff;

            float distX = halfW - Mathf.Abs(localPos.x);
            float distY = halfH - Mathf.Abs(localPos.y);

            float fx = 1f, fy = 1f;
            if (distX < edgeX && edgeX > 0f)
            {
                fx = Mathf.Clamp01(distX / edgeX);
                fx = fx * fx * (3f - 2f * fx);
            }
            if (distY < edgeY && edgeY > 0f)
            {
                fy = Mathf.Clamp01(distY / edgeY);
                fy = fy * fy * (3f - 2f * fy);
            }

            falloff = fx * fy;
        }

        // Strength variation via Perlin noise
        float noise = Mathf.PerlinNoise(time * variationFrequency + noiseSeed, 0f);
        float currentStrength = Mathf.Lerp(minStrength, maxStrength, noise);

        // Fade in / fade out
        float fade = 1f;
        if (fadeInTime > 0f && elapsed < fadeInTime)
        {
            fade = elapsed / fadeInTime;
            fade = fade * fade * (3f - 2f * fade);
        }
        else if (fadeOutTime > 0f && elapsed > duration - fadeOutTime)
        {
            fade = (duration - elapsed) / fadeOutTime;
            fade = Mathf.Clamp01(fade);
            fade = fade * fade * (3f - 2f * fade);
        }

        Vector2 dir = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.right;

        return dir * currentStrength * falloff * fade;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw area rectangle (follows rotation)
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 size = new Vector3(width, height, 0.1f);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(Vector3.zero, size);

        // Draw direction arrow (world space)
        Gizmos.matrix = Matrix4x4.identity;
        Vector2 center = transform.position;
        Vector2 dir = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.right;
        Gizmos.color = Color.yellow;
        DrawArrow(center, dir, Mathf.Clamp(maxStrength, 0.5f, 5f));
    }

    private static void DrawArrow(Vector2 origin, Vector2 direction, float length)
    {
        Vector2 end = origin + direction * length;
        Gizmos.DrawLine(origin, end);
        Vector2 perp = new Vector2(-direction.y, direction.x);
        Gizmos.DrawLine(end, end - direction * 0.3f + perp * 0.15f);
        Gizmos.DrawLine(end, end - direction * 0.3f - perp * 0.15f);
    }
}
