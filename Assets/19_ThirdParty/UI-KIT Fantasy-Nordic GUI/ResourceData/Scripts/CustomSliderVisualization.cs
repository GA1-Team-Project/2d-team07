using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Slider))]
public class CustomSliderVisualization : MonoBehaviour
{
    public RectTransform imageTransform;

    float visiblePositionX;
    float hiddenPositionX = -200f;

    void Start()
    {
        if (imageTransform != null)
            visiblePositionX = imageTransform.anchoredPosition.x;

        hiddenPositionX = -imageTransform.rect.width;

        var slider = gameObject.GetComponent<UnityEngine.UI.Slider>();

        slider.onValueChanged.AddListener(UpdateImagePosition);
        UpdateImagePosition(slider.value);
    }

    void UpdateImagePosition(float value)
    {
        if (imageTransform != null)
        {
            float newX = Mathf.Lerp(hiddenPositionX, visiblePositionX, value);
            imageTransform.anchoredPosition = new Vector2(newX, imageTransform.anchoredPosition.y);
        }
    }
}
