using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Xceed.Words.NET;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class AssessmentManager : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    private string apiKey;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    void Start()
    {
        apiKey = LoadApiKeyFromEnv();
        Debug.Log("Using API Key: " + apiKey);
        resultText.text = "";
    }

    // Load API key directly from a .env file
    private string LoadApiKeyFromEnv()
    {
        string filePath = Path.Combine(Application.dataPath, "../.env");
        Debug.Log("Reading .env from: " + filePath);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning(".env file not found.");
            return null;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim() == "OPENAI_API_KEY")
            {
                return parts[1].Trim();
            }
        }

        Debug.LogWarning("OPENAI_API_KEY not found in .env file.");
        return null;
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
            new ExtensionFilter("All Files", "*")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Upload Your Assessment", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string filePath = paths[0];
            string fileName = Path.GetFileName(filePath);
            string content = ParseDocx(filePath);

            if (!string.IsNullOrEmpty(content))
            {
                resultText.text = $"Upload Successful! Summative Assessment Uploaded:\n{fileName}\n\nEvaluating...";
                StartCoroutine(SendToGPT4o(content));
            }
            else
            {
                resultText.text = "Failed to parse the document content.";
            }
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
            Debug.LogError("Error reading DOCX file: " + e.Message);
            return null;
        }
    }

    private IEnumerator SendToGPT4o(string content)
    {
        var messages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", "You are a nursing educator evaluating a student's root cause analysis report. Provide constructive feedback on accuracy, clarity, and the strength of recommendations. Keep it concise and professional." }
            },
            new Dictionary<string, string>()
            {
                { "role", "user" },
                { "content", summativePrompt }
            }
        };

        var requestBody = new
        {
            model = "gpt-4o",
            messages = messages,
            temperature = 0.5f,
            max_tokens = 1000
        };

        string json = JsonConvert.SerializeObject(requestBody);
        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("GPT Request Error: " + request.error);
            resultText.text = "Evaluation failed: " + request.error;
        }
        else
        {
            var response = JObject.Parse(request.downloadHandler.text);
            string gptFeedback = response["choices"][0]["message"]["content"].ToString();
            resultText.text = "GPT Evaluation:\n\n" + gptFeedback;
        }
    }

    private readonly string summativePrompt = @"
You are an expert nurse educator evaluating a student's written Root Cause Analysis (RCA) report based on a simulated ICU error interview. Use the following rubric to score and comment on the student's performance.

Evaluate the following criteria (score each out of 10, with reasons if points were deducted):

1. Clarity of Problem Statement:
- Does the student clearly describe what happened, when it happened, and why it was significant?

2. Identification of Causes:
- Immediate Causes: Are direct causes (e.g., misreading the wristband) correctly identified?
- Contributing Factors: Are secondary issues (e.g., fatigue, communication failures) thoroughly analyzed?

3. Systemic Issues Analysis:
- Are broader systemic failures addressed, such as:
  - Poor workflow or unclear responsibilities
  - Gaps in protocols or staff training
  - Communication or leadership breakdowns during the code?

4. Use of Interview Evidence:
- Does the student accurately incorporate and reference interview responses?
- Are conflicting or differing perspectives from staff analyzed?

5. Proposed Solutions and Preventive Measures:
- Are recommendations practical, specific, and targeted at root causes?
- Do they include system-level fixes (e.g., protocol changes, staff training) beyond individual errors?

6. Structure and Presentation:
- Is the RCA organized clearly (e.g., SBAR format)?
- Is the language professional, concise, and analytical?

At the end, provide:
- Overall Score (out of 10)
- Strengths (e.g., strong use of evidence, realistic recommendations)
- Areas for Improvement (e.g., missed causes, unclear statements)
- Suggestions (e.g., ways to deepen systemic analysis or improve clarity)

Be objective and constructive. Do not add any made-up interview data or details that were not in the student's submission.";

}
