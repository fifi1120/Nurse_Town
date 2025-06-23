using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private string scoringPrompt = "";
    private string currentScenario = "";
    private List<ConversationTurn> conversationTurns = new List<ConversationTurn>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E key pressed — submitting full evaluation.");
            SubmitEvaluation();
        }
    }

    public void Initialize(string scenario)
    {
        currentScenario = scenario;
        LoadScoringPrompt();
    }

    private void LoadScoringPrompt()
    {
        string promptPath = Path.Combine(Application.streamingAssetsPath, "Prompts", currentScenario, "scoringPrompt.txt");
        if (!File.Exists(promptPath))
        {
            Debug.LogError("Scoring prompt file not found: " + promptPath);
            scoringPrompt = "";
            return;
        }
        scoringPrompt = File.ReadAllText(promptPath);
        Debug.Log("Scoring prompt loaded successfully.");
    }

    public void RecordTurn(string patientResponse, string nurseResponse)
    {
        conversationTurns.Add(new ConversationTurn
        {
            Patient = patientResponse,
            Nurse = nurseResponse
        });
        Debug.Log($"Turn {conversationTurns.Count} recorded.");
    }

    public void SubmitEvaluation()
    {
        if (conversationTurns.Count == 0)
        {
            Debug.LogWarning("No conversation turns recorded.");
            return;
        }
        StartCoroutine(EvaluateFullConversationCoroutine());
    }

    private IEnumerator EvaluateFullConversationCoroutine()
    {
        if (string.IsNullOrEmpty(scoringPrompt))
        {
            Debug.LogWarning("Scoring prompt not loaded.");
            yield break;
        }

        StringBuilder conversationBuilder = new StringBuilder();
        for (int i = 0; i < conversationTurns.Count; i++)
        {
            conversationBuilder.AppendLine($"Turn {i + 1}:");
            conversationBuilder.AppendLine($"Patient: \"{conversationTurns[i].Patient}\"");
            conversationBuilder.AppendLine($"Nursing Student: \"{conversationTurns[i].Nurse}\"");
            conversationBuilder.AppendLine();
        }

        string fullPrompt = $"{scoringPrompt}\n\nNow analyze the following full simulated conversation between the patient and nursing student:\n{conversationBuilder}";
        Debug.Log($"Prompt length: {fullPrompt.Length} characters");
        Debug.Log($"Estimated tokens (rough): {fullPrompt.Length / 4} tokens");
        var requestBody = new
        {
            model = "gpt-4",
            messages = new List<Dictionary<string, string>>()
            {
                new Dictionary<string, string>() { { "role", "user" }, { "content", fullPrompt } }
            },
            temperature = 0.0,
            max_tokens = 1500
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);

        var request = new UnityWebRequest(OpenAIRequest.Instance.apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + OpenAIRequest.Instance.apiKey);

        Debug.Log("Submitting full conversation for evaluation...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("OpenAI Request Error: " + request.error);
            yield break;
        }

        var jsonResponse = JObject.Parse(request.downloadHandler.text);
        string responseContent = jsonResponse["choices"][0]["message"]["content"].ToString();

        try
        {
            var evaluation = JsonConvert.DeserializeObject<DynamicEvaluationResult>(responseContent);
            Debug.Log("===== FINAL EVALUATION =====");
            foreach (var criterion in evaluation.criteria)
            {
                Debug.Log($"[{criterion.name}] Score: {criterion.score}/{criterion.maxScore} — {criterion.explanation}");
            }
            Debug.Log($"Total Score: {evaluation.totalScore}");
            Debug.Log($"Performance Level: {evaluation.performanceLevel}");
            Debug.Log($"Overall Summary: {evaluation.overallExplanation}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse evaluation JSON: " + ex.Message);
        }
    }
}

[Serializable]
public class ConversationTurn
{
    public string Patient;
    public string Nurse;
}

[Serializable]
public class DynamicEvaluationResult
{
    public List<CriterionScore> criteria;
    public int totalScore;
    public string performanceLevel;
    public string overallExplanation;
}

[Serializable]
public class CriterionScore
{
    public string name;
    public int score;
    public int maxScore;
    public string explanation;
}
