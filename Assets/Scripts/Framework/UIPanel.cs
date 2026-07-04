using UnityEngine;

/// <summary>
/// UI面板基类
/// 所有UI面板都应继承此类
/// </summary>
public abstract class UIPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    public string panelName;
    public bool useAnimation = false;

    protected CanvasGroup canvasGroup;
    protected bool isShowing = false;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && useAnimation)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// 显示面板时调用
    /// </summary>
    public virtual void OnShow()
    {
        isShowing = true;
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// 隐藏面板时调用
    /// </summary>
    public virtual void OnHide()
    {
        isShowing = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 刷新面板数据
    /// </summary>
    public virtual void OnRefresh() { }

    /// <summary>
    /// 面板是否正在显示
    /// </summary>
    public bool IsShowing()
    {
        return isShowing;
    }
}
