using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DisasterManager : MonoBehaviour
{
    public static DisasterManager Instance { get; private set; }

    [Header("Disaster Settings")]
    public Vector2 forceDirection = Vector2.right;
    public float maxForce = 500f;
    public float disasterDuration = 10f;

    [Header("Force Curve")]
    public AnimationCurve forceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual Effects")]
    public ParticleSystem windEffect;
    public AudioSource audioSource;
    public AudioClip disasterSound;

    private bool isDisasterActive = false;
    private float disasterTimer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartDisaster()
    {
        if (isDisasterActive) return;

        isDisasterActive = true;
        disasterTimer = 0f;

        if (windEffect != null)
        {
            windEffect.Play();
        }

        if (audioSource != null && disasterSound != null)
        {
            audioSource.PlayOneShot(disasterSound);
        }

        StartCoroutine(DisasterCoroutine());
    }

    private IEnumerator DisasterCoroutine()
    {
        while (disasterTimer < disasterDuration)
        {
            disasterTimer += Time.deltaTime;
            float progress = disasterTimer / disasterDuration;
            float currentForce = forceCurve.Evaluate(progress) * maxForce;

            ApplyForceToStructure(currentForce);

            UIManager.Instance?.UpdateTimer(disasterTimer);

            yield return null;
        }

        EndDisaster();
    }

    private void ApplyForceToStructure(float force)
    {
        if (StructureBuilder.Instance == null) return;

        List<GameObject> parts = StructureBuilder.Instance.GetStructureParts();
        foreach (var part in parts)
        {
            if (part != null)
            {
                Rigidbody2D rb = part.GetComponent<Rigidbody2D>();
                if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                {
                    rb.AddForce(forceDirection * force);
                }
            }
        }
    }

    private void EndDisaster()
    {
        isDisasterActive = false;

        if (windEffect != null)
        {
            windEffect.Stop();
        }

        GameManager.Instance?.LevelComplete();
    }

    public void StopDisaster()
    {
        StopAllCoroutines();
        isDisasterActive = false;

        if (windEffect != null)
        {
            windEffect.Stop();
        }
    }

    public bool IsDisasterActive()
    {
        return isDisasterActive;
    }
}

public class CharacterCollision : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            GameManager.Instance?.LevelFail();
        }
    }
}
