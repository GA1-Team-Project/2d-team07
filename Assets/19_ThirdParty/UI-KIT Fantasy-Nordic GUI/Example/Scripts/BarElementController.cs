using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BarElementController : MonoBehaviour
{
    [System.Serializable]
    public class HiddenPanelGroup
    {
        public List<GameObject> elementsToHide;
        public List<int> panelIndexes;
    }

    public List<BarElement> barElements;
    public List<GameObject> panels;
    public Button nextButton;
    public Button backButton;

    public List<HiddenPanelGroup> hiddenElements;
    public List<ColorPickerActivator> colorPickers;

    private int currentIndex = 0;

    void Start()
    {
        UpdateBarElements();
        UpdatePanelsView();
    }

    public void OnNextButtonClick()
    {
        currentIndex = (currentIndex + 1) % barElements.Count; // Cycle forward
        UpdateBarElements();
        UpdatePanelsView();
    }

    public void OnBackButtonClick()
    {
        currentIndex = (currentIndex - 1 + barElements.Count) % barElements.Count; // Cycle backward
        UpdateBarElements();
        UpdatePanelsView();
    }

    private void UpdateBarElements()
    {
        for (int i = 0; i < barElements.Count; i++)
            barElements[i].SetState(i == currentIndex);
    }

    private void UpdatePanelsView()
    {
        // Show/Hide panels based on current index
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == currentIndex);
        }

        // Hide elements in panels based on the hiddenElements list
        foreach (var group in hiddenElements)
        {
            bool shouldHide = group.panelIndexes.Contains(currentIndex);

            foreach (var element in group.elementsToHide)
            {
                element.SetActive(!shouldHide);
            }
        }

        foreach (var item in colorPickers)
        {
            item.ResetColor();
            if (item.gameObject.activeSelf)
                item.DisableColorPicker();
        }
    }
}
