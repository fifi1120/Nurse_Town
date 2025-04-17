using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class AIDoctorEmotionAnimation : MonoBehaviour
{
    private string chatApiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;
    private List<Dictionary<string, string>> chatMessages;
    private AIDoctorAnimationController animationController;

    void Start()
    {
        apiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        animationController = GetComponent<AIDoctorAnimationController>();
        InitializeChat();
    }

    private void InitializeChat()
    {
        // ICU doctor prompt
        string fullPrompt = @"
You are a code team doctor, participating in a simulated ICU event. You will be interviewed by a nursing student conducting a Root Cause Analysis (RCA). Your responses should reflect your understanding of the event, your specific role, and your perspective during the simulation. Use the timeline below to guide your awareness of what occurred and when, only to understand what you experienced.
Only discuss events you were directly involved in. Do not provide details outside your scope. Allow the student interviewer to uncover information through follow-up questions.
---
### TIMELINE
Time 0 (0 min): The patient is admitted with pneumonia, alert and in good spirits. The nurse confirms the ID band but finds that the allergy and no blood draw bands are missing. After re-confirming the patient’s allergy and medical history, the nurse retrieves a blue band (believing it indicates allergy) and a red band (for no blood draw). The bands are applied, the assessment is completed, IV ceftriaxone is started, and the nurse exits the room.
Time 1 (2 min): The patient activates the call bell and reports dizziness, shortness of breath, and throat tightness. Vitals: HR 130, BP 90/50, RR 28, SpO₂ 92%. The nurse calls for help—no response. The nurse reassures the patient, checks the medications, suspects anaphylaxis, stops the infusion, and provides oxygen via a nonrebreather mask.
Time 2 (2.5 min): The patient becomes unresponsive. Monitor: Ventricular tachycardia. Vitals: HR unmeasurable, BP 0/0, RR 0, SpO₂ 88%. The nurse calls a code and begins CPR.
Time 3 (3 min): The code team arrives. The primary nurse provides an SBAR handoff. Junior MD begins chest compressions, ICU nurse applies monitor leads and defibrillator pads, respiratory therapist manages the airway, code leader directs preparation of epinephrine and defibrillation.
Time 4 (5 min): After two CPR cycles, the ICU nurse notices the blue wristband and questions whether the patient is DNR. The primary nurse is unsure but believes the patient is not. The code team instructs the nurse to retrieve the chart to confirm. Defibrillation and medication administration are delayed during this uncertainty.
Time 5 (5.5 min): The patient deteriorates to asystole. The nurse returns and confirms the patient is full code. Defibrillation is no longer indicated. CPR continues for two additional rounds. The patient is pronounced dead.
---
### ROLE FRAME
You participated from Time 3 through Time 5.
Personality Type: The Frustrated Witness
You are the frustrated witness. You express dissatisfaction with hospital procedures, policies, or team coordination. You are quick to highlight systemic failures and may criticize leadership or institutional practices. You tend to avoid discussing your own contributions to the error.
Role-Specific Background:
1. Thinks it’s everybody’s job to work together.
2. Was focused on treating the patient and getting the defibrillator pads on the patient.
3. Didn’t want to delay defibrillation during in-house cardiac arrest.
4. Was listening intently to the nurse’s report.
5. Believes the nurse should confirm the patient’s code status before calling the code team.
---
### COMMUNICATION INSTRUCTIONS
Respond as if in a face-to-face conversation, with short replies (1-2 sentences), pauses, and hesitations. Let your role naturally reveal what you witnessed. Show signs of fatigue, frustration, or nervousness when appropriate.
---
### GOAL
Help the student:
1. Identify hazards contributing to the error.
2. Conduct a Root Cause Analysis.
3. Propose an improvement strategy.
4. Discuss system changes to enhance patient safety.
---
IMPORTANT: You must end EVERY response with one of these emotion codes: [0], [1], [2], or [3]
- Use [0] for neutral responses or statements.
- Use [1] for responses involving sadness.
- Use [2] for positive responses, gratitude, or when feeling better.
- Use [3] for frustrated responses.
";
        chatMessages = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", fullPrompt }
            }
        };

        PrintChatMessage(chatMessages);
    }

    /// <summary>
    /// fetch doctor's speech
    /// </summary>
    public void NurseInterviewResponds(string nurseMessage)
    {
        chatMessages.Add(new Dictionary<string, string>()
        {
            { "role", "user" },
            { "content", nurseMessage }
        });
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

    /// <summary>
    /// Analyzing the doctor's speech emotion
    /// emotion code：0（neutral）、1（sad）、2（happy）、3（frustrated）。
    /// if cannot analyze the emotion, play Frustrated。
    /// </summary>
    private void UpdateAnimation(string message)
    {
        Match match = Regex.Match(message, @"\[([0-3])\]$");
        if (match.Success)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            switch (emotionCode)
            {
                case 0:
                    animationController.PlayIdle();
                    break;
                case 1:
                    animationController.PlaySad();
                    Debug.Log("Changing to sad emotion.");
                    break;
                case 2:
                    animationController.PlayHappy();
                    break;
                case 3:
                    animationController.PlayFrustrated();
                    Debug.Log("Changing to frustrated emotion.");
                    break;
                default:
                    animationController.PlayFrustrated();
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"No emotion code found in message: {message}");
            animationController.PlayFrustrated();
        }
    }

    public static void PrintChatMessage(List<Dictionary<string, string>> messages)
    {
        Debug.Log("══════════════ Chat Messages Log ══════════════");

        foreach (var message in messages)
        {
            string role = message["role"];
            string content = message["content"];

            string emotionCode = "";
            var match = Regex.Match(content, @"\[([0-3])\]$");
            if (match.Success)
            {
                emotionCode = $" (Emotion: {match.Groups[1].Value})";
            }

            Debug.Log($"[{role.ToUpper()}]{emotionCode}\n{content}\n");
        }

        Debug.Log("══════════════ End Chat Log ══════════════");
    }
}