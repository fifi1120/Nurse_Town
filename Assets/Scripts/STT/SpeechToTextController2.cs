using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json; // Make sure you have Newtonsoft.Json imported for JSON parsing

public class SpeechToTextController2 : MonoBehaviour
{
    public TextMeshProUGUI transcriptText; // Reference to the Text or TextMeshPro field in the UI
    private bool isRecording = false;
    private AudioClip recordedClip;

    // Set your OpenAI API Key here
    private string openAiApiKey;

    void Start()
    {
        openAiApiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        // Debug.Log("APIKey:" + openAiApiKey);

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartRecording();
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            StopRecordingAndTranscribe();
        }
    }

    private void StartRecording()
    {
        if (!isRecording)
        {
            recordedClip = Microphone.Start(null, false, 10, 44100);
            isRecording = true;
        }
    }

    private void StopRecordingAndTranscribe()
    {
        if (isRecording)
        {
            Microphone.End(null);
            isRecording = false;
            _ = TranscribeAudio(); // Fire and forget the async task
        }
    }

    private async Task TranscribeAudio()
    {
        // Save the AudioClip as a WAV file using SavWav
        string filePath = Path.Combine(Application.persistentDataPath, "recordedAudio.wav");
        SavWav.Save("recordedAudio.wav", recordedClip);

        // Send the WAV file to OpenAI Whisper API
        WhisperResult speech = await SendToWhisperAPI(filePath, "whisper-1", "en", 0.2f);

        // Display only the transcription text
        transcriptText.text = speech.text;

        // Optionally delete the temporary file
        File.Delete(filePath);

        // Fiona update 11/13: integrate with patient NPC
        if (OpenAIRequest.Instance != null)
        {
            OpenAIRequest.Instance.ReceiveNurseTranscription(speech.text, speech.wpm);
        }
        else
        {
            Debug.LogError("OpenAIRequest instance not found.");
        }
    }

    private async Task<WhisperResult> SendToWhisperAPI(string filePath, string model, string language, float temperature)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + openAiApiKey);

            using (var form = new MultipartFormDataContent())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var audioContent = new StreamContent(fileStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                form.Add(audioContent, "file", Path.GetFileName(filePath));
                form.Add(new StringContent(model), "model");

                if (!string.IsNullOrEmpty(language))
                    form.Add(new StringContent(language), "language");

                form.Add(new StringContent("verbose_json"), "response_format");
                form.Add(new StringContent(temperature.ToString()), "temperature");

                HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);

                if (response.IsSuccessStatusCode)
                {
                    // Parse the JSON response and extract only the "text" field
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transcriptionResponse = JsonConvert.DeserializeObject<TranscriptionResponse>(responseContent);

                    // check for missing or empty segments
                    if (transcriptionResponse.segments == null || transcriptionResponse.segments.Count == 0)
                    {
                        Debug.LogWarning("No segments received from Whisper.");
                        return new WhisperResult
                        {
                            text = transcriptionResponse.text,
                            wpm = 0f
                        };
                    }

                    // Calculate the speech rate
                    float start = transcriptionResponse.segments[0].start;
                    float end = transcriptionResponse.segments[transcriptionResponse.segments.Count - 1].end;

                    float durationInMinutes = Mathf.Max((end - start) / 60f, 0.001f); // avoid division by zero

                    int wordCount = System.Text.RegularExpressions.Regex.Matches(transcriptionResponse.text, @"\b\w+\b").Count;
                    float wpm = wordCount / durationInMinutes;

                    return new WhisperResult
                    {
                        text = transcriptionResponse.text,
                        wpm = wpm
                    };
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError("Transcription failed: " + response.ReasonPhrase + " - " + errorContent);
                    return new WhisperResult { text = "Error in transcription", wpm = 0f };
                }
            }
        }
    }

    // Define a class to represent the JSON response structure
    private class TranscriptionResponse
    {
        public string text { get; set; }
        public List<Segment> segments { get; set; }
    }

    private class Segment
    {
        public float start { get; set; }
        public float end { get; set; }
        public string text { get; set; }
    }
    
    private class WhisperResult
    {
        public string text;
        public float wpm;
    }
}
