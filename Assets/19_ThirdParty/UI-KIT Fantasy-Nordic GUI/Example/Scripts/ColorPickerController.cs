using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ColorPickerActivator : MonoBehaviour
{
    public Button ColorPickerButton;
    public GameObject ColorPicker;

    void Awake()
    {
        if (ColorPickerButton == null || ColorPicker == null)
        {
            Debug.LogError("ColorPickerButton or ColorPicker не !");
            return;
        }

        ColorPicker.SetActive(false);

        ColorPickerButton.onClick.AddListener(ToggleColorPicker);
        ColorPickerButton.onClick.AddListener(delegate {Debug.Log($"Color picker button pressed for {ColorPicker.name}");});

    }

    void ToggleColorPicker()
    {
        ColorPicker.SetActive(!ColorPicker.activeSelf);
    }

    public void DisableColorPicker()
    {
        if (ColorPicker.activeSelf)
        {
            ToggleColorPicker();
            ResetColor();
            Debug.Log($"Color picker state set to {false} for {ColorPicker.name}");
        }
    }

    public void ResetColor()
        => ColorPicker.GetComponent<Color_Test>()?.SetColor(Color.white);
}
