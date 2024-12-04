using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine.Audio;
using System.Collections;
using Newtonsoft.Json;
using System.Text;
// for animation
using System.Text.RegularExpressions;

public class sitTTSManager : MonoBehaviour
{
    public static sitTTSManager Instance { get; private set; } 

    public AudioSource audioSource; // Reference to the AudioSource where the speech will be played.

    private string openAIApiKey; // API key for OpenAI
    private static readonly string ttsEndpoint = "https://api.openai.com/v1/audio/speech"; // Endpoint for TTS
    private const bool deleteCachedFile = true; // Flag to determine if the audio file should be deleted after playing
    // for animation
    private sitCharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;  
    private BloodTextController bloodTextController;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // If you need to maintain this during scene transitions, please uncomment the following line.
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Load the API key securely
        openAIApiKey = EnvironmentLoader.GetEnvVariable("OPENAI_API_KEY");
        Debug.Log($"[start]API Key loaded: {!string.IsNullOrEmpty(openAIApiKey)}"); // Will print true/false without exposing the key
        
        if (string.IsNullOrEmpty(openAIApiKey))
        {
            Debug.LogError("TTS Manager: OpenAI API key is missing! Make sure OPENAI_API_KEY environment variable is set.");
            return;
        }
        
        animationController = GetComponent<sitCharacterAnimationController>();
        
        // Find the blood effect in the UI
        bloodEffectController = GameObject.FindObjectOfType<BloodEffectController>();
        if (bloodEffectController == null)
        {
            Debug.LogError("BloodEffectController not found in the scene. Make sure it exists in the UI!");
        }
        bloodTextController = GameObject.FindObjectOfType<BloodTextController>();
        if (bloodTextController == null)
        {
            Debug.LogError("BloodTextController not found in the scene. Make sure it exists in the UI!");
        }
    }

    // Public method to be called to convert text to speech
    public async void ConvertTextToSpeech(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("TTS Manager: No text provided for TTS");
            return;
        }

        // Strip emotion code for TTS but keep original text for animation
        string ttsText = text;
        Match match = Regex.Match(text, @"\[([0-9]|10)\]$");
        if (match.Success)
        {
            ttsText = text.Substring(0, text.Length - 3).Trim();
        }

        // Get audio data from OpenAI's TTS service
        byte[] audioData = await GetTTSAudio(ttsText, "tts-1", "nova", "mp3", 1.0f);
        if (audioData != null)
        {
            ProcessAudioBytes(audioData, text); // Pass original text for animation
        }
        else
        {
            Debug.LogError("TTS Manager: Failed to get audio data");
        }
    }

    // Method to get TTS audio from OpenAI
    private async Task<byte[]> GetTTSAudio(string inputText, string model, string voice, string responseFormat = "mp3", float speed = 1.0f)
    {
        using (HttpClient client = new HttpClient())
        {
            try 
            {
                // Make sure to include "Bearer " prefix with the API key
                client.DefaultRequestHeaders.Clear(); // Clear any existing headers
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAIApiKey.Trim()}");
                
                Debug.Log("Authorization header set"); // Confirm header is being set
                
                var requestBody = new TTSRequest
                {
                    model = model,
                    input = inputText,
                    voice = voice,
                    response_format = responseFormat,
                    speed = speed
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                Debug.Log($"Request body: {jsonContent}"); // Check the request body
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(ttsEndpoint, content);
                
                Debug.Log($"Response status: {response.StatusCode}"); // Log the response status

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Error with TTS API: {response.StatusCode} - {response.ReasonPhrase}\nDetails: {errorResponse}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in GetTTSAudio: {ex.Message}\nStack trace: {ex.StackTrace}");
                return null;
            }
        }
    }

    // Method to process and play the audio bytes received
    private void ProcessAudioBytes(byte[] audioData, string messageContent)
    {
        // Save the audio data as a .mp3 file locally
        string filePath = Path.Combine(Application.persistentDataPath, "audio.mp3");
        File.WriteAllBytes(filePath, audioData);

        // Start coroutine to load and play the audio file
        StartCoroutine(LoadAndPlayAudio(filePath, messageContent));
    }

    // Coroutine to load and play the audio file
    private IEnumerator LoadAndPlayAudio(string filePath, string messageContent)
    {
        // Create a UnityWebRequest to load the audio file
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            // If the file is successfully loaded, get the audio clip and play it
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            // Debug.Log("Audio clip loaded successfully");
            audioSource.clip = audioClip;
            audioSource.Play();
            UpdateAnimation(messageContent);
        }
        else
        {
            // Log error if the file loading fails
            Debug.LogError("Audio file loading error: " + www.error);
        }

        // Optionally delete the file after playing
        if (deleteCachedFile)
        {
            File.Delete(filePath);
        }
    }

    // Nested class to structure the TTS request body
    public class TTSRequest
    {
        public string model { get; set; }
        public string input { get; set; }
        public string voice { get; set; }
        public string response_format { get; set; }
        public float speed { get; set; }
    }

        private void UpdateAnimation(string message)
    {
        Match match = Regex.Match(message, @"\[([0-5])\]$");
        if (match.Success)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            switch(emotionCode)
            {
                case 0:
                    animationController.PlayBend();
                    break;
                case 1:
                    animationController.PlayRubArm();
                    break;
                case 2:
                    animationController.PlaySad();
                    break;
                case 3:
                    animationController.PlayThumbUp();
                    break;
                case 4:
                    animationController.PlayBloodPressure();
                    bloodEffectController.SetBloodVisibility(true);
                    bloodTextController.SetBloodTextVisibility(true);
                    break;
                default:
                    Debug.LogWarning($"Invalid emotion code: {emotionCode}");
                    animationController.PlayIdle();
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"No emotion code found: {message}");
            animationController.PlayIdle();
        }
    }
}