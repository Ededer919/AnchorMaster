using UnityEngine;

/// <summary>
/// Plays a particle burst when a block is placed on the grid.
/// Singleton — call PlayEffect(position, color) from placement code.
/// </summary>
public class BlockPlacementEffect : MonoBehaviour
{
    public static BlockPlacementEffect Instance { get; private set; }

    private const int GameLayer = 8;
    private const int SortOrder = 25;

    private Material particleMat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Sprites/Default renders correctly in 2D built-in RP
        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
            particleMat = new Material(shader);
    }

    /// <summary>
    /// Spawns a one-shot placement effect at the given world position.
    /// </summary>
    public void PlayEffect(Vector3 position, Color color)
    {
        SpawnDebris(position, color);
        SpawnRing(position, color);
        SpawnDust(position, color);
    }

    // ── Debris: small squares bursting outward, falling with gravity ──

    private void SpawnDebris(Vector3 pos, Color color)
    {
        const int count = 16;
        const float life = 0.6f;
        const float speed = 3f;
        const float size = 0.15f;
        const float gravity = 5f;

        var ps = CreatePS("Debris", pos);
        var main = ps.main;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.playOnAwake = false;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        var em = ps.emission;
        em.burstCount = 1;
        em.SetBurst(0, new ParticleSystem.Burst(0f, count));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;
        shape.radiusThickness = 1f;

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeGradient(color, 1f, 0f);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sortingOrder = SortOrder;
        if (particleMat != null) r.material = particleMat;

        ps.Play();
        Destroy(ps.gameObject, life + 0.3f);
    }

    // ── Ring: single expanding circle that fades ──

    private void SpawnRing(Vector3 pos, Color color)
    {
        const float life = 0.4f;

        var ps = CreatePS("Ring", pos);
        var main = ps.main;
        main.startLifetime = life;
        main.startSpeed = 0f;
        main.startSize = 0.2f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1;
        main.playOnAwake = false;

        var em = ps.emission;
        em.burstCount = 1;
        em.SetBurst(0, new ParticleSystem.Burst(0f, 1));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.radiusThickness = 0f;

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 1.4f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeGradient(color, 0.7f, 0f);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sortingOrder = SortOrder + 1;
        if (particleMat != null) r.material = particleMat;

        ps.Play();
        Destroy(ps.gameObject, life + 0.3f);
    }

    // ── Dust: slow-rising soft particles that expand and fade ──

    private void SpawnDust(Vector3 pos, Color color)
    {
        const int count = 8;
        const float life = 0.7f;
        const float speed = 0.6f;
        const float size = 0.3f;

        var ps = CreatePS("Dust", pos);
        var main = ps.main;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = new Color(color.r, color.g, color.b, 0.4f);
        main.gravityModifier = -0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.playOnAwake = false;

        var em = ps.emission;
        em.burstCount = 1;
        em.SetBurst(0, new ParticleSystem.Burst(0f, count));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;
        shape.radiusThickness = 1f;

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.5f, 1f), new Keyframe(1f, 1.5f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeGradient(new Color(color.r, color.g, color.b, 0.4f), 0.4f, 0f);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sortingOrder = SortOrder - 1;
        if (particleMat != null) r.material = particleMat;

        ps.Play();
        Destroy(ps.gameObject, life + 0.3f);
    }

    // ── Helpers ──

    private ParticleSystem CreatePS(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.layer = GameLayer;
        go.transform.position = pos;
        return go.AddComponent<ParticleSystem>();
    }

    private static Gradient FadeGradient(Color color, float alphaStart, float alphaEnd)
    {
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) }
        );
        return g;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
