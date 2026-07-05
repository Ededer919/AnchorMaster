using UnityEngine;

/// <summary>
/// During the wind phase, smoothly zooms the Main Camera in and follows the first balloon.
/// Restores the original camera position/size when wind ends.
/// </summary>
public class BalloonCamera : MonoBehaviour
{
    private Camera cam;
    private Camera uiCam;
    private Vector3 basePosition;
    private float baseOrthoSize;

    private GridObject trackedBalloon;
    private bool tracking;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSize = 3.5f;
    [SerializeField] private float followLerpSpeed = 3f;
    [SerializeField] private float zoomLerpSpeed = 2f;

    void Awake()
    {
        cam = Camera.main;
        if (cam == null) return;

        basePosition = cam.transform.position;
        baseOrthoSize = cam.orthographicSize;

        // Find UI Camera for syncing
        var allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in allCams)
        {
            if (c != cam && c.gameObject.name == "UI Camera")
            {
                uiCam = c;
                break;
            }
        }
    }

    /// <summary>
    /// Starts tracking the first balloon found in the scene.
    /// </summary>
    public void StartTracking()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Record base state
        basePosition = cam.transform.position;
        baseOrthoSize = cam.orthographicSize;

        // Find a balloon
        var allObjs = FindObjectsByType<GridObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if (obj != null && obj.objectType == ObjectType.Balloon)
            {
                trackedBalloon = obj;
                break;
            }
        }

        tracking = trackedBalloon != null;
    }

    /// <summary>
    /// Stops tracking and restores the camera.
    /// </summary>
    public void StopTracking()
    {
        tracking = false;
        trackedBalloon = null;
    }

    void LateUpdate()
    {
        if (!tracking || cam == null) return;

        // If balloon destroyed or null, stop
        if (trackedBalloon == null)
        {
            tracking = false;
            return;
        }

        Vector3 balloonPos = trackedBalloon.transform.position;

        // Follow balloon (lerp position, keep z fixed)
        Vector3 targetPos = new Vector3(balloonPos.x, balloonPos.y, cam.transform.position.z);
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * followLerpSpeed);

        // Zoom in (lerp orthographic size)
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomSize, Time.deltaTime * zoomLerpSpeed);

        // Sync UI camera
        if (uiCam != null)
        {
            uiCam.transform.position = cam.transform.position;
            uiCam.orthographicSize = cam.orthographicSize;
        }
    }

    /// <summary>
    /// Smoothly restores camera to original position and size.
    /// Called every frame by DisasterWindController after wind ends.
    /// </summary>
    public bool IsRestored()
    {
        if (cam == null) return true;

        bool posDone = Vector3.Distance(cam.transform.position, basePosition) < 0.05f;
        bool sizeDone = Mathf.Abs(cam.orthographicSize - baseOrthoSize) < 0.05f;
        return posDone && sizeDone;
    }

    public void RestoreUpdate()
    {
        if (cam == null) return;

        cam.transform.position = Vector3.Lerp(cam.transform.position, basePosition, Time.deltaTime * followLerpSpeed);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, baseOrthoSize, Time.deltaTime * zoomLerpSpeed);

        if (uiCam != null)
        {
            uiCam.transform.position = cam.transform.position;
            uiCam.orthographicSize = cam.orthographicSize;
        }
    }
}
