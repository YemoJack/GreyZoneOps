using UnityEngine;
using ZMUIFrameWork;

public class LoadingWindow : WindowBase
{
    public LoadingWindowDataComponent dataCompt;

    private const float MaxProgressWidth = 1000f;

    #region Lifecycle
    public override void OnAwake()
    {
        dataCompt = gameObject.GetComponent<LoadingWindowDataComponent>();
        dataCompt.InitComponent(this);
        base.OnAwake();
        mDisableAnim = true;
    }

    public override void OnShow()
    {
        base.OnShow();
        SetProgressWidth(0f);
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    #endregion

    public void SetProgress01(float progress01)
    {
        SetProgressWidth(Mathf.Lerp(0f, MaxProgressWidth, Mathf.Clamp01(progress01)));
    }

    public void SetProgressWidth(float width)
    {
        if (dataCompt == null || dataCompt.ProgressImage == null)
        {
            return;
        }

        var rect = dataCompt.ProgressImage.rectTransform;
        var size = rect.sizeDelta;
        size.x = Mathf.Clamp(width, 0f, MaxProgressWidth);
        rect.sizeDelta = size;
    }
}
