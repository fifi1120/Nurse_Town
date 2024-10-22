using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class BodyMove : MonoBehaviour
{
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;
    private List<Dictionary<string, string>> chatMessages;

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log("APIKey:" + apiKey);
        Debug.Log("Script started. Starting initial conversation...");
        
        chatMessages = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", 
                    "You are strictly playing the role of a patient NPC in a hospital. " +
                    "You will be interacting with a user, who is a nursing student. " +
                    "As a patient NPC, you are not allowed to ask any questions or provide any assistance. " +
                    "You must respond with short answers that describe your symptoms or feelings based on the player's input.\n\n" +
                    "IMPORTANT: You must end EVERY response with one of these emotion tags: [happy] or [pain]\n" +
                    "- Use [happy] for positive responses, gratitude, or when feeling better\n" +
                    "- Use [pain] for responses involving discomfort, symptoms, or negative feelings\n\n" +
                    "Examples:\n" +
                    "- If describing symptoms: 'My chest feels very tight[pain]'\n" +
                    "- If expressing gratitude: 'Thank you for helping me[happy]'\n\n" +
                    "Start by saying: 'Hi, nurse, I am feeling pain in my chest[pain]'"
                }
            },
            new Dictionary<string, string>
            {
                { "role", "assistant" },
                { "content", "Hi, nurse, I am feeling pain in my chest[pain]" }
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

    IEnumerator PostRequest()
    {
        Debug.Log("Building request body for chat completion...");

        var requestObject = new
        {
            model = "gpt-4o-mini",  
            messages = chatMessages,
            max_tokens = 100
        };

        // Serialize the request object properly using Json.NET
        string requestBody = JsonConvert.SerializeObject(requestObject);
        Debug.Log("Request Body: " + requestBody);  // Log the request body for debugging

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