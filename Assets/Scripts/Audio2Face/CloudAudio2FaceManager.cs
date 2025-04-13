using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Linq;

public class CloudAudio2FaceManager : MonoBehaviour
{
    public static CloudAudio2FaceManager Instance { get; private set; }
    
    [Header("NVIDIA API Configuration")]
    [Tooltip("Your NVIDIA API key")]
    public string apiKey = "";
    
    [Header("Output Settings")]
    [Tooltip("Whether to automatically load the animation after generating it")]
    public bool autoLoadAnimation = true;
    
    [Tooltip("Whether to delete temporary files after use")]
    public bool deleteTempFiles = true;
    
    [Header("Animation Settings")]
    [Tooltip("Playback speed multiplier for the animation")]
    [Range(0.5f, 2.0f)]
    public float animationPlaybackSpeed = 1.0f;
    
    [Tooltip("Scale factor for animation strength")]
    [Range(0.5f, 2.0f)]
    public float animationStrength = 1.0f;
    
    [Header("Component References")]
    [Tooltip("Reference to the CSVFacialAnimationController")]
    public CSVFacialAnimationController animationController;
    
    [Tooltip("Reference to the TTSManager (optional)")]
    public TTSManager ttsManager;
    
    [Header("Auto Processing Settings")]
    [Tooltip("Whether to automatically generate facial animation when TTS is generated")]
    public bool autoProcessTTS = true;

    // Define the enum for model selection
    public enum NvidiaModel { Mark, Claire, James }
    
    [Header("NVIDIA Model Settings")]
    [Tooltip("Select which model to use for facial animation")]
    public NvidiaModel selectedModel = NvidiaModel.Claire;
    
    [Header("Debug Options")]
    [Tooltip("Generate a dummy CSV file for testing without API calls")]
    public bool useDummyData = false;
    
    [Tooltip("Show detailed debug logs")]
    public bool detailedLogging = false;
    
    // Private state variables
    private string tempAudioPath;
    private string csvOutputPath;
    private string tempDirectory;
    
    // NVIDIA Cloud API endpoints
    private readonly string baseApiUrl = "https://grpc.nvcf.nvidia.com:443/";
    
    void Awake()
    {
        tempDirectory = Path.Combine(Application.temporaryCachePath, "Audio2FaceTemp");
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Load API key from environment if not set
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = EnvironmentLoader.GetEnvVariable("NVIDIA_API_KEY");
            
            if (string.IsNullOrEmpty(apiKey))
            {
                try {
                    apiKey = System.Environment.GetEnvironmentVariable("NVIDIA_API_KEY");
                } catch (Exception ex) {
                    Debug.LogWarning($"Error accessing environment variable: {ex.Message}");
                }
            }
            
            Debug.Log($"API Key is {(string.IsNullOrEmpty(apiKey) ? "empty" : $"{apiKey.Length} characters long")}");
        }
        
        Debug.Log($"Cloud Audio2Face Manager initialized. Using temporary directory: {tempDirectory}");
    }
    
    void Start()
    {
        // Find the animation controller if not assigned
        if (animationController == null)
        {
            animationController = FindObjectOfType<CSVFacialAnimationController>();
            if (animationController == null)
            {
                Debug.LogError("No CSVFacialAnimationController found in the scene. Animations won't be applied.");
            }
        }

        // Find the TTS manager if not assigned
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<TTSManager>();
            if (ttsManager == null && autoProcessTTS)
            {
                Debug.LogWarning("No TTSManager found in the scene, but autoProcessTTS is enabled. Auto-processing will not work.");
            }
        }

        // Subscribe to TTS events if available and auto-processing is enabled
        if (ttsManager != null && autoProcessTTS)
        {
            // We'll use a coroutine to periodically check for TTS audio
            StartCoroutine(CheckForTTSAudio());
        }
    }
    
    // Coroutine to periodically check for new TTS audio
    private IEnumerator CheckForTTSAudio()
    {
        AudioClip lastProcessedClip = null;
        
        while (true)
        {
            if (ttsManager != null)
            {
                AudioClip currentClip = GetCurrentAudioClipFromTTS();
                
                // If we have a new clip that we haven't processed yet
                if (currentClip != null && currentClip != lastProcessedClip)
                {
                    Debug.Log($"New TTS audio detected: {currentClip.name}");
                    lastProcessedClip = currentClip;
                    
                    // Wait a short time to ensure the clip is fully generated
                    yield return new WaitForSeconds(0.5f);
                    
                    // Process this clip for facial animation
                    ProcessTTSAudioForAnimationSafe();
                }
            }
            
            // Check every half second
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// Gets the current audio clip from the TTSManager using various methods
    /// </summary>
    public AudioClip GetCurrentAudioClipFromTTS()
    {
        if (ttsManager == null)
            return null;
        
        AudioClip clip = null;
        
        // Try to get the clip using reflection
        try
        {
            // Try GetCurrentAudioClip method
            var method = ttsManager.GetType().GetMethod("GetCurrentAudioClip", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                clip = method.Invoke(ttsManager, null) as AudioClip;
                if (clip != null)
                    return clip;
            }
            
            // Try audioClip field or property
            var field = ttsManager.GetType().GetField("audioClip", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                clip = field.GetValue(ttsManager) as AudioClip;
                if (clip != null)
                    return clip;
            }
            
            // Try audioSource.clip
            AudioSource audioSource = ttsManager.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                return audioSource.clip;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error accessing TTS clip: {ex.Message}");
        }
        
        return null;
    }
    
    // Function to get the correct function ID based on the selected model
    public string GetFunctionId()
    {
        switch (selectedModel)
        {
            case NvidiaModel.Mark:
                return "b85c53f3-5d18-4edf-8b12-875a400eb798";
            case NvidiaModel.Claire:
                return "a05a5522-3059-4dfd-90e4-4bc1699ae9d4";
            case NvidiaModel.James:
                return "52f51a79-324c-4dbe-90ad-798ab665ad64";
            default:
                return "a05a5522-3059-4dfd-90e4-4bc1699ae9d4"; // Default to Claire
        }
    }

    /// <summary>
    /// Process audio data to generate facial animation through NVIDIA Cloud API
    /// </summary>
    /// <param name="audioData">The raw audio data bytes</param>
    /// <returns>True if processing was successful</returns>
    public async Task<bool> ProcessAudioForFacialAnimation(byte[] audioData)
    {
        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("Cannot process audio: Audio data is null or empty!");
            return false;
        }
        
        // Double-check API key availability
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("Cannot process audio: No NVIDIA API key provided!");
            return false;
        }
        
        // Use dummy data if specified (for testing without API)
        if (useDummyData)
        {
            // Generate a unique name for the CSV output
            string csvFilename = $"facial_animation_{DateTime.Now.Ticks}.csv";
            csvOutputPath = Path.Combine(tempDirectory, csvFilename);
            bool success = GenerateDummyCsvFile(csvOutputPath);
            
            if (success && autoLoadAnimation)
            {
                // Save the audio to a temporary file for the animation controller
                tempAudioPath = Path.Combine(tempDirectory, $"temp_audio_{DateTime.Now.Ticks}.wav");
                File.WriteAllBytes(tempAudioPath, audioData);
                LoadAnimationIntoController(tempAudioPath, csvOutputPath);
            }
            
            return success;
        }
        
        // Save the audio to a temporary file for the API
        tempAudioPath = Path.Combine(tempDirectory, $"temp_audio_{DateTime.Now.Ticks}.wav");
        File.WriteAllBytes(tempAudioPath, audioData);
        
        Debug.Log($"Saved temporary audio file to {tempAudioPath}");
        
        // Generate a unique name for the CSV output
        string csvFilename2 = $"facial_animation_{DateTime.Now.Ticks}.csv";
        csvOutputPath = Path.Combine(tempDirectory, csvFilename2);
        
        // Call the NVIDIA Cloud API
        bool apiSuccess = await CallNvidiaCloudApi(tempAudioPath, csvOutputPath);
        
        if (apiSuccess && autoLoadAnimation)
        {
            // Load the generated animation into the controller
            LoadAnimationIntoController(tempAudioPath, csvOutputPath);
        }
        
        return apiSuccess;
    }
    
    /// <summary>
    /// Call the NVIDIA Cloud API to generate facial animation from audio
    /// </summary>
    private async Task<bool> CallNvidiaCloudApi(string audioFilePath, string outputCsvPath)
    {
        try
        {
            // Check if audio file exists
            if (!File.Exists(audioFilePath))
            {
                Debug.LogError($"Audio file not found: {audioFilePath}");
                return false;
            }
            
            Debug.Log($"Calling NVIDIA Cloud API with audio file: {audioFilePath}");
            
            // Read the audio file as bytes
            byte[] audioBytes = File.ReadAllBytes(audioFilePath);
            
            // Create an HTTP client handler with TLS 1.2 support
            var handler = new HttpClientHandler();
            handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            
            // Prepare the HTTP client with the handler
            using (HttpClient client = new HttpClient(handler))
            {
                // According to the documentation, we should use the gRPC endpoint
                string apiUrl = "https://grpc.nvcf.nvidia.com:443";
                string functionId = GetFunctionId();
                
                // Set timeout to 3 minutes
                client.Timeout = TimeSpan.FromMinutes(3);
                
                // Set headers exactly as required by NVIDIA for gRPC
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
                client.DefaultRequestHeaders.Add("function-id", functionId);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Log request details for debugging
                Debug.Log($"API URL: {apiUrl}");
                Debug.Log($"API Key (first 10 chars): {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
                Debug.Log($"Function ID: {functionId}");
                
                // Create request payload (properly formatted JSON)
                var requestObj = new Dictionary<string, string>
                {
                    { "audio", Convert.ToBase64String(audioBytes) }
                };
                
                string jsonContent = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Send the request
                Debug.Log("Sending API request to NVIDIA Cloud...");
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                
                // Log the complete response for debugging
                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.Log($"API Response Status: {response.StatusCode}");
                
                // Log headers safely without LINQ
                StringBuilder headerLog = new StringBuilder();
                foreach (var header in response.Headers)
                {
                    headerLog.Append($"{header.Key}: ");
                    foreach (var value in header.Value)
                    {
                        headerLog.Append($"{value}, ");
                    }
                    headerLog.Append("; ");
                }
                Debug.Log($"API Response Headers: {headerLog}");
                
                if (responseBody.Length > 1000)
                {
                    Debug.Log($"API Response Body (truncated): {responseBody.Substring(0, 1000)}...");
                }
                else
                {
                    Debug.Log($"API Response Body: {responseBody}");
                }
                
                // Process the response
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("API call successful");
                    
                    try
                    {
                        // Parse the response JSON
                        var responseObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
                        
                        // Extract the animation data
                        if (responseObj != null && responseObj.ContainsKey("data"))
                        {
                            var dataObj = responseObj["data"];
                            string csvData = "";
                            
                            // The structure might vary, try different possible formats
                            if (dataObj is Dictionary<string, object> dataDict)
                            {
                                if (dataDict.ContainsKey("animation_data"))
                                {
                                    csvData = dataDict["animation_data"].ToString();
                                }
                                else if (dataDict.ContainsKey("csv"))
                                {
                                    csvData = dataDict["csv"].ToString();
                                }
                                else if (dataDict.ContainsKey("animation_frames"))
                                {
                                    csvData = dataDict["animation_frames"].ToString();
                                }
                            }
                            else
                            {
                                // Try to get it directly
                                csvData = dataObj.ToString();
                            }
                            
                            if (!string.IsNullOrEmpty(csvData))
                            {
                                // Save the CSV data to a file
                                File.WriteAllText(outputCsvPath, csvData);
                                Debug.Log($"Animation CSV saved to: {outputCsvPath}");
                                
                                return true;
                            }
                            else
                            {
                                Debug.LogError("API response does not contain animation data in an expected format");
                                Debug.Log($"Full response structure: {JsonConvert.SerializeObject(responseObj, Formatting.Indented)}");
                                return false;
                            }
                        }
                        else
                        {
                            Debug.LogError("API response does not contain expected 'data' field");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error parsing API response: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError($"API call failed: {response.StatusCode}, Error: {responseBody}");
                    
                    // Try to parse error response for more details
                    try
                    {
                        var errorObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
                        if (errorObj != null && errorObj.ContainsKey("error"))
                        {
                            Debug.LogError($"API Error: {errorObj["error"]}");
                        }
                    }
                    catch {}
                    
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error calling NVIDIA Cloud API: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
    
    /// <summary>
    /// Process TTS audio for facial animation
    /// </summary>
    public void ProcessTTSAudioForAnimationSafe()
    {
        StartCoroutine(ProcessTTSAudioCoroutine());
    }
    
    /// <summary>
    /// Coroutine for processing TTS audio
    /// </summary>
    private IEnumerator ProcessTTSAudioCoroutine()
    {
        // Check for TTSManager
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<TTSManager>();
        }
        
        if (ttsManager == null)
        {
            Debug.LogError("Cannot process TTS audio: No TTSManager found in the scene!");
            yield break;
        }
        
        // Get the audio clip from TTSManager
        AudioClip ttsAudioClip = GetCurrentAudioClipFromTTS();
        
        if (ttsAudioClip == null)
        {
            Debug.LogError("Cannot process TTS audio: No audio clip available from TTSManager!");
            yield break;
        }
        
        Debug.Log($"Retrieved TTS audio clip: {ttsAudioClip.name}, length: {ttsAudioClip.length}s");
        
        // Make sure temp directory exists
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }
        
        // Convert AudioClip to WAV
        string tempWavPath = Path.Combine(tempDirectory, $"temp_audio_{DateTime.Now.Ticks}.wav");
        
        // Convert AudioClip to WAV
        bool conversionSuccess = false;
        
        try
        {
            // Get raw audio data
            float[] samples = new float[ttsAudioClip.samples * ttsAudioClip.channels];
            ttsAudioClip.GetData(samples, 0);
            
            // Convert to 16-bit PCM
            byte[] wavBytes = ConvertAudioToWav(samples, ttsAudioClip.channels, ttsAudioClip.frequency);
            
            // Save to file
            if (wavBytes != null && wavBytes.Length > 0)
            {
                File.WriteAllBytes(tempWavPath, wavBytes);
                conversionSuccess = true;
                Debug.Log($"Successfully saved TTS audio to WAV file: {tempWavPath}, size: {wavBytes.Length} bytes");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error converting audio clip to WAV: {ex.Message}");
        }
        
        if (!conversionSuccess || !File.Exists(tempWavPath))
        {
            Debug.LogError("Failed to convert AudioClip to WAV format");
            yield break;
        }
        
        // Generate a unique name for the CSV output
        string csvFilename = $"facial_animation_{DateTime.Now.Ticks}.csv";
        csvOutputPath = Path.Combine(tempDirectory, csvFilename);
        
        // Call the NVIDIA Cloud API
        Task<bool> apiTask = CallNvidiaCloudApi(tempWavPath, csvOutputPath);
        
        // Display progress indicator
        int dots = 0;
        while (!apiTask.IsCompleted)
        {
            dots = (dots + 1) % 4;
            string progressDots = new string('.', dots);
            Debug.Log($"Processing{progressDots}");
            yield return new WaitForSeconds(0.5f);
        }
        
        bool success = apiTask.Result;
        
        if (success && autoLoadAnimation)
        {
            // Load the generated animation into the controller
            LoadAnimationIntoController(tempWavPath, csvOutputPath);
        }
        else if (!success)
        {
            Debug.LogError("Failed to process TTS audio for facial animation");
            
            // Try dummy data as a fallback
            Debug.Log("Falling back to dummy data generation");
            bool dummySuccess = GenerateDummyCsvFile(csvOutputPath);
            
            if (dummySuccess && autoLoadAnimation)
            {
                LoadAnimationIntoController(tempWavPath, csvOutputPath);
            }
        }
    }
    
    /// <summary>
    /// Convert audio samples to WAV format
    /// </summary>
    private byte[] ConvertAudioToWav(float[] samples, int channels, int sampleRate)
    {
        try
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // Convert float[] to Int16[]
                    Int16[] intData = new Int16[samples.Length];
                    for (int i = 0; i < samples.Length; i++)
                    {
                        intData[i] = (short)(samples[i] * 32767);
                    }
                    
                    // Calculate sizes
                    int dataSize = intData.Length * 2; // 16-bit = 2 bytes per sample
                    int fileSize = 36 + dataSize;
                    
                    // Write WAV header
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(fileSize - 8); // File size minus 8 bytes for "RIFF" and size
                    writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                    
                    // Format chunk
                    writer.Write(Encoding.ASCII.GetBytes("fmt "));
                    writer.Write(16); // Chunk size
                    writer.Write((short)1); // Audio format (1 = PCM)
                    writer.Write((short)channels);
                    writer.Write(sampleRate);
                    writer.Write(sampleRate * channels * 2); // Byte rate
                    writer.Write((short)(channels * 2)); // Block align
                    writer.Write((short)16); // Bits per sample
                    
                    // Data chunk
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write(dataSize);
                    
                    // Write sample data
                    foreach (short sample in intData)
                    {
                        writer.Write(sample);
                    }
                }
                
                return stream.ToArray();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error converting to WAV: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Creates a dummy CSV file for testing without the API
    /// </summary>
    public bool GenerateDummyCsvFile(string outputPath)
    {
        try
        {
            Debug.Log("Generating dummy CSV file for testing");
            
            // Create a simple CSV with basic animation data
            StringBuilder sb = new StringBuilder();
            
            // Header row
            sb.AppendLine("timeCode,blendShapes.JawOpen,blendShapes.MouthClose,blendShapes.MouthFunnel,blendShapes.MouthPucker,blendShapes.MouthSmile_L,blendShapes.MouthSmile_R,blendShapes.EyeBlink_L,blendShapes.EyeBlink_R");
            
            // Generate 120 frames of dummy animation data (4 seconds at 30fps)
            for (int i = 0; i < 120; i++)
            {
                float time = i / 30f;  // Time in seconds
                float jawValue = 0f;
                float mouthCloseValue = 0f;
                float funnelValue = 0f;
                float puckerValue = 0f;
                float smileLeftValue = 0f;
                float smileRightValue = 0f;
                float eyeBlinkLeftValue = 0f;
                float eyeBlinkRightValue = 0f;
                
                // Create a simple talking pattern
                if (i % 10 < 5)
                {
                    jawValue = Mathf.Sin(i * 0.5f) * 30f;
                    mouthCloseValue = 0f;
                }
                else
                {
                    jawValue = 0f;
                    mouthCloseValue = 20f;
                }
                
                if (i % 15 < 7)
                {
                    funnelValue = Mathf.Cos(i * 0.3f) * 15f;
                }
                
                if (i % 20 > 15)
                {
                    puckerValue = 25f;
                }
                
                // Add occasional smiles
                if (i % 30 > 25)
                {
                    smileLeftValue = smileRightValue = 25f;
                }
                
                // Add occasional blinks
                if (i % 40 > 38)
                {
                    eyeBlinkLeftValue = eyeBlinkRightValue = 100f;
                }
                
                sb.AppendLine($"{time:F3},{jawValue:F1},{mouthCloseValue:F1},{funnelValue:F1},{puckerValue:F1},{smileLeftValue:F1},{smileRightValue:F1},{eyeBlinkLeftValue:F1},{eyeBlinkRightValue:F1}");
            }
            
            // Make sure the output directory exists
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Write to file
            File.WriteAllText(outputPath, sb.ToString());
            Debug.Log($"Dummy CSV file created at: {outputPath}");
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating dummy CSV file: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Loads the generated animation and audio into the facial animation controller
    /// </summary>
    public void LoadAnimationIntoController(string audioPath, string csvPath)
    {
        if (animationController == null)
        {
            Debug.LogError("Can't load animation: No CSVFacialAnimationController reference");
            return;
        }
        
        if (!File.Exists(audioPath) || !File.Exists(csvPath))
        {
            Debug.LogError($"Cannot load animation: Missing files. Audio exists: {File.Exists(audioPath)}, CSV exists: {File.Exists(csvPath)}");
            return;
        }
        
        StartCoroutine(LoadAnimationCoroutine(audioPath, csvPath));
    }
    
    /// <summary>
    /// Coroutine to load animation safely
    /// </summary>
    private IEnumerator LoadAnimationCoroutine(string audioPath, string csvPath)
    {
        // Log file details
        Debug.Log($"Loading animation - Audio file: {audioPath}, CSV file: {csvPath}");
        
        // First try WAV format
        string uriPath = "file://" + audioPath;
        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.WAV);
        yield return www.SendWebRequest();
        
        // Try different audio formats if WAV fails
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Failed to load audio as WAV: {www.error}. Trying other formats...");
            www.Dispose();
            
            // Try MPEG
            www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.MPEG);
            yield return www.SendWebRequest();
            
            // Try OGGVORBIS if MPEG fails
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Failed to load audio as MPEG: {www.error}. Trying OGG...");
                www.Dispose();
                
                www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.OGGVORBIS);
                yield return www.SendWebRequest();
            }
        }
        
        AudioClip clip = null;
        if (www.result == UnityWebRequest.Result.Success)
        {
            clip = DownloadHandlerAudioClip.GetContent(www);
            Debug.Log($"Successfully loaded audio clip: {clip.name}, Length: {clip.length}s");
        }
        else
        {
            Debug.LogError($"Failed to load audio clip: {www.error}");
            www.Dispose();
            yield break;
        }
        
        www.Dispose();
        
        // Load the CSV file
        if (clip != null)
        {
            LoadAnimationFromCSV(clip, csvPath);
            
            // Clean up temporary files if enabled
            if (deleteTempFiles)
            {
                yield return new WaitForSeconds(clip.length + 2f);
                DeleteTemporaryFiles(audioPath, csvPath);
            }
        }
    }
    
    /// <summary>
    /// Loads animation data from a CSV file into the animation controller
    /// </summary>
    private void LoadAnimationFromCSV(AudioClip clip, string csvPath)
    {
        try
        {
            Debug.Log("Loading animation data from CSV");
            
            // Create a TextAsset from the CSV file
            TextAsset csvAsset = new TextAsset(File.ReadAllText(csvPath));
            
            // Set up the animation controller
            animationController.audioClip = clip;
            animationController.animationCSV = csvAsset;
            animationController.playbackSpeed = animationPlaybackSpeed * 30f;
            animationController.animationScale = animationStrength;
            
            // Start the animation
            animationController.RestartAnimation();
            Debug.Log("Animation started successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading animation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Deletes temporary files
    /// </summary>
    private void DeleteTemporaryFiles(string audioPath, string csvPath)
    {
        try
        {
            if (File.Exists(audioPath)) File.Delete(audioPath);
            if (File.Exists(csvPath)) File.Delete(csvPath);
            Debug.Log("Temporary files deleted");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to delete temporary files: {ex.Message}");
        }
    }
}