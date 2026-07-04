using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelChooseEffects : MonoBehaviour
{
    [Header("Floating Particles")]
    public int particleCount = 25;
    public float particleMinSpeed = 20f;
    public float particleMaxSpeed = 50f;
    public float particleMinSize = 4f;
    public float particleMaxSize = 12f;
    public float particleMinAlpha = 0.15f;
    public float particleMaxAlpha = 0.5f;
    public float particleFlickerSpeed = 2f;

    [Header("Button Floating")]
    public float buttonFloatAmplitude = 6f;
    public float buttonFloatSpeed = 1.2f;
    public float buttonScalePulse = 0.03f;

    private class Particle
    {
        public RectTransform rt;
        public Image img;
        public float speed;
        public float flickerPhase;
        public float baseAlpha;
    }

    private class ButtonInfo
    {
        public RectTransform rt;
        public Vector2 basePos;
        public float phase;
    }

    private readonly List<Particle> particles = new List<Particle>();
    private readonly List<ButtonInfo> buttons = new List<ButtonInfo>();
    private Canvas canvas;
    private float canvasWidth, canvasHeight;
    private Sprite dotSprite;
    private float lastRealTime;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var canvasRt = canvas.transform as RectTransform;
        canvasWidth = canvasRt.rect.width;
        canvasHeight = canvasRt.rect.height;

        dotSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 100);

        SpawnParticles();

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            var child = canvas.transform.GetChild(i);
            if (child.name.StartsWith("lv") && child.GetComponent<Button>() != null)
            {
                buttons.Add(new ButtonInfo
                {
                    rt = child as RectTransform,
                    basePos = child.localPosition,
                    phase = Random.Range(0f, 6.28f)
                });
            }
        }

        lastRealTime = Time.realtimeSinceStartup;
    }

    void SpawnParticles()
    {
        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject("Particle");
            go.transform.SetParent(canvas.transform, false);
            go.layer = canvas.gameObject.layer;

            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = dotSprite;
            img.raycastTarget = false;

            float size = Random.Range(particleMinSize, particleMaxSize);
            rt.sizeDelta = new Vector2(size, size);

            float alpha = Random.Range(particleMinAlpha, particleMaxAlpha);
            img.color = new Color(1f, 1f, 1f, alpha);

            rt.anchoredPosition = new Vector2(
                Random.Range(-canvasWidth * 0.5f, canvasWidth * 0.5f),
                Random.Range(-canvasHeight * 0.5f, canvasHeight * 0.5f)
            );

            particles.Add(new Particle
            {
                rt = rt,
                img = img,
                speed = Random.Range(particleMinSpeed, particleMaxSpeed),
                flickerPhase = Random.Range(0f, 6.28f),
                baseAlpha = alpha
            });
        }
    }

    void Update()
    {
        float now = Time.realtimeSinceStartup;
        float dt = now - lastRealTime;
        lastRealTime = now;
        if (dt > 0.5f) dt = 0.5f;

        float halfW = canvasWidth * 0.5f;
        float halfH = canvasHeight * 0.5f;

        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            p.rt.anchoredPosition += Vector2.up * p.speed * dt;

            if (p.rt.anchoredPosition.y > halfH + 20f)
            {
                p.rt.anchoredPosition = new Vector2(
                    Random.Range(-halfW, halfW),
                    -halfH - 20f
                );
            }

            p.flickerPhase += dt * particleFlickerSpeed;
            float a = p.baseAlpha * (0.6f + 0.4f * Mathf.Sin(p.flickerPhase));
            var c = p.img.color;
            c.a = a;
            p.img.color = c;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            var b = buttons[i];
            b.phase += dt * buttonFloatSpeed;
            float yOffset = Mathf.Sin(b.phase) * buttonFloatAmplitude;
            float scalePulse = 1f + Mathf.Sin(b.phase * 0.8f) * buttonScalePulse;

            b.rt.anchoredPosition = b.basePos + new Vector2(0, yOffset);
            b.rt.localScale = Vector3.one * scalePulse;
        }
    }

    void OnDestroy()
    {
        if (dotSprite != null)
            Destroy(dotSprite);
    }
}
