using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameParticleEffects : MonoBehaviour
{
    [Header("Dust Motes")]
    public int moteCount = 40;
    public float moteMinSpeed = 8f;
    public float moteMaxSpeed = 20f;
    public float moteMinSize = 8f;
    public float moteMaxSize = 16f;
    public float moteMinAlpha = 0.4f;
    public float moteMaxAlpha = 0.8f;
    public float moteFlickerSpeed = 1.5f;
    public float moteDriftRange = 40f;

    [Header("Wind Petals")]
    public int petalCount = 15;
    public float petalMinSpeed = 25f;
    public float petalMaxSpeed = 45f;
    public float petalMinSize = 30f;
    public float petalMaxSize = 50f;
    public float petalMinAlpha = 0.6f;
    public float petalMaxAlpha = 0.9f;
    public float petalSwayAmplitude = 60f;
    public float petalSwaySpeed = 2f;

    [Header("Sun Sparkles")]
    public int sparkleCount = 12;
    public float sparkleRadius = 200f;
    public float sparkleMinSize = 8f;
    public float sparkleMaxSize = 18f;
    public float sparkleMinAlpha = 0.4f;
    public float sparkleMaxAlpha = 0.9f;
    public float sparkleSpeed = 1f;
    public Vector2 sunPosition = new Vector2(220f, 405f);

    private class Particle
    {
        public RectTransform rt;
        public Image img;
        public float speedY;
        public float speedX;
        public float driftPhase;
        public float driftSpeed;
        public float driftAmp;
        public float baseAlpha;
        public float flickerPhase;
        public float flickerSpeed;
        public float baseX;
        public Vector2 anchor;
        public float orbitPhase;
    }

    private readonly List<Particle> particles = new List<Particle>();
    private Canvas canvas;
    private float canvasW, canvasH;
    private Sprite dotSprite;
    private float lastRealTime;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var rt = canvas.transform as RectTransform;
        canvasW = rt.rect.width;
        canvasH = rt.rect.height;

        dotSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 100);

        SpawnDustMotes();
        SpawnWindPetals();
        SpawnSunSparkles();

        lastRealTime = Time.realtimeSinceStartup;
    }

    void SpawnDustMotes()
    {
        for (int i = 0; i < moteCount; i++)
        {
            var p = CreateParticle("DustMote", new Color(1f, 0.95f, 0.7f, 1f));
            float size = Random.Range(moteMinSize, moteMaxSize);
            p.rt.sizeDelta = new Vector2(size, size);
            p.baseAlpha = Random.Range(moteMinAlpha, moteMaxAlpha);
            p.img.color = new Color(1f, 0.95f, 0.7f, p.baseAlpha);

            p.baseX = Random.Range(-canvasW * 0.5f, canvasW * 0.5f);
            p.rt.anchoredPosition = new Vector2(p.baseX, Random.Range(-canvasH * 0.5f, canvasH * 0.5f));
            p.speedY = Random.Range(moteMinSpeed, moteMaxSpeed);
            p.driftSpeed = Random.Range(0.3f, 0.8f);
            p.driftAmp = Random.Range(10f, moteDriftRange);
            p.driftPhase = Random.Range(0f, 6.28f);
            p.flickerSpeed = Random.Range(0.5f, moteFlickerSpeed);
            p.flickerPhase = Random.Range(0f, 6.28f);

            particles.Add(p);
        }
    }

    void SpawnWindPetals()
    {
        for (int i = 0; i < petalCount; i++)
        {
            var p = CreateParticle("Petal", new Color(1f, 0.7f, 0.8f, 1f));
            float size = Random.Range(petalMinSize, petalMaxSize);
            p.rt.sizeDelta = new Vector2(size, size * 0.5f);
            p.baseAlpha = Random.Range(petalMinAlpha, petalMaxAlpha);
            p.img.color = new Color(1f, 0.7f, 0.8f, p.baseAlpha);

            float y = Random.Range(-canvasH * 0.4f, canvasH * 0.5f);
            p.rt.anchoredPosition = new Vector2(Random.Range(-canvasW * 0.4f, canvasW * 0.4f), y);
            p.speedX = Random.Range(petalMinSpeed, petalMaxSpeed);
            p.driftAmp = petalSwayAmplitude;
            p.driftSpeed = Random.Range(1f, petalSwaySpeed);
            p.driftPhase = Random.Range(0f, 6.28f);
            p.baseX = y;

            particles.Add(p);
        }
    }

    void SpawnSunSparkles()
    {
        for (int i = 0; i < sparkleCount; i++)
        {
            var p = CreateParticle("Sparkle", new Color(1f, 0.9f, 0.5f, 1f));
            float size = Random.Range(sparkleMinSize, sparkleMaxSize);
            p.rt.sizeDelta = new Vector2(size, size);
            p.baseAlpha = Random.Range(sparkleMinAlpha, sparkleMaxAlpha);
            p.img.color = new Color(1f, 0.9f, 0.5f, p.baseAlpha);

            p.anchor = sunPosition;
            float angle = Random.Range(0f, 6.28f);
            float radius = Random.Range(50f, sparkleRadius);
            p.rt.anchoredPosition = new Vector2(
                sunPosition.x + Mathf.Cos(angle) * radius,
                sunPosition.y + Mathf.Sin(angle) * radius
            );
            p.orbitPhase = angle;
            p.driftAmp = radius;
            p.driftSpeed = sparkleSpeed * Random.Range(0.3f, 0.8f);
            p.flickerSpeed = Random.Range(1f, 3f);
            p.flickerPhase = Random.Range(0f, 6.28f);

            particles.Add(p);
        }
    }

    private Particle CreateParticle(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        go.layer = canvas.gameObject.layer;

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();
        img.sprite = dotSprite;
        img.color = color;
        img.raycastTarget = false;

        return new Particle
        {
            rt = rt,
            img = img
        };
    }

    void Update()
    {
        float now = Time.realtimeSinceStartup;
        float dt = now - lastRealTime;
        lastRealTime = now;
        if (dt > 0.5f) dt = 0.5f;

        float halfW = canvasW * 0.5f;
        float halfH = canvasH * 0.5f;

        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            var pos = p.rt.anchoredPosition;

            switch (p.rt.name)
            {
                case "DustMote":
                    pos.y += p.speedY * dt;
                    p.driftPhase += dt * p.driftSpeed;
                    pos.x = p.baseX + Mathf.Sin(p.driftPhase) * p.driftAmp;
                    p.flickerPhase += dt * p.flickerSpeed;
                    var mc = p.img.color;
                    mc.a = p.baseAlpha * (0.5f + 0.5f * Mathf.Sin(p.flickerPhase));
                    p.img.color = mc;
                    if (pos.y > halfH + 20f)
                    {
                        pos.y = -halfH - 20f;
                        p.baseX = Random.Range(-halfW, halfW);
                    }
                    break;

                case "Petal":
                    pos.x += p.speedX * dt;
                    p.driftPhase += dt * p.driftSpeed;
                    pos.y = p.baseX + Mathf.Sin(p.driftPhase) * p.driftAmp;
                    p.rt.localEulerAngles = new Vector3(0, 0, Mathf.Sin(p.driftPhase * 1.5f) * 30f);
                    if (pos.x > halfW + 50f)
                    {
                        pos.x = -halfW - 50f;
                        p.baseX = Random.Range(-canvasH * 0.4f, halfH);
                    }
                    break;

                case "Sparkle":
                    p.orbitPhase += dt * p.driftSpeed;
                    float r = p.driftAmp + Mathf.Sin(p.orbitPhase * 0.5f) * 20f;
                    pos.x = p.anchor.x + Mathf.Cos(p.orbitPhase) * r;
                    pos.y = p.anchor.y + Mathf.Sin(p.orbitPhase) * r;
                    p.flickerPhase += dt * p.flickerSpeed;
                    var sc = p.img.color;
                    sc.a = p.baseAlpha * (0.3f + 0.7f * Mathf.Abs(Mathf.Sin(p.flickerPhase)));
                    p.img.color = sc;
                    break;
            }

            p.rt.anchoredPosition = pos;
        }
    }

    void OnDestroy()
    {
        if (dotSprite != null)
            Destroy(dotSprite);
    }
}
