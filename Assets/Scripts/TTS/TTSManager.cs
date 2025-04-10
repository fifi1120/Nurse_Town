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

public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Tooltip("Reference to the AudioSource where the speech will be played")]
    public AudioSource audioSource;

    [Header("TTS Configuration")]
    [Tooltip("API key for ElevenLabs (loaded from environment variable by default)")]
    [SerializeField] private string elevenLabsApiKey;
    
    [Tooltip("Voice ID for ElevenLabs")]
    public string voiceId = "Bz0vsNJm8uY1hbd4c4AE";
    
    [Tooltip("Model ID for ElevenLabs")]
    public string modelId = "eleven_multilingual_v2";
    
    [Header("Voice Settings")]
    [Range(0f, 1f)]
    [Tooltip("Stability value (0-1)")]
    public float stability = 0.4f;
    
    [Range(0f, 1f)]
    [Tooltip("Similarity boost value (0-1)")]
    public float similarityBoost = 0.75f;
    
    [Range(0f, 1f)]
    [Tooltip("Style exaggeration value (0-1)")]
    public float styleExaggeration = 0.3f;
    
    [Header("Audio2Face Integration")]
    [Tooltip("Whether to use Audio2Face for facial animation")]
    public bool useAudio2Face = true;
    
    [Tooltip("Whether to delete cached audio files after use")]
    public bool deleteCachedFiles = true;
    
    // API endpoints
    private static readonly string ttsEndpoint = "https://api.elevenlabs.io/v1/text-to-speech";
    
    // Component references
    private CharacterAnimationController animationController;
    private BloodEffectController bloodEffectController;  
    private BloodTextController bloodTextController;
    private Audio2FaceManager audio2FaceManager;
    
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
        // Load the API key from environment variables if not set manually
        if (string.IsNullOrEmpty(elevenLabsApiKey))
        {
            elevenLabsApiKey = EnvironmentLoader.GetEnvVariable("ELEVENLABS_API_KEY");
            Debug.Log("TTS Manager: API key loaded from environment variable");
        }
        
        // Get references to required components
        animationController = GetComponent<CharacterAnimationController>();
        
        // Find the Audio2FaceManager if we're using it
        if (useAudio2Face)
        {
            audio2FaceManager = FindObjectOfType<Audio2FaceManager>();
            if (audio2FaceManager == null)
            {
                Debug.LogWarning("Audio2FaceManager not found in the scene. Audio2Face integration disabled.");
                useAudio2Face = false;
            }
            else
            {
                Debug.Log("Audio2Face integration enabled");
            }
        }
        
        // Find the blood effect in the UI
        bloodEffectController = FindObjectOfType<BloodEffectController>();
        if (bloodEffectController == null)
        {
            Debug.LogError("BloodEffectController not found in the scene. Make sure it exists in the UI!");
        }
        
        bloodTextController = FindObjectOfType<BloodTextController>();
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

        // Get audio data from ElevenLabs TTS service
        byte[] audioData = await GetElevenLabsTTSAudio(
            ttsText,
            voiceId,
            modelId,
            stability,
            similarityBoost,
            styleExaggeration
        );
        
        if (audioData != null)
        {
            if (useAudio2Face && audio2FaceManager != null)
            {
                // Process with Audio2Face
                ProcessWithAudio2Face(audioData, text);
            }
            else
            {
                // Fallback to direct audio playback with emotion code
                ProcessAudioBytes(audioData, text);
            }
        }
        else
        {
            Debug.LogError("TTS Manager: Failed to get audio data from ElevenLabs");
        }
    }

    // Method to get TTS audio from ElevenLabs
    private async Task<byte[]> GetElevenLabsTTSAudio(
        string inputText, 
        string voiceId, 
        string modelId, 
        float stability = 0.4f, 
        float similarityBoost = 0.75f, 
        float styleExaggeration = 0.3f)
    {
        string endpoint = $"{ttsEndpoint}/{voiceId}";
        
        using (HttpClient client = new HttpClient())
        {
            try
            {
                Debug.Log($"API Key length: {elevenLabsApiKey?.Length}");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("xi-api-key", elevenLabsApiKey.Trim());
                Debug.Log("Headers set: xi-api-key header present: " + client.DefaultRequestHeaders.Contains("xi-api-key"));

                // Create the request body
                var requestBody = new ElevenLabsTTSRequest
                {
                    text = inputText,
                    model_id = modelId,
                    voice_settings = new VoiceSettings
                    {
                        stability = stability,
                        similarity_boost = similarityBoost,
                        style_exaggeration = styleExaggeration
                    }
                };

                // Serialize the request body to JSON
                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    // If the request is successful, read the byte array of the audio data
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    // Log error if the request fails
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.LogError("Error with ElevenLabs TTS API: " + response.ReasonPhrase + "\nDetails: " + errorResponse);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in GetElevenLabsTTSAudio: {ex.Message}\nStack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
    
    // Method to process audio with Audio2Face
    private async void ProcessWithAudio2Face(byte[] audioData, string messageContent)
    {
        try
        {
            Debug.Log("Starting Audio2Face processing...");
            
            // Save a copy of the audio for direct playback (we'll need this for emotion triggers)
            string filePath = Path.Combine(Application.persistentDataPath, "audio.mp3");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllBytes(filePath, audioData);
            
            // Process the audio with Audio2Face
            bool success = await audio2FaceManager.ProcessAudioForFacialAnimation(audioData);
            
            if (success)
            {
                Debug.Log("Audio2Face processing completed successfully");
                
                // Animation will be loaded by the Audio2FaceManager
                // We just need to play the audio and handle emotion triggers
                StartCoroutine(LoadAndPlayAudio(filePath, messageContent));
            }
            else
            {
                Debug.LogError("Audio2Face processing failed. Falling back to direct audio playback.");
                ProcessAudioBytes(audioData, messageContent);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in ProcessWithAudio2Face: {ex.Message}");
            ProcessAudioBytes(audioData, messageContent);
        }
    }

    // Method to process and play the audio bytes received
    private void ProcessAudioBytes(byte[] audioData, string messageContent)
    {
        // Save the audio data as a .mp3 file locally
        string filePath = Path.Combine(Application.persistentDataPath, "audio.mp3");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("Deleted existing audio file");
        }
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
            audioSource.clip = audioClip;
            audioSource.Play();
            
            // Update animation based on emotion code
            UpdateAnimation(messageContent);
        }
        else
        {
            // Log error if the file loading fails
            Debug.LogError("Audio file loading error: " + www.error);
        }

        // Optionally delete the file after playing
        if (deleteCachedFiles && File.Exists(filePath))
        {
            // Wait until audio is done playing to delete
            yield return new WaitForSeconds(audioSource.clip.length + 0.5f);
            try
            {
                File.Delete(filePath);
                Debug.Log("Deleted cached audio file");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete cached audio file: {ex.Message}");
            }
        }
    }

    [Serializable]
    public class ElevenLabsTTSRequest
    {
        public string text { get; set; }
        public string model_id { get; set; }
        public VoiceSettings voice_settings { get; set; }
    }

    [Serializable]
    public class VoiceSettings
    {
        public float stability { get; set; }
        public float similarity_boost { get; set; }
        public float style_exaggeration { get; set; }
    }

    private void UpdateAnimation(string message)
    {
        Match match = Regex.Match(message, @"\[([0-9]|10)\]$");
        if (match.Success)
        {
            int emotionCode = int.Parse(match.Groups[1].Value);
            switch(emotionCode)
            {
                case 0:
                    animationController.PlayIdle();
                    break;
                case 1:
                    animationController.PlayHeadPain();
                    Debug.Log("changing to pain");
                    break;
                case 2:
                    animationController.PlayHappy();
                    break;
                case 3:
                    animationController.PlayShrug();
                    break;
                case 4:
                    animationController.PlayHeadNod();
                    break;
                case 5:
                    animationController.PlayHeadShake();
                    break;
                case 6:
                    animationController.PlayWrithingInPain();
                    break;
                case 7:
                    animationController.PlaySad();
                    break;
                case 8:
                    animationController.PlayArmStretch();
                    break;
                case 9:
                    animationController.PlayNeckStretch();
                    break;
                case 10:
                    animationController.PlayBloodPressure();
                    if (bloodEffectController != null)
                        bloodEffectController.SetBloodVisibility(true);
                    if (bloodTextController != null)
                        bloodTextController.SetBloodTextVisibility(true);
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