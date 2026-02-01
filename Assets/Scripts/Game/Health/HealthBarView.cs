using UnityEngine;
using UnityEngine.UI;

public class HealthBarView : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Text valueText;

    public void SetValue(float current, float max)
    {
        max = Mathf.Max(1f, max);
        current = Mathf.Clamp(current, 0f, max);
        var normalized = current / max;

        if (fillImage != null)
        {
            fillImage.fillAmount = normalized;
        }

        if (valueText != null)
        {
            valueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }
}
