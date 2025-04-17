using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;
using System.IO;
using Xceed.Words.NET;  // Add this after importing DocX

public class AssessmentManager : MonoBehaviour
{
    public TextMeshProUGUI resultText;

    void Start()
    {
        resultText.text = "";
    }

    public void ShowFormativeAssessment()
    {
        resultText.text = "Formative Assessment:\nThis assessment helps students learn by providing feedback.";
    }

    public void StartSummativeAssessment()
    {
        var extensions = new[]
        {
            new ExtensionFilter("Word Documents", "docx"),
            new ExtensionFilter("All Files", "*"),
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Upload Your Assessment", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string filePath = paths[0];
            string fileName = Path.GetFileName(filePath);

            string content = ParseDocx(filePath);
            resultText.text = $"Upload Successful! Summative Assessment Uploaded:\n{fileName}\n\nContent:\n{content}";
        }
    }

    private string ParseDocx(string filePath)
    {
        try
        {
            using (DocX document = DocX.Load(filePath))
            {
                return document.Text;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error reading docx file: " + e.Message);
            return "Failed to parse the document.";
        }
    }
}
