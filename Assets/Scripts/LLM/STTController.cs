using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class STTController : MonoBehaviour
{
    public TextMeshProUGUI transcriptText;
    private bool isRecording = false;
    private AudioClip recordedClip;
    private BodyMove bodyMove;
    private string openAiApiKey;
    private string lastTranscription;

    void Start()
    {
        openAiApiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        bodyMove = FindObjectOfType<BodyMove>();
        Debug.Log("APIKey loaded (hidden for security)");
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
            transcriptText.text = "Recording...";
        }
    }

    private void StopRecordingAndTranscribe()
    {
        if (isRecording)
        {
            Microphone.End(null);
            isRecording = false;
            transcriptText.text = "Processing...";
            _ = TranscribeAudio();
        }
    }

    private async Task TranscribeAudio()
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "recordedAudio.wav");
            SavWav.Save("recordedAudio.wav", recordedClip);

            lastTranscription = await SendToWhisperAPI(filePath, "whisper-1");
            transcriptText.text = "You: " + lastTranscription;
            
            if (bodyMove != null)
            {
                bodyMove.PlayerResponds(lastTranscription);
            }
            
            File.Delete(filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in transcription: {e.Message}");
            transcriptText.text = "Error in transcription. Please try again.";
        }
    }

    private async Task<string> SendToWhisperAPI(string filePath, string model)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiApiKey}");

            using (var form = new MultipartFormDataContent())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var audioContent = new StreamContent(fileStream);
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                
                form.Add(audioContent, "file", Path.GetFileName(filePath));
                form.Add(new StringContent(model), "model");
                form.Add(new StringContent("en"), "language");
                form.Add(new StringContent("json"), "response_format");
                form.Add(new StringContent("0.2"), "temperature");

                var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transcriptionResponse = JsonConvert.DeserializeObject<TranscriptionResponse>(responseContent);
                    return transcriptionResponse.text;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Transcription failed: {response.ReasonPhrase} - {errorContent}");
                    throw new System.Exception("Failed to transcribe audio");
                }
            }
        }
    }

    private class TranscriptionResponse
    {
        public string text { get; set; }
    }
}