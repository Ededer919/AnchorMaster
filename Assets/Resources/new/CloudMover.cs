using System.Collections.Generic;
using UnityEngine;

public class CloudMover : MonoBehaviour
{
    [Header("Cloud Sprites")]
    public Sprite[] cloudSprites;

    [Header("Movement Settings")]
    public float minSpeed = 15f;
    public float maxSpeed = 30f;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    public float minSpawnY = 150f;
    public float maxSpawnY = 300f;
    public float minScale = 35f;
    public float maxScale = 60f;

    [Header("Boundaries (local space)")]
    public float leftBound = -1000f;
    public float rightBound = 1000f;

    private class CloudInfo
    {
        public Transform transform;
        public float speed;
    }

    private readonly List<CloudInfo> clouds = new List<CloudInfo>();
    private float spawnTimer;
    private float lastRealTime;

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform.parent;
        if (parent == null) parent = transform;

        var spriteSet = new HashSet<Sprite>();
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child == transform) continue;
            if (child.name.ToLower().Contains("cloud") && child.GetComponent<SpriteRenderer>() != null)
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr.sprite != null)
                    spriteSet.Add(sr.sprite);
                clouds.Add(new CloudInfo
                {
                    transform = child,
                    speed = Random.Range(minSpeed, maxSpeed)
                });
            }
        }

        if (cloudSprites == null || cloudSprites.Length == 0)
        {
            if (spriteSet.Count > 0)
            {
                cloudSprites = new List<Sprite>(spriteSet).ToArray();
            }
            else
            {
                cloudSprites = new Sprite[]
                {
                    Resources.Load<Sprite>("cloud"),
                    Resources.Load<Sprite>("cloud2")
                };
                if (cloudSprites[0] == null && cloudSprites[1] == null)
                    cloudSprites = null;
            }
        }

        spawnTimer = spawnInterval;
        lastRealTime = Time.realtimeSinceStartup;
    }

    void Update()
    {
        float now = Time.realtimeSinceStartup;
        float dt = now - lastRealTime;
        lastRealTime = now;
        if (dt > 0.5f) dt = 0.5f;

        for (int i = clouds.Count - 1; i >= 0; i--)
        {
            var cloud = clouds[i];
            if (cloud.transform == null)
            {
                clouds.RemoveAt(i);
                continue;
            }

            cloud.transform.localPosition += Vector3.right * cloud.speed * dt;

            if (cloud.transform.localPosition.x > rightBound)
            {
                Destroy(cloud.transform.gameObject);
                clouds.RemoveAt(i);
            }
        }

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            SpawnCloud();
            spawnTimer = spawnInterval + Random.Range(-1f, 2f);
        }
    }

    private void SpawnCloud()
    {
        if (cloudSprites == null || cloudSprites.Length == 0) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        var go = new GameObject("Cloud");
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];

        float scale = Random.Range(minScale, maxScale);
        go.transform.localScale = Vector3.one * scale;

        float y = Random.Range(minSpawnY, maxSpawnY);
        go.transform.localPosition = new Vector3(leftBound, y, -5256f);

        clouds.Add(new CloudInfo
        {
            transform = go.transform,
            speed = Random.Range(minSpeed, maxSpeed)
        });
    }
}
