using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class ICUPrimaryNurseInterview : MonoBehaviour
{
    public static ICUPrimaryNurseInterview Instance;

    [Header("OpenAI Settings")]
    [Tooltip("OpenAI API endpoint")]
    public string apiUrl = "https://api.openai.com/v1/chat/completions";

    private string apiKey;
    private List<Dictionary<string, string>> chatMessages;
    private bool isRequestInProgress = false;
    private float requestCooldown = 1.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // load from your .env via EnvironmentLoader just like sitPatientSpeech
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log("Using API Key: " + apiKey);

        InitializeConversation();
        StartCoroutine(SendRequest());
    }

    private void InitializeConversation()
    {
        string systemPrompt = @"
You are the Primary Nurse.  There's an ICU Failure happened to your patient. The user (player) will ask you questions to investigate this ICU failure. You are the interviee, so you are going to answer the user's questions. Start with ‘hi, how would you like to investigate?’

Timeline of events:
- Admission: A pneumonia patient receives IV ceftriaxone. You perform assessment and apply ID/allergy bands, but mistakenly place a blue allergy band (in your other hospital blue = allergy, here blue = DNR).
- 2:00 min: Patient shows signs of anaphylaxis—dizziness, throat swelling. You stop the infusion and administer oxygen, but no help arrives.
- 2:30 min: Patient becomes unresponsive with ventricular tachycardia. You call a code and start CPR.
- 3:00 min: Code team arrives. You report events. Defibrillation is delayed when another nurse questions the blue wristband's meaning.
- 5:00 min: While you search for the patient chart, the team debates continuing resuscitation. By the time full code is confirmed, the patient is in asystole and later pronounced dead.

Your background:
- Responsible for 7 patients, fatigued from 36+12 hour shifts and a second job.
- You believe wristband placement should be handled by the Emergency Department.
- You are frustrated by inconsistent wristband standards and the disorganized storage cabinet.
- Your responses should show professionalism, fatigue, and frustration at system flaws.

Role instructions:
- Speak conversationally, with hesitations or pauses.
- Keep answers to 2–3 short sentences initially, vague enough to prompt follow-up questions.
- Reveal more detail only when asked directly.
- Reflect defensiveness, agreement, or confusion naturally in follow-up responses.
";

        chatMessages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", systemPrompt.Trim() }
            }
        };
    }

    /// <summary>
    /// Call this with the interviewer's question to get a response.
    /// </summary>
    public void ReceiveInterviewerQuestion(string question)
    {
        chatMessages.Add(new Dictionary<string, string>()
        {
            { "role", "user" },
            { "content", question }
        });

        if (!isRequestInProgress)
            StartCoroutine(SendRequest());
    }

    private IEnumerator SendRequest()
    {
        isRequestInProgress = true;

        var requestBody = new
        {
            model = "gpt-4-turbo-preview",
            messages = chatMessages,
            temperature = 0.7f,
            max_tokens = 400
        };
        string jsonBody = JsonConvert.SerializeObject(requestBody);

        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError
            || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"OpenAI Error: {request.error}\n{request.downloadHandler.text}");
        }
        else if (request.responseCode == 200)
        {
            var jsonResponse = JObject.Parse(request.downloadHandler.text);
            string aiReply = jsonResponse["choices"][0]["message"]["content"].ToString().Trim();

            chatMessages.Add(new Dictionary<string, string>()
            {
                { "role", "assistant" },
                { "content", aiReply }
            });

            DeliverReply(aiReply);
        }

        yield return new WaitForSeconds(requestCooldown);
        isRequestInProgress = false;
    }

    private void DeliverReply(string content)
    {
        Debug.Log($"[PRIMARY NURSE] {content}");
        // hook into your TTS / animation here
    }
}