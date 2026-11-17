using UnityEngine;

namespace AnchorMaster.Workshop
{
    public sealed class AnchorWorkshopBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 60;
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            var root = new GameObject("AnchorPuzzleGame");
            root.AddComponent<AnchorPuzzleGame>();
        }
    }
}
