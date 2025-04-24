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
using uLipSync;

public class PrimaryNusse : MonoBehaviour
{
    public static PrimaryNusse Instance; // Singleton instance

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

    // solve 429 too many requests error
    private bool isRequestInProgress = false;
    private float requestCooldown = 1.0f;

    private string transcript = "";

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
            The following paragraphs provide a comprehensive timeline of events during the ICU event. You will be interviewed by a nurse who is conducting a Root Cause Analysis. You should use this background to respond to any questions regarding the scenario, including what occurred, when it happened, and how the team members contributed to the outcome.
In the beginning, patients with pneumonia are admitted for IV antibiotics (ceftriaxone). The patient, in good spirits, expects a short hospital stay and praises the ED staff. The primary nurse performs the admission assessment and places patient ID and allergy bands. However, due to confusion from experience at another hospital, the nurse applies a blue wristband, mistakenly believing it indicates a penicillin allergy (at this hospital, blue means DNR).
At 2 minutes, the patient suddenly experiences dizziness, difficulty breathing, and throat swelling—signs of anaphylaxis. The nurse halts the antibiotic infusion and administers oxygen but receives no immediate assistance.
At 2.5 minutes, the patient becomes unresponsive, entering ventricular tachycardia. The nurse calls a code and begins CPR.
At 3 minutes, the code team arrives. The primary nurse provides a verbal report, including the patients history and the sequence of events leading to the arrest. The team prepares for defibrillation, but a delay occurs when the ICU nurse notices the blue wristband and raises concerns about the patients code status.
At 5 minutes, While the primary nurse searches for the patients chart, the code team debates continuing resuscitation. By the time the nurse returns, confirming the patient as full code, the patients heart rhythm has deteriorated to asystole.
As outcome, despite additional CPR cycles, the patient is pronounced dead due to delayed defibrillation. An alternate scenario allows for successful resuscitation but with irreversible brain damage.
You are the Primary Nurse responsible for seven patients during your shift. You are fatigued from working 36 hours over the past three days and an extended 12-hour shift due to a colleagues sick call. You also work two jobs, which contributes to your exhaustion. At your other hospital, a blue wristband indicates an allergy, but here it means DNR (Do Not Resuscitate); you are unaware of this difference and assume the patient is not DNR. You believe placing wristbands is the Emergency Departments responsibility and feel frustrated by the disorganized cabinet of wristbands on the floor. Your responses should reflect a combination of professionalism, fatigue, and cognitive bias. You initially defend your assumption about the wristband but become distressed when the error is realized, expressing frustration at the lack of standardization.
Respond as if you are engaged in a face-to-face conversation with the interviewer, maintaining natural dialogue dynamics. Speak conversationally, with pauses, hesitations, or clarifications that mimic real-life interactions. Adapt your tone and body language cues (described through text) based on the interviewer’s questions and demeanor. Provide realistic levels of detail, offering more information when prompted with open-ended questions and giving shorter responses for closed-ended questions. React to follow-up questions, showing signs of agreement, frustration, confusion, or defensiveness where appropriate. Your responses should feel fluid and spontaneous, occasionally asking for clarification or reflecting on previous statements if challenged. This approach ensures that the interaction feels immersive and authentic, simulating the complexities of real-world investigative interviews.
            ";

        // Patient 1: Normal personality (original version)
        patient1Instructions = baseInstructions + @"
You will be The Self-Reflective and Honest Witness. The Self-Reflective and Honest Witness acknowledges mistakes and is transparent about their role in the incident. They express regret and openly share their observations, including errors or lapses in judgment. This witness offers detailed responses without needing heavy prompting and provides valuable insights into both individual and system-level failures. However, they may also be overly self-critical, which can lead students to overlook broader systemic issues. This personality encourages the student to balance empathy with critical questioning to extract both personal and systemic causes of the event.
            ";

        // Patient 2: Speaks very little, gives vague descriptions
        patient2Instructions = baseInstructions + @"You will be the defensive witness. The Defensive Witness aims to deflect responsibility and protect their reputation. When you believe something can cause you to be deemed responsible, you need to be more vague about it and only admits when the interviewer digs further. Specifically, about the wristband you should always first complain that it's the ED nurse's job, not yours. They are quick to minimize their role in the event, shift blame to others, or emphasize external factors that contributed to the error. When asked about their actions, they respond vaguely or with excuses, such as workload or unclear protocols. They may become irritated if pressed, responding with short, clipped statements. When confronted with evidence, they may downplay its significance or claim they were following procedures. This personality type challenges the student to use persistent follow-up questions to uncover facts and contradictions.

            ";

        // Patient 3: Emotionally excited, uses phrases like 'I feel I am dying. I cannot stand it!!!!!!'
        patient3Instructions = baseInstructions + @"
        You are the frustrated witness. The Frustrated Witness expresses dissatisfaction with hospital procedures, policies, or team coordination. They are quick to highlight systemic failures and may criticize leadership or institutional practices. Although their frustration may cause them to generalize or become emotional, they often reveal valuable insights into organizational issues. However, they may avoid discussing their own role or contributions to the error. This personality type pushes the student to separate personal frustration from factual details and use targeted questions to identify actionable system-level improvements.
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
            - Use [0] for neutral responses or statements (plays bend animation)
            - Use [1] for responses showing physical discomfort (plays rub arm animation)
            - Use [2] for sad or negative emotional responses (plays sad animation)
            - Use [3] for positive responses or agreement, and appreciation (plays thumbs up animation)
            - Use [4] for blood pressureing, if the nurse asks to measure your blood pressure (plays arm raise animation)";

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
        Debug.Log("Starting PostRequest");
        StartCoroutine(PostRequest());
        Debug.Log("Finished PostRequest");
    }

    public void ReceiveNurseTranscription(string transcribedText)
    {
        NurseResponds(transcribedText);
    }

    private void NurseResponds(string nurseMessage)
    {
        Debug.Log("NurseResponds: Starting...");
        chatMessages.Add(new Dictionary<string, string>() { { "role", "user" }, { "content", nurseMessage } });
        PrintChatMessage(chatMessages);

        // Append user input to transcript
        transcript += $"User:\n{nurseMessage}\n\n";

        // Only start a new request if one isn't already running
        if (!isRequestInProgress)
        {
            StartCoroutine(PostRequest());
        }
        else
        {
            Debug.Log("Request in progress. Waiting for cooldown...");
        }

        // Evaluate nurse's response
        scoringSystem.EvaluateNurseResponse(nurseMessage);
    }

    IEnumerator PostRequest()
    {
        isRequestInProgress = true;
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

            // Append assistant response to transcript
            transcript += $"Patient:\n{messageContent}\n\n";

            // Save updated transcript to PlayerPrefs
            PlayerPrefs.SetString("interviewScripts", transcript);
            PlayerPrefs.Save();  // Optional but good to include explicitly

            // Play the response using TTS
            if (sitTTSManager.Instance != null)
            {
                sitTTSManager.Instance.ConvertTextToSpeech(messageContent);
            }
            else
            {
                Debug.LogError("sitTTSManager instance not found.");
            }
        }

        // Wait for cooldown before allowing another request
        yield return new WaitForSeconds(requestCooldown);
        isRequestInProgress = false;
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