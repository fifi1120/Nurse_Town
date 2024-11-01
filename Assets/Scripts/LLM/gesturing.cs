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
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;
    private List<Dictionary<string, string>> chatMessages;
    private CharacterAnimationController animationController;

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        animationController = GetComponent<CharacterAnimationController>();
        Debug.Log("APIKey:" + apiKey);
        Debug.Log("Script started. Starting initial conversation...");
        
        // 0 -> idle
        // 1 -> head pain
        // 2 -> happy
        string emotionInstructions = 
            "IMPORTANT: You must end EVERY response with one of these emotion codes: [0], [1], or [2]\n" +
            "- Use [0] for neutral responses or statements\n" +
            "- Use [1] for responses involving pain, discomfort, symptoms, or negative feelings\n" +
            "- Use [2] for positive responses, gratitude, or when feeling better\n\n" +
            "Examples:\n" +
            "- If describing symptoms: 'My chest feels very tight[1]'\n" +
            "- If expressing gratitude: 'Thank you for helping me[2]'\n" +
            "- If making a neutral statement: 'I've been here since morning[0]'\n\n";

        string baseInstructions = 
            "You are strictly playing the role of a patient NPC in a hospital. " +
            "You will be interacting with a user, who is a nursing student. " +
            "As a patient NPC, you are not allowed to ask any questions or provide any assistance. " +
            "You must respond with short answers that describe your symptoms or feelings based on the player's input.\n\n";

        chatMessages = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", baseInstructions + emotionInstructions + "Start by saying: 'Hi, nurse, I am feeling pain in my chest[1]'" }
            },
            new Dictionary<string, string>
            {
                { "role", "assistant" },
                { "content", "Hi, nurse, I am feeling pain in my chest[1]" }
            },
            new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", "From level 1 to 10, how would you rate your pain?" }
            }
        };

        StartCoroutine(PostRequest());
    }

    public void PlayerResponds(string playerMessage)
    {
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", playerMessage } });
        StartCoroutine(PostRequest());
    }

    private void UpdateAnimation(string message)
    {
        Match match = Regex.Match(message, @"\[([012])\]$");
        if (match.Success)
        {
            // int emotionCode = int.Parse(match.Groups[1].Value);
            int emotionCode = 1;
            Debug.Log("Emotion code set to " + emotionCode);
            switch(emotionCode)
            {
                case 0: // neutral
                    animationController.PlayIdle();
                    break;
                case 1: // pain
                    animationController.PlayHeadPain();
                    break;
                case 2: // happy
                    animationController.PlayHappy();
                    break;
            }
        }
        else
        {
            Debug.LogWarning("No valid emotion code found in message: " + message);
            animationController.PlayIdle();
        }
    }

    IEnumerator PostRequest()
    {
        Debug.Log("Building request body for chat completion...");

        var requestObject = new
        {
            model = "gpt-4o-mini",  
            messages = chatMessages,
            max_tokens = 100
        };

        string requestBody = JsonConvert.SerializeObject(requestObject);
        Debug.Log("Request Body: " + requestBody);

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        Debug.Log("Request completed. Status Code: " + request.responseCode);

        if (request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response Body: " + request.downloadHandler.text);
        }
        else if (request.responseCode == 200)
        {
            Debug.Log("Response received: " + request.downloadHandler.text);
            
            var jsonResponse = JObject.Parse(request.downloadHandler.text);
            var messageContent = jsonResponse["choices"][0]["message"]["content"].ToString();
            Debug.Log("AI (Patient) response: " + messageContent);

            // Update animation 
            UpdateAnimation(messageContent);

            chatMessages.Add(new Dictionary<string, string>() 
            { 
                { "role", "assistant" }, 
                { "content", messageContent } 
            });
        }
        else
        {
            Debug.LogWarning("Request completed with response code: " + request.responseCode);
            Debug.LogWarning("Response Body: " + request.downloadHandler.text);
        }
    }
}