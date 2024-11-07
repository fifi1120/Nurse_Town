using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json; // Make sure you have Newtonsoft.Json imported for JSON parsing

public class SpeechToTextController : MonoBehaviour
{
    public TextMeshProUGUI transcriptText; // Reference to the Text or TextMeshPro field in the UI
    private bool isRecording = false;
    private AudioClip recordedClip;

    // Set your OpenAI API Key here
    private readonly string openAiApiKey = "YOUR_OPENAI_API_KEY";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartRecording();
        }
        if (Input.GetKeyUp(KeyCode.Space))
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
        string transcription = await SendToWhisperAPI(filePath, "whisper-1", "en", "json", 0.2f);

        // Display only the transcription text
        transcriptText.text = transcription;

        // Optionally delete the temporary file
        File.Delete(filePath);
    }

    private async Task<string> SendToWhisperAPI(string filePath, string model, string language, string responseFormat, float temperature)
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

                form.Add(new StringContent(responseFormat), "response_format");
                form.Add(new StringContent(temperature.ToString()), "temperature");

                HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);

                if (response.IsSuccessStatusCode)
                {
                    // Parse the JSON response and extract only the "text" field
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transcriptionResponse = JsonConvert.DeserializeObject<TranscriptionResponse>(responseContent);
                    return transcriptionResponse.text;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError("Transcription failed: " + response.ReasonPhrase + " - " + errorContent);
                    return "Error in transcription";
                }
            }
        }
    }

    // Define a class to represent the JSON response structure
    private class TranscriptionResponse
    {
        public string text { get; set; }
    }
}
