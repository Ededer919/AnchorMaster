using UnityEngine;

/// <summary>
/// Point wind source. Produces wind within a radius, supporting radial or directional mode.
/// Auto-registers with WindSystem2D on enable.
/// </summary>
[AddComponentMenu("Wind2D/Point Wind 2D")]
public class PointWind2D : MonoBehaviour, IWindSource2D
{
    public enum WindMode
    {
        Radial,
        Directional
    }

    [Header("Point Wind Settings")]
    [Range(0.1f, 50f)] public float radius = 5f;
    [Range(0f, 50f)] public float strength = 8f;
    public WindMode mode = WindMode.Directional;
    public Vector2 fixedDirection = new Vector2(1f, 0f);

    [Header("Natural Variation")]
    [Range(0f, 1f)] public float naturalVariation = 0.2f;
    [Range(0.01f, 2f)] public float noiseFrequency = 0.5f;

    private float noiseSeed;

    private void Awake()
    {
        noiseSeed = Random.Range(0f, 1000f);
    }

    private void OnEnable()
    {
        WindSystem2D.EnsureExists().RegisterWindSource(this);
    }

    private void Start()
    {
        // Retry registration in case WindSystem2D.Awake ran after OnEnable
        if (WindSystem2D.Instance != null)
            WindSystem2D.Instance.RegisterWindSource(this);
    }

    private void OnDisable()
    {
        WindSystem2D.Instance?.UnregisterWindSource(this);
    }

    /// <summary>
    /// Calculates wind force at the given position with distance falloff and natural variation.
    /// </summary>
    public Vector2 GetForceAt(Vector2 position, float time)
    {
        Vector2 center = transform.position;
        float distance = Vector2.Distance(position, center);

        if (distance > radius)
            return Vector2.zero;

        // Smooth falloff: strongest at center, zero at edge
        float t = distance / radius;
        float falloff = 1f - t;
        falloff = falloff * falloff * (3f - 2f * falloff);

        Vector2 direction;
        if (mode == WindMode.Radial)
        {
            if (distance < 0.01f)
                return Vector2.zero;
            direction = (position - center) / distance;
        }
        else
        {
            direction = fixedDirection.sqrMagnitude > 0.001f
                ? fixedDirection.normalized
                : Vector2.right;
        }

        // Natural variation
        float variation = 1f;
        if (naturalVariation > 0f)
        {
            float noise = Mathf.PerlinNoise(time * noiseFrequency + noiseSeed, 0f) * 2f - 1f;
            variation = 1f + noise * naturalVariation;
        }

        return direction * strength * falloff * variation;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = transform.position;

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
        Gizmos.DrawWireSphere(center, radius);

        Vector2 dir = mode == WindMode.Radial
            ? Vector2.up
            : (fixedDirection.sqrMagnitude > 0.001f ? fixedDirection.normalized : Vector2.right);
        Gizmos.color = Color.yellow;
        DrawArrow(center, dir, Mathf.Clamp(strength, 0.5f, 5f));
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
