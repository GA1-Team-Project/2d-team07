using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Color_Test : MonoBehaviour
{
    public FlexibleColorPicker fcp;
    public List<Image> images;
    public List<TMPro.TextMeshProUGUI> texts;

    private void Update()
    {
        SetColor(fcp.color);
    }

    public void SetColor(Color targetColor)
    {
        foreach (var mat in texts)
        {
            if (mat != null) // Skip null texts
                mat.color = targetColor;
        }

        foreach (var mat in images)
        {
            if (mat != null) // Skip null images
                mat.color = targetColor;
        }
    }
}