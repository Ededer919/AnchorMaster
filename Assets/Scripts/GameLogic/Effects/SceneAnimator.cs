using UnityEngine;

/// <summary>
/// Drives ambient world animations: sun rotation, cloud drift.
/// Attach to the GameWorld root or any persistent GameObject.
/// </summary>
public class SceneAnimator : MonoBehaviour
{
    private Transform sun;
    private Transform[] clouds;
    private float[] cloudSpeeds;
    private float[] cloudStartX;
    private float cloudRange;

    private const float SunRotateSpeed = 8f;
    private const float CloudDriftSpeed = 0.3f;

    void LateUpdate()
    {
        if (sun == null)
        {
            var sunGo = GameObject.Find("Sun");
            if (sunGo != null) sun = sunGo.transform;
        }

        if (sun != null)
            sun.Rotate(0, 0, SunRotateSpeed * Time.deltaTime);

        if (clouds == null || clouds.Length == 0)
        {
            var cloudObjects = GameObject.FindGameObjectsWithTag("Untagged");
            var cloudList = new System.Collections.Generic.List<Transform>();
            foreach (var go in cloudObjects)
            {
                if (go.name.StartsWith("Cloud_"))
                {
                    cloudList.Add(go.transform);
                }
            }
            if (cloudList.Count > 0)
            {
                clouds = cloudList.ToArray();
                cloudSpeeds = new float[clouds.Length];
                cloudStartX = new float[clouds.Length];
                for (int i = 0; i < clouds.Length; i++)
                {
                    cloudSpeeds[i] = CloudDriftSpeed * Random.Range(0.7f, 1.3f);
                    cloudStartX[i] = clouds[i].position.x;
                }
                cloudRange = 15f;
            }
        }

        if (clouds != null)
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                if (clouds[i] == null) continue;
                var pos = clouds[i].position;
                pos.x += cloudSpeeds[i] * Time.deltaTime;
                if (pos.x > cloudStartX[i] + cloudRange)
                    pos.x = cloudStartX[i] - cloudRange;
                else if (pos.x < cloudStartX[i] - cloudRange)
                    pos.x = cloudStartX[i] + cloudRange;
                clouds[i].position = pos;
            }
        }
    }

    void OnDisable()
    {
        sun = null;
        clouds = null;
    }
}
