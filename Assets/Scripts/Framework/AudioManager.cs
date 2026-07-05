using UnityEngine;

/// <summary>
/// Singleton audio manager: plays BGM (looped) and SFX (one-shot).
/// Loads clips from Resources/new/mucsic/.
/// BGM starts automatically in Awake.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private AudioClip bgmClip;
    private AudioClip clickClip;
    private AudioClip putClip;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0.4f;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = 0.7f;
        sfxSource.playOnAwake = false;

        bgmClip = Resources.Load<AudioClip>("new/mucsic/bgm");
        clickClip = Resources.Load<AudioClip>("new/mucsic/click");
        putClip = Resources.Load<AudioClip>("new/mucsic/put");

        if (bgmClip == null) Debug.LogWarning("[Audio] BGM not found at new/mucsic/bgm");
        if (clickClip == null) Debug.LogWarning("[Audio] Click SFX not found at new/mucsic/click");
        if (putClip == null) Debug.LogWarning("[Audio] Put SFX not found at new/mucsic/put");

        // Start BGM immediately
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
            Debug.Log("[Audio] BGM started");
        }
    }

    public void PlayBGM()
    {
        if (bgmClip != null && !bgmSource.isPlaying)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource.isPlaying)
            bgmSource.Stop();
    }

    public void PlayClick()
    {
        if (clickClip != null)
            sfxSource.PlayOneShot(clickClip);
    }

    public void PlayPut()
    {
        if (putClip != null)
            sfxSource.PlayOneShot(putClip);
    }
}
