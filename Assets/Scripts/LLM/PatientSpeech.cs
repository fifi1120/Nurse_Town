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
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;
    private ScoringSystem scoringSystem = new ScoringSystem(); // For scoring system

    private List<Dictionary<string, string>> chatMessages;

    // Variables for multiple patients
    private List<string> patientInstructionsList;
    private string patient1Instructions;
    private string patient2Instructions;
    private string patient3Instructions;

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

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log("Using APIKey:" + apiKey);

        // Initialize patient instructions and chat
        InitializePatientInstructions();
        InitializeChat();

        animationController = GetComponent<CharacterAnimationController>();
        bloodEffectController = GetComponent<BloodEffectController>();
    }

    private void InitializePatientInstructions()
    {
        // Base instructions for the patient's medical history and symptoms
        string baseInstructions = @"
            You are strictly playing the role of Mrs. Johnson. 
            Background:
            - Mrs. Johnson is a 62-year-old female admitted to the hospital with severe headache and dizziness.
            - She has a 5-year history of hypertension and occasionally misses doses due to forgetfulness.
            - Family history includes hypertension and heart disease (mother and brother).
            - Works as a school teacher and lives with her husband.
            - Leads a sedentary lifestyle and enjoys watching TV in her spare time.

            Clinical Presentation and Responses:
            - Symptoms: Constant, throbbing headache in the temples; dizziness worsens upon standing quickly. No vision changes, nausea, or confusion.
            - Medical History: Openly shares hypertension history; mentions sometimes forgetting medication.
            - Current Medications: Tries to recall antihypertensive medication name (e.g., 'I think it's called lisinopril...').
            - Lifestyle: Admits to a sedentary routine; doesn't exercise regularly; occasionally eats salty foods and drinks coffee daily.
            - Family History: Mentions mother and brother with high blood pressure; adds that mother had heart disease if prompted.
            ";

        // Patient 1: Normal personality (original version)
        patient1Instructions = baseInstructions + @"

            Tone and Personality:
            - Polite and cooperative tone; generally compliant and concerned about her health.
            - Expresses mild anxiety about current symptoms; headaches and dizziness are more severe than usual.
            - Occasionally shows forgetfulness or hesitation when recalling medication details.

            Emotional Response:
            - Displays concern when discussing family history but reassures that such symptoms are unusual for her.
            - Open to lifestyle changes or medication adherence strategies but hesitant about drastic changes.

            As Mrs. Johnson, please initiate the conversation by greeting the nurse and mentioning how you're feeling. 
            If off-topic, guide the conversation back to your health concerns.
            Please keep responses concise.
            ";

        // Patient 2: Speaks very little, gives vague descriptions
        patient2Instructions = baseInstructions + @"

            Tone and Personality:
            - Reserved and speaks very little.
            - Provides brief and sometimes vague answers, saying something like 'i don't remember.../i am not sure'
            - Requires the nurse to ask more probing questions to obtain information.

            Emotional Response:
            - Appears indifferent or slightly detached.
            - Does not volunteer additional information unless specifically asked.
            - May give one-word answers or simple acknowledgments.

            As Mrs. Johnson, please initiate the conversation by saying minimal words like 'hi nurse'.
            ";

        // Patient 3: Emotionally excited, uses phrases like 'I feel I am dying. I cannot stand it!!!!!!'
        patient3Instructions = baseInstructions + @"

            Tone and Personality:
            - Highly emotional and anxious.
            - Responses are intense and be exaggerated.
            - Frequently uses emotional phrases like 'I feel I am dying. I cannot stand it!!!!!!'

            Emotional Response:
            - Displays significant anxiety and distress about her condition.
            - May interrupt the nurse or speak rapidly.
            - Finds it difficult to be consoled.

            As Mrs. Johnson, please initiate the conversation by expressing your extreme distress.
            ";

        patientInstructionsList = new List<string>()
        {
            patient1Instructions,
            patient2Instructions,
            patient3Instructions
        };
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
            - Use [10] for blood pressureing, if the nurse asks to measure your blood pressure";

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
            // Animation update is handled in TTSManager
        }
    }

    private string BuildRequestBody()
    {
        var requestObject = new
        {
            model = "gpt-4-turbo-preview",
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