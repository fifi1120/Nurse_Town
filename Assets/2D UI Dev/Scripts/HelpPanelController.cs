using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HelpPanelController : MonoBehaviour
{
    public GameObject helpPanel;  // The pop-up window
    public TextMeshProUGUI helpText;  // The text inside the panel
    public string instructionsText;  // Stores Instructions
    public string backgroundText;  // Stores Background info

    void Start()
    {
        helpPanel.SetActive(false); // Make sure it's hidden at the start
    }

    public void ShowHelpPanel()
    {
        helpPanel.SetActive(true);
        ShowInstructions();  // Default to Instructions
    }

    public void HideHelpPanel()
    {
        helpPanel.SetActive(false);
    }

    public void ShowInstructions()
    {
        helpText.text = instructionsText;
    }

    public void ShowBackground()
    {
        helpText.text = backgroundText;
    }
}
