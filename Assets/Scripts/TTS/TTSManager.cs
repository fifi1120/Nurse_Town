using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;


public class TTSManager : MonoBehaviour
{
    public TMP_InputField userInput; // For TextMeshPro input field
    public AudioSource audioSource;

    // Can load this securely from PlayerPrefs or another secure location
    private string openAIApiKey;
    private static readonly string ttsEndpoint = "https://api.openai.com/v1/audio/speech";
    private const bool deleteCachedFile = true;

    public void Start()
    {
        openAIApiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log("OpenAI API Key: " + openAIApiKey);

        userInput.onEndEdit.AddListener(delegate { if (Input.GetKeyDown(KeyCode.Return)) ConvertTextToSpeech(); });
    }

    public async void ConvertTextToSpeech()
    {
        string text = userInput.text;
        if (string.IsNullOrEmpty(text)) return;

        byte[] audioData = await GetTTSAudio(text, "tts-1", "nova", "mp3", 1.0f);
        if (audioData != null)
        {
            ProcessAudioBytes(audioData);
        }
    }

    private async Task<byte[]> GetTTSAudio(string inputText, string model, string voice, string responseFormat = "mp3", float speed = 1.0f)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + openAIApiKey);

            // Debug each parameter before creating the JSON
            // Debug.Log("Model: " + model);
            // Debug.Log("Input Text: " + inputText);
            // Debug.Log("Voice: " + voice);
            // Debug.Log("Response Format: " + responseFormat);
            // Debug.Log("Speed: " + speed);

            // Use a defined class for the request body to ensure correct JSON serialization
            var requestBody = new TTSRequest
            {
                model = model,
                input = inputText,
                voice = voice,
                response_format = responseFormat,
                speed = speed
            };

            string jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(ttsEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                Debug.LogError("Error with TTS API: " + response.ReasonPhrase + "\nDetails: " + errorResponse);
                return null;
            }
        }
    }

    private void ProcessAudioBytes(byte[] audioData)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "audio.mp3");
        File.WriteAllBytes(filePath, audioData);

        StartCoroutine(LoadAndPlayAudio(filePath));
    }

    private IEnumerator LoadAndPlayAudio(string filePath)
    {
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else
        {
            Debug.LogError("Audio file loading error: " + www.error);
        }

        if (deleteCachedFile)
        {
            File.Delete(filePath);
        }
    }

    // Define the request body structure
    public class TTSRequest
    {
        public string model { get; set; }
        public string input { get; set; }
        public string voice { get; set; }
        public string response_format { get; set; }
        public float speed { get; set; }
    }
}
