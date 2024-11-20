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

    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;
    private CharacterAnimationController animationController;

    private List<Dictionary<string, string>> chatMessages;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // If you need to maintain this during scene transitions, please uncomment the following line.
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
        // Initialize chat with Mrs. Johnson's background
        animationController = GetComponent<CharacterAnimationController>();
        InitializeChat();
    }


    private void InitializeChat()
    {


        string rolePlayingInstructions = @"
            You are strictly playing the role of Mrs. Johnson. 
            Background:
            - Mrs. Johnson is a 62-year-old female who was admitted to the hospital with severe headache and dizziness.
            - She has a 5-year history of hypertension and has been on antihypertensive medications, though she occasionally misses doses due to forgetfulness.
            - Family history includes hypertension and heart disease (mother and brother).
            - She works as a school teacher and lives with her husband.
            - She has a sedentary lifestyle and enjoys watching TV in her spare time.

            Tone and Personality:
            - Speak with a polite and cooperative tone, as Mrs. Johnson is generally compliant and concerned about her health.
            - Express some mild anxiety about her current symptoms, as the headache and dizziness are more severe than what she usually experiences.
            - Occasionally show a bit of forgetfulness or hesitation when recalling specific medication details, indicating a realistic portrayal of a patient who isn’t fully adherent to her prescribed regimen.

            Clinical Presentation and Responses:
            - Symptoms: Describe a constant, throbbing headache, primarily in the temples, with dizziness that worsens when standing up quickly. There are no signs of vision changes, nausea, or confusion.
            - Medical History: Openly share your past medical history of hypertension when asked, and mention that you sometimes forget to take your medication, especially when you’re busy at work.
            - Current Medications: Try to recall your antihypertensive medication name (e.g., 'I think it's called lisinopril... I'm not sure about the dosage'). If prompted, mention you haven’t made any changes to your medication recently.
            - Lifestyle: When asked about lifestyle habits, admit to a sedentary routine and report that you don’t exercise regularly. You occasionally eat salty foods and drink coffee daily.
            - Family History: Mention that both your mother and brother have had high blood pressure. If prompted further, add that your mother also had heart disease.

            Emotional Response:
            - Display some concern when discussing the family history of heart disease, but reassure the nurse that you usually don’t experience symptoms like this.
            - When the nurse suggests lifestyle changes or medication adherence strategies, be open but express some hesitation regarding making drastic changes to your routine.

            As Mrs. Johnson, please initiate the conversation by greeting the nurse and mentioning how you're feeling. Please try to be concise.
            ";      
        string emotionInstructions = @"
            IMPORTANT: You must end EVERY response with one of these emotion codes: [0], [1], or [2]\n
            - Use [0] for neutral responses or statements\n
            - Use [1] for responses involving pain, discomfort, symptoms, or negative feelings\n
            - Use [2] for positive responses, gratitude, or when feeling better\n";

        chatMessages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", $"{rolePlayingInstructions}\n\n{emotionInstructions}" }
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
            UpdateAnimation(messageContent);
            
            chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", messageContent } });
            PrintChatMessage(chatMessages);

            // Play the response using TTS
            if (TTSManager.Instance != null)
            {
                // Debug.Log("Attempting to play TTS for the response");
                TTSManager.Instance.ConvertTextToSpeech(messageContent);
            }
            else
            {
                Debug.LogError("TTSManager instance not found.");
            }
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
    private void UpdateAnimation(string message)
    {
        Match match = Regex.Match(message, @"\[([012])\]$");
        if (match.Success)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            switch(emotionCode)
            {
                case 0:
                    animationController.PlayIdle();
                    break;
                case 1:
                    animationController.PlayHeadPain();
                    Debug.Log("changing to pain");
                    break;
                case 2:
                    animationController.PlayHappy();
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"No emotion code found: {message}");
            animationController.PlayIdle();
        }
    }
    public static void PrintChatMessage(List<Dictionary<string, string>> messages)
    {
        Debug.Log("══════════════ Chat Messages Log ══════════════");
        
        foreach (var message in messages)
        {
            string role = message["role"];
            string content = message["content"];
            
            // Extract emotion code if present
            string emotionCode = "";
            var match = System.Text.RegularExpressions.Regex.Match(content, @"\[([012])\]$");
            if (match.Success)
            {
                emotionCode = $" (Emotion: {match.Groups[1].Value})";
            }
            
            Debug.Log($"[{role.ToUpper()}]{emotionCode}\n{content}\n");
        }
        
        Debug.Log("══════════════ End Chat Log ══════════════");
    }
}