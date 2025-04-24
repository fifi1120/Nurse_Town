/// === How to Use in a New Scene ===
/// 1. Attach this script to an empty GameObject (e.g., "AssessmentManager") in your scene.
/// 2. Create a UI TextMeshProUGUI element and assign it to the `resultText` field in the Inspector.
/// 3. [Formative Assessment]
///    - Set `playerTranscript` either from another script or automatically via PlayerPrefs.
///    - Call `ShowFormativeAssessment()` (e.g., via a Button's OnClick event) to send the transcript to GPT-4o for feedback.
/// 
/// 4. [Summative Assessment]
///    - Add a Button that triggers `StartSummativeAssessment()`
///    - This will open a file dialog allowing upload of a .docx file. Its contents are parsed and sent to GPT-4o for evaluation.


/// Integration Notes:
/// In each interview scene:
/// 1. Collect the transcript as the user progresses through the scene (e.g., user/LLM dialogue).
/// 2. When the user finishes the interview (e.g., on "Finish Interview" button click), append the scene’s transcript to PlayerPrefs:
/// 
///    string thisSceneTranscript = currentTranscript; // your local variable
///    string existingTranscript = PlayerPrefs.GetString("interviewScripts", "");
///    PlayerPrefs.SetString("interviewScripts", existingTranscript + "!nurse/doctor/medical student...!" + thisSceneTranscript + "\n\n");

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Xceed.Words.NET;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class AssessmentManager : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    public string playerTranscript;  // To be set before starting formative assessment

    private string apiKey;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    void Start()
    {
        apiKey = LoadApiKeyFromEnv();
        Debug.Log("Using API Key: " + apiKey);
        resultText.text = "";
        // Try to load from PlayerPrefs
        string storedScript = PlayerPrefs.GetString("interviewScripts", "");
        playerTranscript = string.IsNullOrWhiteSpace(storedScript) ? exampleScripts : storedScript;
    }

    // Load API key directly from a .env file
    private string LoadApiKeyFromEnv()
    {
        string filePath = Path.Combine(Application.dataPath, "../.env");
        Debug.Log("Reading .env from: " + filePath);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning(".env file not found.");
            return null;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim() == "OPENAI_API_KEY")
            {
                return parts[1].Trim();
            }
        }

        Debug.LogWarning("OPENAI_API_KEY not found in .env file.");
        return null;
    }

    // ===== FORMATIVE =====

    public void ShowFormativeAssessment()
    {
        if (string.IsNullOrWhiteSpace(playerTranscript))
        {
            resultText.text = "No interview transcript found. Please complete the interview first.";
            return;
        }

        resultText.text = "Evaluating formative assessment...";
        StartCoroutine(SendFormativeToGPT4o(playerTranscript));
    }

    private IEnumerator SendFormativeToGPT4o(string playerInput)
    {
        var messages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", nursePatientPrompt }
            },
            new Dictionary<string, string>()
            {
                { "role", "user" },
                { "content", $"Here is the student’s interview transcript:\n\n{playerInput}" }
            }
        };

        var requestBody = new
        {
            model = "gpt-4o",
            messages = messages,
            temperature = 0.5f,
            max_tokens = 800
        };

        string json = JsonConvert.SerializeObject(requestBody);
        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("GPT Request Error: " + request.error);
            resultText.text = "Formative evaluation failed: " + request.error;
        }
        else
        {
            var response = JObject.Parse(request.downloadHandler.text);
            string gptFeedback = response["choices"][0]["message"]["content"].ToString();
            resultText.text = "Formative GPT Feedback:\n\n" + gptFeedback;
            Debug.Log(gptFeedback);
        }
    }

    private readonly string formativePrompt = @"
You are a clinical educator evaluating a nursing student's performance during a simulated root cause analysis (RCA) interview about a medical mistake that occurred in the ICU. The student is role-playing as a nurse interviewing medical staff involved in the incident.

Please assess the student's performance using the following criteria:

1. Depth of Inquiry:
- Did the student ask open-ended questions that encouraged detailed responses?
- Did they use follow-up questions to clarify or expand on vague answers?

2. Comprehensiveness of Investigation:
- Did the student cover key topics, including individual actions, decisions, communication breakdowns, and system-level issues?
- Did they explore both human errors and systemic factors (e.g., fatigue, unclear protocols, teamwork gaps)?

3. Active Listening and Adaptability:
- Did the student adjust their questioning based on responses (e.g., probe further when inconsistencies emerged)?
- Did they notice and follow up on non-verbal cues or hesitations if provided by the LLM?

4. Identification of Key Themes:
- Did the student recognize and pursue relevant issues such as:
  • Misinterpretation of wristband colors
  • Fatigue and workload issues
  • Lack of standardized protocols
  • Communication barriers

5. Professionalism and Clarity:
- Was the student’s tone professional, respectful, and appropriate for an investigative interview?
- Did they maintain control of the interview without being confrontational or dismissive?

At the end, provide:
- A score out of 10
- Specific reasons for any point deductions
- Suggestions for improvement to strengthen future interview performance

Keep your tone constructive and focused on growth. Limit your response to 2–3 paragraphs.";

    private readonly string nursePatientPrompt = $"You are an expert nursing instructor. For each of the nurse's respoonse, evaluate based on the criteria provided.\n\n" +
                        "Scoring Criteria:\n" +
                        "- Deduct 1 point if medical jargon was used without explanation.\n" +
                        "- Add 2 points if the nurse mentions printing a list or sending an email.\n" +
                        "- Add 2 points if the nurse mentions the \"0-10 scale\" of pain or other discomfort.\n\n" +
                        "Anything like that that feels reasonable" +
                        "Provide the output in the following format:" +
                        "Nurse response:" +
                        "Points change:" +
                        "Reason"
;


    // ===== SUMMATIVE =====

    public void StartSummativeAssessment()
    {
        var extensions = new[]
        {
            new ExtensionFilter("Word Documents", "docx"),
            new ExtensionFilter("All Files", "*")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Upload Your Assessment", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string filePath = paths[0];
            string fileName = Path.GetFileName(filePath);
            string content = ParseDocx(filePath);

            if (!string.IsNullOrEmpty(content))
            {
                resultText.text = $"Upload Successful! Summative Assessment Uploaded:\n{fileName}\n\nEvaluating...";
                StartCoroutine(SendToGPT4o(content));
            }
            else
            {
                resultText.text = "Failed to parse the document content.";
            }
        }
    }

    private string ParseDocx(string filePath)
    {
        try
        {
            using (DocX document = DocX.Load(filePath))
            {
                return document.Text;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error reading DOCX file: " + e.Message);
            return null;
        }
    }

    private IEnumerator SendToGPT4o(string content)
    {
        var messages = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string>()
            {
                { "role", "system" },
                { "content", summativePrompt }
            },
            new Dictionary<string, string>()
            {
                { "role", "user" },
                { "content", $"Here is the student's RCA submission:\n\n{content}" }
            }
        };

        var requestBody = new
        {
            model = "gpt-4o",
            messages = messages,
            temperature = 0.5f,
            max_tokens = 1000
        };

        string json = JsonConvert.SerializeObject(requestBody);
        var request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("GPT Request Error: " + request.error);
            resultText.text = "Summative evaluation failed: " + request.error;
        }
        else
        {
            var response = JObject.Parse(request.downloadHandler.text);
            string gptFeedback = response["choices"][0]["message"]["content"].ToString();
            resultText.text = "Summative GPT Feedback:\n\n" + gptFeedback;
            Debug.Log(gptFeedback);
        }
    }

    private readonly string summativePrompt = @"
You are an expert nurse educator evaluating a student's written Root Cause Analysis (RCA) report based on a simulated ICU error interview. Use the following rubric to score and comment on the student's performance.

Evaluate the following criteria (score each out of 10, with reasons if points were deducted):

1. Clarity of Problem Statement:
- Does the student clearly describe what happened, when it happened, and why it was significant?

2. Identification of Causes:
- Immediate Causes: Are direct causes (e.g., misreading the wristband) correctly identified?
- Contributing Factors: Are secondary issues (e.g., fatigue, communication failures) thoroughly analyzed?

3. Systemic Issues Analysis:
- Are broader systemic failures addressed, such as:
  - Poor workflow or unclear responsibilities
  - Gaps in protocols or staff training
  - Communication or leadership breakdowns during the code?

4. Use of Interview Evidence:
- Does the student accurately incorporate and reference interview responses?
- Are conflicting or differing perspectives from staff analyzed?

5. Proposed Solutions and Preventive Measures:
- Are recommendations practical, specific, and targeted at root causes?
- Do they include system-level fixes (e.g., protocol changes, staff training) beyond individual errors?

6. Structure and Presentation:
- Is the RCA organized clearly (e.g., SBAR format)?
- Is the language professional, concise, and analytical?

At the end, provide:
- Overall Score (out of 10)
- Strengths (e.g., strong use of evidence, realistic recommendations)
- Areas for Improvement (e.g., missed causes, unclear statements)
- Suggestions (e.g., ways to deepen systemic analysis or improve clarity)

Be objective and constructive. Do not add any made-up interview data or details that were not in the student's submission.";

    private readonly string exampleScripts = @"
User
Hello, I’m a nurse at the hospital. I'm here to talk about the ICU incident earlier.

Nurse - Frustrated
Hi. (Nods) I'm here to discuss that, too. It's been... quite overwhelming, to say the least. Where would you like to start?

User
Why did the patient have a delayed therapy?

Nurse - Frustrated
(Sighs) It was mainly the confusion over the wristband color. At my other job, a blue wristband means something different—it indicates an allergy. Here, it means DNR. There was a mix-up because of that.

User
Have you ever been confused with the wristband colors before?

Nurse - Frustrated
(Shaking head) Not until now. This was the first time, and I didn't realize the difference until it was too late. It's frustrating that different hospitals use different systems. It makes things complicated.

User
How can we prevent this from happening again?

Nurse - Frustrated
(Pauses, thinking) Standardizing wristband colors across hospitals would help, so there's no room for confusion. Clearer communication and organization of wristbands in the supply cabinet would also make a difference. It's hard to juggle different systems when we're already exhausted.

User
You mentioned organizing the supply cabinet. Why?

Nurse - Frustrated
(Sighs, looks tired) The wristbands are all over the place in the cabinet. It's really disorganized, which makes it challenging to quickly find the right ones, especially when you're in a stressful situation. Keeping things in order would definitely help avoid mistakes.

User
How exactly can we better organize the supply cabinet?

Nurse - Frustrated
(Labeling shelves) would be a start, so everything's clear and easy to find. Maybe have separate sections or bins for each wristband type. That way, there's no fumbling around, especially when time is critical.

User
What else do you think may have contributed to the mistake?

Nurse - Frustrated
(Frowns) Honestly, being fatigued played a role. Working long hours and managing multiple responsibilities can cloud judgment. Plus, I assumed placing wristbands was the ED's job, which was a misunderstanding on my part.

User
Why were you fatigued?

Nurse - Frustrated
(Rubbing temples) I've been working a lot—36 hours over the past few days and an extended 12-hour shift yesterday because of a colleague's sick call. Plus, I'm juggling two jobs. It's just been a lot to handle.

User
Do you think we should limit the maximum hours a nurse can work?

Nurse - Frustrated
(Nods) That could definitely help. We need to make sure nurses aren't overworked because fatigue can really impact decision-making. Having limits could ensure we're at our best to provide safe patient care.

User
What do you think is a reasonable number of hours?

Nurse - Frustrated
(Pauses to think) Maybe something like a maximum of 12-hour shifts with adequate time off in between. And perhaps also limiting the number of consecutive days worked, so there's a balance between patient safety and nurse well-being.

User
The wristband colors and fatigue — which do you think is more important?

Nurse - Frustrated
(Reflects for a moment) They're both important, but standardizing wristband colors might be more immediately impactful in preventing mix-ups like this one. Fatigue, though, is a broader issue that affects overall performance and safety. Both need attention.
";

}
