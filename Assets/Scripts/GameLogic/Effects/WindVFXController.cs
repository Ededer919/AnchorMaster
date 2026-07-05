using UnityEngine;

/// <summary>
/// Spawns the Stylised Wind VFX prefab during disaster wind phase.
/// Replaces HDRP material with built-in RP compatible one.
/// </summary>
public class WindVFXController : MonoBehaviour
{
    private GameObject windInstance;
    private ParticleSystem ps;
    private Material windMat;

    void Awake()
    {
        // Create a built-in RP compatible material using the wind texture
        var tex = Resources.Load<Texture2D>("../Stylised_Wind_VFX/Textures/Air_Lines_c");
        if (tex == null)
        {
            // Try direct path
            tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Stylised_Wind_VFX/Textures/Air_Lines_c.tif");
        }

        var shader = Shader.Find("Particles/Alpha Blend");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        windMat = new Material(shader);
        if (tex != null) windMat.SetTexture("_MainTex", tex);
        windMat.color = new Color(0.9f, 0.95f, 1f, 0.85f);
    }

    /// <summary>
    /// Starts the wind VFX at the given position with the given direction and scale.
    /// </summary>
    public void PlayWind(Vector3 position, Vector2 windDirection, float areaWidth, float areaHeight)
    {
        if (windInstance != null) return;

        // Load the prefab
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Stylised_Wind_VFX/Prefabs/Heavy_Wind.prefab");

        if (prefab != null)
        {
            windInstance = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            // Fallback: create a simple particle system
            windInstance = CreateFallbackWind();
        }

        windInstance.name = "WindVFX";
        windInstance.layer = 8; // Game layer

        // Position and orient
        float angle = Mathf.Atan2(windDirection.y, windDirection.x) * Mathf.Rad2Deg;
        windInstance.transform.position = position;
        windInstance.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Scale to cover the game area (enlarged for better visibility)
        windInstance.transform.localScale = new Vector3(
            areaWidth / 40f, areaHeight / 40f, 1f);

        // Replace material on all ParticleSystemRenderers
        foreach (var renderer in windInstance.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            renderer.material = windMat;
            renderer.sortingOrder = 15;
        }

        ps = windInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            Debug.Log("[WindVFX] Wind particle effect started");
        }
    }

    /// <summary>
    /// Stops and destroys the wind VFX.
    /// </summary>
    public void StopWind()
    {
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (windInstance != null)
        {
            Destroy(windInstance, 2f); // Let existing particles fade
            windInstance = null;
        }

        ps = null;
    }

    private GameObject CreateFallbackWind()
    {
        var go = new GameObject("WindVFX_Fallback");
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 10f;
        main.startSize = 0.4f;
        main.startColor = new Color(0.9f, 0.95f, 1f, 0.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        main.loop = true;
        main.playOnAwake = false;

        var em = ps.emission;
        em.rateOverTime = 150;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30, 15, 1);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(12f, 22f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (windMat != null) renderer.material = windMat;
        renderer.sortingOrder = 15;

        return go;
    }
}
