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

    [Header("UI Integration")]
    public ClipboardReportDisplay clipboardDisplay; // UI显示组件

    // Store final evaluation results
    private DynamicEvaluationResult finalEvaluation;
    private bool evaluationCompleted = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Enhanced search for ClipboardReportDisplay
        if (clipboardDisplay == null)
        {
            Debug.Log("Searching for ClipboardReportDisplay component...");

            // Method 1: Standard FindObjectOfType
            clipboardDisplay = FindObjectOfType<ClipboardReportDisplay>();

            // Method 2: Search in all GameObjects (including inactive and in prefabs)
            if (clipboardDisplay == null)
            {
                Debug.Log("Standard search failed, trying comprehensive search...");
                ClipboardReportDisplay[] allDisplays = Resources.FindObjectsOfTypeAll<ClipboardReportDisplay>();

                foreach (var display in allDisplays)
                {
                    // Skip if it's a prefab asset (not an instance in scene)
                    if (display.gameObject.scene.name != null)
                    {
                        clipboardDisplay = display;
                        Debug.Log($"Found ClipboardReportDisplay in prefab instance: {display.gameObject.name}");
                        break;
                    }
                }
            }

            if (clipboardDisplay == null)
            {
                Debug.LogError("ClipboardReportDisplay component not found in scene! Make sure it exists in the current scene.");
            }
            else
            {
                Debug.Log($"ClipboardReportDisplay found and integrated: {clipboardDisplay.gameObject.name}");
            }
        }
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
            finalEvaluation = JsonConvert.DeserializeObject<DynamicEvaluationResult>(responseContent);
            evaluationCompleted = true;

            // Console输出详细评估
            Debug.Log("===== FINAL EVALUATION =====");
            foreach (var criterion in finalEvaluation.criteria)
            {
                Debug.Log($"[{criterion.name}] Score: {criterion.score}/{criterion.maxScore} — {criterion.explanation}");
            }
            Debug.Log($"Total Score: {finalEvaluation.totalScore}");
            Debug.Log($"Performance Level: {finalEvaluation.performanceLevel}");
            Debug.Log($"Overall Summary: {finalEvaluation.overallExplanation}");

            // E键同时触发UI显示
            if (clipboardDisplay != null)
            {
                Debug.Log("E key also triggering UI display");
                Debug.Log($"ClipboardDisplay found: {clipboardDisplay.name}");
                ConvertAndDisplayInUI();
            }
            else
            {
                Debug.LogError("ClipboardDisplay is NULL! Please assign it in ScoreManager Inspector.");
            }

            Debug.Log("Evaluation completed! Console and UI reports generated.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse evaluation JSON: " + ex.Message);
        }
    }

    // 将复杂评估数据转换为UI格式并显示
    private void ConvertAndDisplayInUI()
    {
        if (finalEvaluation == null)
        {
            Debug.LogError("No evaluation data to display");
            return;
        }

        // 转换数据格式
        List<string> positiveReasons = new List<string>();
        List<string> improvementReasons = new List<string>();

        foreach (var criterion in finalEvaluation.criteria)
        {
            float scorePercent = (float)criterion.score / criterion.maxScore;

            if (scorePercent >= 0.7f) // 70%及以上视为优点
            {
                positiveReasons.Add($"{criterion.name}: {criterion.explanation}");
            }
            else // 70%以下视为改进点
            {
                improvementReasons.Add($"{criterion.name}: {criterion.explanation}");
            }
        }

        // 使用OpenAI返回的真实总分，而不是重新计算
        int actualTotalScore = finalEvaluation.totalScore; // 使用OpenAI返回的分数

        // 计算最大可能分数（用于UI显示格式）
        int maxPossibleScore = 0;
        foreach (var criterion in finalEvaluation.criteria)
        {
            maxPossibleScore += criterion.maxScore;
        }

        Debug.Log($"Converting to UI format:");
        Debug.Log($"Actual Total Score: {actualTotalScore}/{maxPossibleScore}");
        Debug.Log($"Positive aspects: {positiveReasons.Count}");
        Debug.Log($"Improvement areas: {improvementReasons.Count}");
        Debug.Log($"Interaction count: {conversationTurns.Count}");

        // 详细调试clipboardDisplay
        if (clipboardDisplay == null)
        {
            Debug.LogError("ClipboardDisplay is NULL in ConvertAndDisplayInUI!");
            return;
        }

        Debug.Log($"About to call PrepareReportAndDisplay on {clipboardDisplay.name}");

        // 触发UI显示，使用真实的总分和最大分数
        clipboardDisplay.PrepareReportAndDisplay(
            actualTotalScore,
            maxPossibleScore, // 传递最大分数
            positiveReasons,
            improvementReasons,
            conversationTurns.Count
        );

        Debug.Log("PrepareReportAndDisplay called successfully");
    }

    // 公共访问器方法
    public int GetConversationCount()
    {
        return conversationTurns.Count;
    }

    public bool CanViewReport()
    {
        return conversationTurns.Count >= 5;
    }

    public bool IsEvaluationCompleted()
    {
        return evaluationCompleted;
    }

    // 测试方法：模拟完整的复杂对话
    [ContextMenu("Simulate Complete Complex Conversation")]
    public void SimulateCompleteComplexConversation()
    {
        // 清除现有数据
        conversationTurns.Clear();
        finalEvaluation = null;
        evaluationCompleted = false;

        // 添加测试对话轮次
        RecordTurn("Hello, I'm experiencing chest pain", "Hello, I'm here to help you. Can you tell me more about your chest pain?");
        RecordTurn("It started about an hour ago, sharp pain", "On a scale of 1-10, how would you rate your pain level?");
        RecordTurn("I'd say it's about a 7", "I understand that must be very uncomfortable. Let me get some more information from you.");
        RecordTurn("It hurts when I breathe deeply", "Thank you for that information. I'm going to do a quick assessment now.");
        RecordTurn("Okay, what do you need me to do?", "Just try to stay comfortable while I check your vital signs and listen to your heart and lungs.");

        // 模拟复杂评估数据（24分制 - 匹配你的scoring prompt）
        finalEvaluation = new DynamicEvaluationResult
        {
            criteria = new List<CriterionScore>
            {
                new CriterionScore {
                    name = "Rapport Building & Empathy",
                    score = 3,
                    maxScore = 4,
                    explanation = "Establishes rapport; demonstrates empathy; uses supportive behaviors."
                },
                new CriterionScore {
                    name = "Communication Strategies",
                    score = 3,
                    maxScore = 4,
                    explanation = "Uses appropriate communication supports to facilitate understanding."
                },
                new CriterionScore {
                    name = "Information Gathering (Case History)",
                    score = 2,
                    maxScore = 4,
                    explanation = "Misses important points; some disorganization or vagueness in questioning."
                },
                new CriterionScore {
                    name = "Clinical Reasoning",
                    score = 2,
                    maxScore = 4,
                    explanation = "Basic understanding; some missed opportunities for clinical insight."
                },
                new CriterionScore {
                    name = "Professionalism",
                    score = 3,
                    maxScore = 4,
                    explanation = "Maintains professionalism with minor lapses in communication."
                },
                new CriterionScore {
                    name = "Responsiveness to Feedback",
                    score = 3,
                    maxScore = 4,
                    explanation = "Accepts feedback and expresses understanding appropriately."
                }
            },
            totalScore = 16, // 3+3+2+2+3+3 = 16/24
            performanceLevel = "Developing", // 基于16/24的评级
            overallExplanation = "The student demonstrates solid foundational skills but needs improvement in systematic information gathering and clinical reasoning to enhance patient care quality."
        };

        evaluationCompleted = true;

        Debug.Log("=== COMPLEX CONVERSATION SIMULATION COMPLETED ===");
        Debug.Log("Simulated conversation with 5 turns and complex criteria evaluation.");
        Debug.Log("Press Enter to switch to clipboard view, then press E to generate reports!");
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