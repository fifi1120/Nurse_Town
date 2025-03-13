using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB; // Standalone File Browser for file upload (PC/Mac)
using System.IO;

public class AssessmentManager : MonoBehaviour
{
    public TextMeshProUGUI resultText;  // Text box to display results

    void Start()
    {
        resultText.text = "";  // Start with empty text
    }

    // Formative Assessment: Show Text Immediately
    public void ShowFormativeAssessment()
    {
        resultText.text = "Formative Assessment: \nThis assessment helps students learn by providing feedback.";
    }

    // Summative Assessment: File Upload & Show Text
    public void StartSummativeAssessment()
    {
        var extensions = new[] { new ExtensionFilter("PDF Files", "pdf"), new ExtensionFilter("All Files", "*"), };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Upload Your Assessment", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string filePath = paths[0];
            resultText.text = "Upload Successful! Summative Assessment Uploaded:\n" + Path.GetFileName(filePath) + "\nThis assessment evaluates overall performance.";
        }
    }
}
