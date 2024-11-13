using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class BodyMove : MonoBehaviour
{
    private string chatApiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;
    private List<Dictionary<string, string>> chatMessages;
    private CharacterAnimationController animationController;

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log("APIKey:" + apiKey);
        animationController = GetComponent<CharacterAnimationController>();
        InitializeChat();
    }

    private void InitializeChat()
    {
        string emotionInstructions = 
            "IMPORTANT: You must end EVERY response with one of these emotion codes: [0], [1], or [2]\n" +
            "- Use [0] for neutral responses or statements\n" +
            "- Use [1] for responses involving pain, discomfort, symptoms, or negative feelings\n" +
            "- Use [2] for positive responses, gratitude, or when feeling better\n";

        string baseInstructions = 
            "You are a patient NPC in a hospital, interacting with a nursing student. " +
            "Respond with brief, natural answers about your symptoms or feelings. " +
            "Keep responses concise and focused on your condition.";

        chatMessages = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", $"{baseInstructions}\n\n{emotionInstructions}" }
            },
            new Dictionary<string, string>
            {
                { "role", "assistant" },
                { "content", "Hi, nurse, I am feeling pain in my chest[1]" }
            }
        };
        PrintChatMessage(chatMessages);
    }

    public void PlayerResponds(string playerMessage)
    {
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", playerMessage } });
        StartCoroutine(SendChatRequest());
        PrintChatMessage(chatMessages);
        
    }

    private IEnumerator SendChatRequest()
    {
        var requestObject = new
        {
            model = "gpt-4",
            messages = chatMessages,
            max_tokens = 150,
            temperature = 0.7f
        };

        string requestBody = JsonConvert.SerializeObject(requestObject);
        
        using (UnityWebRequest request = new UnityWebRequest(chatApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var jsonResponse = JObject.Parse(request.downloadHandler.text);
                var messageContent = jsonResponse["choices"][0]["message"]["content"].ToString();
                
                UpdateAnimation(messageContent);
                chatMessages.Add(new Dictionary<string, string>() 
                { 
                    { "role", "assistant" }, 
                    { "content", messageContent } 
                });
                PrintChatMessage(chatMessages);
            }
            else
            {
                Debug.LogError($"Chat API Error: {request.error}");
            }
        }
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