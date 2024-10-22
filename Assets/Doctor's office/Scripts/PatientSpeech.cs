using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq;

public class OpenAIRequest : MonoBehaviour
{
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey = "XXX"; // TODO: replace with the true OpenAI API key

    private List<Dictionary<string, string>> chatMessages;  // to store the conversation history

    void Start()
    {
        Debug.Log("Script started. Starting initial conversation...");

        // To start, making the model say “I'm feeling not well”
        chatMessages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>() { { "role", "system" }, { "content", "You are strictly playing the role of a patient NPC in a hospital. You will be interacting with a user, who is a nursing student. As a patient NPC, you are not allowed to ask any questions or provide any assistance. You can only respond with short answers that describe your symptoms based on the player's input. Start by saying 'Hi, nurse, I am feeling pain in my chest.' and wait for further input." } },
            new Dictionary<string, string>() { { "role", "assistant" }, { "content", "Hi, nurse, I am feeling pain in my chest." } }
        };

        // let the model say the first sentence and display it on the console
        string initialMessage = "I am feeling not well.";
        Debug.Log("AI (Patient) says: " + initialMessage);

        // add the first sentence to the conversation history
        chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", initialMessage } });

        // start the first request
        StartCoroutine(PostRequest());
    }

    // simulate the player's response
    public void PlayerResponds(string playerMessage)
    {
        // add the player's message to the conversation history
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", playerMessage } });

        // send the request again to the model
        StartCoroutine(PostRequest());
    }

    IEnumerator PostRequest()
    {
        Debug.Log("Building request body for chat completion...");

        string requestBody = "{\"model\": \"gpt-4\", \"messages\": [";
        foreach (var message in chatMessages)
        {
            requestBody += "{\"role\": \"" + message["role"] + "\", \"content\": \"" + message["content"] + "\"},";
        }
        requestBody = requestBody.TrimEnd(',') + "], \"max_tokens\": 50}";

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        Debug.Log("Setting headers and preparing to send the request.");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        Debug.Log("Sending request...");
        yield return request.SendWebRequest();

        Debug.Log("Request completed. Status Code: " + request.responseCode);

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response Body: " + request.downloadHandler.text);
        }
        else if (request.responseCode == 200)  // HTTP OK
        {
            Debug.Log("Response received: " + request.downloadHandler.text);

            // parse the AI's response
            var jsonResponse = JObject.Parse(request.downloadHandler.text);
            var choices = jsonResponse["choices"];
            var messageContent = choices[0]["message"]["content"].ToString();  // get the AI's response content

            Debug.Log("AI (Patient) response: " + messageContent);

            // add the AI's response to the conversation history
            chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", messageContent } });

        }
        else
        {
            Debug.LogWarning("Request completed with response code: " + request.responseCode);
            Debug.LogWarning("Response Body: " + request.downloadHandler.text);
        }
    }
}