using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D wind simulation system. Manages global wind, natural wind, and all wind sources.
/// Place one instance in the scene; WindReceiver2D components query wind force from here.
/// If no instance exists, one is auto-created on first access via EnsureExists().
/// </summary>
[AddComponentMenu("Wind2D/Wind System 2D")]
public class WindSystem2D : MonoBehaviour
{
    public static WindSystem2D Instance { get; private set; }

    [Header("Global Wind")]
    public bool enableGlobalWind = true;
    public Vector2 globalWindDirection = new Vector2(1f, 0f);
    [Range(0f, 50f)] public float globalWindStrength = 2f;

    [Header("Natural Wind")]
    public bool enableNaturalWind = true;
    [Range(0f, 20f)] public float naturalWindStrength = 1f;
    [Range(0.01f, 2f)] public float naturalWindFrequency = 0.3f;
    public float naturalWindSeed = 0f;

    private readonly List<IWindSource2D> windSources = new List<IWindSource2D>();

    /// <summary>
    /// Ensures a WindSystem2D instance exists in the scene. Auto-creates one if missing.
    /// </summary>
    public static WindSystem2D EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var existing = FindObjectOfType<WindSystem2D>();
        if (existing != null)
            return existing;

        var go = new GameObject("WindSystem");
        return go.AddComponent<WindSystem2D>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // Collect any wind sources that were created before this instance existed
        var allMonos = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mono in allMonos)
        {
            if (mono is IWindSource2D ws && !windSources.Contains(ws))
                windSources.Add(ws);
        }
    }

    /// <summary>
    /// Optional bounds — wind only applies inside this rect. Set by SceneBuilder.
    /// </summary>
    public Rect? windBounds { get; set; }

    /// <summary>
    /// Returns the total wind force at the given position (global + natural + all wind sources).
    /// Returns zero outside windBounds if set.
    /// </summary>
    public Vector2 GetWindForceAt(Vector2 position)
    {
        // Cull wind outside grid bounds
        if (windBounds.HasValue && !windBounds.Value.Contains(position))
            return Vector2.zero;

        Vector2 totalForce = Vector2.zero;
        float time = Time.time;

        // Global wind: constant direction and strength
        if (enableGlobalWind && globalWindStrength > 0f)
        {
            Vector2 dir = globalWindDirection.sqrMagnitude > 0.001f
                ? globalWindDirection.normalized
                : Vector2.right;
            totalForce += dir * globalWindStrength;
        }

        // Natural wind: Perlin-noise-based varying wind direction and strength
        if (enableNaturalWind && naturalWindStrength > 0f)
        {
            float nx = Mathf.PerlinNoise(time * naturalWindFrequency + naturalWindSeed, 0f) * 2f - 1f;
            float ny = Mathf.PerlinNoise(0f, time * naturalWindFrequency + naturalWindSeed) * 2f - 1f;
            totalForce += new Vector2(nx, ny) * naturalWindStrength;
        }

        // All registered wind sources (PointWind2D, AreaWind2D, etc.)
        for (int i = 0; i < windSources.Count; i++)
        {
            var ws = windSources[i];
            if (ws != null && ((MonoBehaviour)ws).isActiveAndEnabled)
                totalForce += ws.GetForceAt(position, time);
        }

        return totalForce;
    }

    public void RegisterWindSource(IWindSource2D source)
    {
        if (!windSources.Contains(source))
            windSources.Add(source);
    }

    public void UnregisterWindSource(IWindSource2D source)
    {
        windSources.Remove(source);
    }

    // --- Convenience API ---

    /// <summary>Quickly configure global wind.</summary>
    public void SetGlobalWind(Vector2 direction, float strength)
    {
        globalWindDirection = direction;
        globalWindStrength = strength;
        enableGlobalWind = strength > 0f;
    }

    /// <summary>Quickly configure natural wind.</summary>
    public void SetNaturalWind(float strength, float frequency)
    {
        naturalWindStrength = strength;
        naturalWindFrequency = frequency;
        enableNaturalWind = strength > 0f;
    }

    /// <summary>Toggle global and natural wind at once.</summary>
    public void SetWindEnabled(bool global, bool natural)
    {
        enableGlobalWind = global;
        enableNaturalWind = natural;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = transform.position;

        if (enableGlobalWind)
        {
            Vector2 dir = globalWindDirection.sqrMagnitude > 0.001f
                ? globalWindDirection.normalized
                : Vector2.right;
            float length = Mathf.Clamp(globalWindStrength, 0.5f, 8f);
            Gizmos.color = Color.cyan;
            DrawArrow(origin, dir, length);
        }

        if (enableNaturalWind)
        {
            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(origin, 0.5f);
        }
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
