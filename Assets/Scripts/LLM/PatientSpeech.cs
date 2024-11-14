using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq;
using System;

public class OpenAIRequest : MonoBehaviour
{
    public static OpenAIRequest Instance;  

    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;

    private List<Dictionary<string, string>> chatMessages;

    void Awake()
    {
        Instance = this; 

    }

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log("In awake here we are using APIKey:" + apiKey);
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

As Mrs. Johnson, please initiate the conversation by greeting the nurse and mentioning how you're feeling.
";
        
        chatMessages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", rolePlayingInstructions }
            }
        };

        StartCoroutine(PostRequest());
    }

    public void ReceiveNurseTranscription(string transcribedText) // integrate with STT 11/13
    {
        NurseResponds(transcribedText);
    }

    private void NurseResponds(string nurseMessage)
    {
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", nurseMessage } });
        Debug.Log("Nurse says: " + nurseMessage);
        StartCoroutine(PostRequest());
    }

    IEnumerator PostRequest()
    {
        Debug.Log("Building request body for chat completion...");

        string requestBody = "{\"model\": \"gpt-4\", \"messages\": [";
        foreach (var message in chatMessages)
        {
            string contentEscaped = message["content"]
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
            requestBody += "{\"role\": \"" + message["role"] + "\", \"content\": \"" + contentEscaped + "\"},";
        }
        requestBody = requestBody.TrimEnd(',') + "]}"; 

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response Body: " + request.downloadHandler.text);
        }
        else if (request.responseCode == 200)
        {
            Debug.Log("Response received: " + request.downloadHandler.text);
            var jsonResponse = JObject.Parse(request.downloadHandler.text);
            var choices = jsonResponse["choices"];
            var messageContent = choices[0]["message"]["content"].ToString();
            Debug.Log("AI (Mrs. Johnson) response: " + messageContent);
            chatMessages.Add(new Dictionary<string, string>() { { "role", "assistant" }, { "content", messageContent } });
        }
    }
}