using UnityEngine;

/// <summary>
/// Wind receiver. Attach to any GameObject with a Rigidbody2D to apply wind forces.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[AddComponentMenu("Wind2D/Wind Receiver 2D")]
public class WindReceiver2D : MonoBehaviour
{
    [Header("Receiver Settings")]
    [Range(0f, 5f)] public float windSensitivity = 1f;
    public ForceMode2D forceMode = ForceMode2D.Force;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (WindSystem2D.Instance == null)
            return;

        Vector2 windForce = WindSystem2D.Instance.GetWindForceAt(transform.position);
        if (windForce.sqrMagnitude > 0.0001f)
            rb.AddForce(windForce * windSensitivity, forceMode);
    }
}
