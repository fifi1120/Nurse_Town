using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

public class OpenAIRequest : MonoBehaviour
{
    public static OpenAIRequest Instance; // Singleton instance
    public string apiUrl = "https://api.openai.com/v1/chat/completions";
    public string apiKey;
    public string currentScenario = "brocaAphasia"; // New scenario selector
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private ScoringSystem scoringSystem = new ScoringSystem(); // For scoring system
    private EmotionController emotionController;
    private string basePath;
    private List<string> patientInstructionsList = new List<string>();
    private List<Dictionary<string, string>> chatMessages;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Uncomment if you want this object to persist across scenes
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private string LoadPromptFromFile(string fileName)
    {
        string filePath = Path.Combine(basePath, fileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError("Prompt file not found: " + filePath);
            return "";
        }
        return File.ReadAllText(filePath);
    }
    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        basePath = Path.Combine(Application.streamingAssetsPath, "Prompts", currentScenario);
        animationController = GetComponent<CharacterAnimationController>();
        bloodEffectController = GetComponent<BloodEffectController>();
        emotionController = GetComponent<EmotionController>();
        basePath = Path.Combine(Application.streamingAssetsPath, "Prompts", currentScenario);
        // Initialize patient instructions and chat
        InitializePatientInstructions();
        InitializeChat();
    }

    private void InitializePatientInstructions()
    {
        string baseInstructions = LoadPromptFromFile("baseInstructions.txt");
        string caseHistoryPrompt = LoadPromptFromFile("caseHistoryPrompt.txt");
        patientInstructionsList = new List<string>();

        for (int i = 1; i <= 3; i++)
        {
            string patientFile = $"patient{i}.txt";
            string patientSpecific = LoadPromptFromFile(patientFile);
            if (string.IsNullOrEmpty(patientSpecific))
            {
                Debug.LogError("Failed to load patient file: " + patientFile);
                continue;
            }
            string fullPrompt = $"{baseInstructions}\n{caseHistoryPrompt}\n{patientSpecific}";
            patientInstructionsList.Add(fullPrompt);
        }
        if (patientInstructionsList.Count == 0)
        {
            Debug.LogError("No patient instructions loaded for scenario: " + currentScenario);
        }
    }

    private void InitializeChat()
    {
        string emotionInstructions = @"
            IMPORTANT: You must end EVERY response with one of these emotion codes:
            - Use [0] for neutral responses or statements
            - Use [1] for responses involving pain, discomfort, symptoms, or negative feelings
            - Use [2] for positive responses, gratitude, or when feeling better
            - Use [3] for shrugging
            - Use [4] for head nodding
            - Use [5] for head shaking
            - Use [6] for writhing in pain
            - Use [7] for sad
            - Use [8] for arm stretching
            - Use [9] for neck stretching
            - Use [10] for anger";

        // Randomly select a patient instruction
        System.Random rand = new System.Random();
        int patientIndex = rand.Next(patientInstructionsList.Count);
        string selectedPatientInstructions = patientInstructionsList[patientIndex];

        // Combine selected patient instructions with emotion instructions
        chatMessages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", $"{selectedPatientInstructions}\n\n{emotionInstructions}" }
            }
        };

        PrintChatMessage(chatMessages);
        StartCoroutine(PostRequest());
    }

    public void ReceiveNurseTranscription(string transcribedText)
    {
        NurseResponds(transcribedText);
    }

    private void NurseResponds(string nurseMessage)
    {
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", nurseMessage } });
        PrintChatMessage(chatMessages);
        StartCoroutine(PostRequest());

        // Evaluate nurse's response
        scoringSystem.EvaluateNurseResponse(nurseMessage);
    }

    IEnumerator PostRequest()
    {
        Debug.Log("Building request body for chat completion...");

        string requestBody = BuildRequestBody();
        var request = CreateRequest(requestBody);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response Body: " + request.downloadHandler.text);
        }
        else if (request.responseCode == 200)
        {
            var jsonResponse = JObject.Parse(request.downloadHandler.text);
            var messageContent = jsonResponse["choices"][0]["message"]["content"].ToString();

            chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", messageContent } });
            PrintChatMessage(chatMessages);

            // Play the response using TTS
            if (TTSManager.Instance != null)
            {
                TTSManager.Instance.ConvertTextToSpeech(messageContent);
            }
            else
            {
                Debug.LogError("TTSManager instance not found.");
            }
            
            var match = Regex.Match(messageContent, @"\[(\d+)\]$");
            if (!match.Success || emotionController == null) yield break;
            int emotionCode = int.Parse(match.Groups[1].Value);
            emotionController.HandleEmotionCode(emotionCode);
        }
    }

    private string BuildRequestBody()
    {
        var requestObject = new
        {
            // model = "gpt-4-turbo-preview",
            model = "gpt-4",
            messages = chatMessages,
            temperature = 0.7f,
            max_tokens = 1500
        };
        return JsonConvert.SerializeObject(requestObject);
    }

    private UnityWebRequest CreateRequest(string requestBody)
    {
        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        return request;
    }

    public static void PrintChatMessage(List<Dictionary<string, string>> messages)
    {
        if (messages.Count == 0)
            return;

        var latestMessage = messages[messages.Count - 1];
        string role = latestMessage["role"];
        string content = latestMessage["content"];

        // Extract emotion code if present
        string emotionCode = "";
        var match = Regex.Match(content, @"\[(\d+)\]$");
        if (match.Success)
        {
            emotionCode = $" (Emotion: {match.Groups[1].Value})";
        }

        Debug.Log($"[{role.ToUpper()}]{emotionCode}\n{content}\n");
    }
}