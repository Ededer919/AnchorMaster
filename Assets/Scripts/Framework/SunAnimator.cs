using UnityEngine;

public class SunAnimator : MonoBehaviour
{
    [Header("Breathing Scale")]
    public float scaleAmplitude = 3f;
    public float scaleSpeed = 0.5f;

    [Header("Glow Pulse")]
    [Range(0f, 0.3f)]
    public float alphaAmplitude = 0.08f;
    public float alphaSpeed = 0.4f;

    [Header("Slow Rotation")]
    public float rotationSpeed = 3f;

    private Vector3 baseScale;
    private SpriteRenderer sr;
    private float baseAlpha;
    private float phase;

    void Start()
    {
        baseScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        baseAlpha = sr != null ? sr.color.a : 1f;
        phase = Random.Range(0f, 6.28f);
    }

    void Update()
    {
        phase += Time.deltaTime;

        float s = 1f + Mathf.Sin(phase * scaleSpeed) * scaleAmplitude / 100f;
        transform.localScale = baseScale * s;

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (sr != null)
        {
            var c = sr.color;
            c.a = baseAlpha + Mathf.Sin(phase * alphaSpeed) * alphaAmplitude;
            sr.color = c;
        }
    }
}
