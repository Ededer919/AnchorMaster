using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns and moves cloud sprites across the top of the screen.
/// Attach to an empty GameObject under a Canvas.
/// Clouds spawn off-screen left, drift to the right, and despawn off-screen.
/// </summary>
public class CloudMover : MonoBehaviour
{
    [Header("Cloud Sprites")]
    public Sprite[] cloudSprites;

    [Header("Movement")]
    public float minSpeed = 10f;
    public float maxSpeed = 25f;

    [Header("Spawn")]
    public float spawnInterval = 4f;
    public float minScale = 0.5f;
    public float maxScale = 1.2f;

    private RectTransform rectTransform;
    private float spawnTimer;

    private class Cloud
    {
        public RectTransform rt;
        public float speed;
    }

    private readonly List<Cloud> clouds = new List<Cloud>();

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = GetComponentInParent<RectTransform>();

        // Auto-load sprites if not assigned
        if (cloudSprites == null || cloudSprites.Length == 0)
        {
            var list = new List<Sprite>();
            var c1 = Resources.Load<Sprite>("UI/Sprites/Objects/cloud");
            var c2 = Resources.Load<Sprite>("UI/Sprites/Objects/cloud2");
            if (c1 != null) list.Add(c1);
            if (c2 != null) list.Add(c2);
            if (list.Count > 0) cloudSprites = list.ToArray();
        }

        spawnTimer = spawnInterval;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Move existing clouds
        for (int i = clouds.Count - 1; i >= 0; i--)
        {
            var c = clouds[i];
            if (c.rt == null)
            {
                clouds.RemoveAt(i);
                continue;
            }

            c.rt.anchoredPosition += Vector2.right * c.speed * dt;

            // Despawn off right edge
            float screenW = Screen.width;
            var canvas = rectTransform.GetComponent<Canvas>();
            if (canvas != null && canvas.isRootCanvas)
            {
                var canvasRT = canvas.transform as RectTransform;
                if (canvasRT != null) screenW = canvasRT.rect.width;
            }
            float halfW = screenW / 2f + 200f;
            if (c.rt.anchoredPosition.x > halfW)
            {
                Destroy(c.rt.gameObject);
                clouds.RemoveAt(i);
            }
        }

        // Spawn timer
        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            SpawnCloud();
            spawnTimer = spawnInterval + Random.Range(-1f, 2f);
        }
    }

    void SpawnCloud()
    {
        if (cloudSprites == null || cloudSprites.Length == 0 || rectTransform == null) return;

        var go = new GameObject("Cloud");
        go.transform.SetParent(rectTransform, false);
        go.layer = rectTransform.gameObject.layer;

        var rt = go.AddComponent<RectTransform>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
        sr.sortingOrder = 100;

        float scale = Random.Range(minScale, maxScale);
        rt.localScale = Vector3.one * scale;

        // Use screen size for positioning
        float screenW = Screen.width;
        float screenH = Screen.height;
        var canvas = rectTransform.GetComponent<Canvas>();
        if (canvas != null && canvas.isRootCanvas)
        {
            var canvasRT = canvas.transform as RectTransform;
            if (canvasRT != null)
            {
                screenW = canvasRT.rect.width;
                screenH = canvasRT.rect.height;
            }
        }

        // Spawn off-screen left, near top
        float leftX = -screenW / 2f - 150f;
        float topY = screenH / 2f - Random.Range(40f, 150f);
        rt.anchoredPosition = new Vector2(leftX, topY);

        clouds.Add(new Cloud
        {
            rt = rt,
            speed = Random.Range(minSpeed, maxSpeed)
        });
    }
}
