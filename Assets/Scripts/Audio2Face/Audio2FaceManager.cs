using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Create classes for JSON responses
[Serializable]
public class NvidiaApiResponse
{
    public string id;
    public string status;
    public string result_url;
    public string message;
    public NvidiaResult result;
}

[Serializable]
public class NvidiaResult
{
    public string csv_url;
    public string animation_data;
}

[Serializable]
public class NvidiaErrorResponse
{
    public string error;
    public string message;
}

public class Audio2FaceManager : MonoBehaviour
{
    public static Audio2FaceManager Instance { get; private set; }
    
    [Header("Local Python Script Configuration")]
    [Tooltip("Path to the Python script")]
    public string pythonScriptPath = "E:\\Unity\\Unity Projects\\Audio2Face-3D-Samples\\scripts\\audio2face_3d_api_client\\nim_a2f_3d_client.py";
    
    [Tooltip("Path to the config directory")]
    public string configDirectoryPath = "E:\\Unity\\Unity Projects\\Audio2Face-3D-Samples\\scripts\\audio2face_3d_api_client\\config";
    
    [Tooltip("Path to Python executable")]
    public string pythonExecutablePath = "E:\\Unity\\Unity Projects\\a2f\\.venv\\Scripts\\python.exe";
    
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

    // Define the enum outside of the field declaration
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
    
    // For tracking API job status
    private string currentJobId = "";
    private float pollInterval = 2.0f; // seconds between status checks
    private Process currentProcess;
    
    void Awake()
    {
        tempDirectory = Path.Combine(Application.temporaryCachePath, "Audio2FaceTemp");
        // Ensure temp directory exists
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
            UnityEngine.Debug.Log($"Created temporary directory: {tempDirectory}");
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
            // First try to get it from the EnvironmentLoader
            apiKey = EnvironmentLoader.GetEnvVariable("NVIDIA_API_KEY");
            
            // If that didn't work, try System.Environment directly
            if (string.IsNullOrEmpty(apiKey))
            {
                try {
                    apiKey = System.Environment.GetEnvironmentVariable("NVIDIA_API_KEY");
                    UnityEngine.Debug.Log("Tried loading API key directly from System.Environment");
                } catch (Exception ex) {
                    UnityEngine.Debug.LogWarning($"Error accessing environment variable: {ex.Message}");
                }
            }
            
            // If still empty, try the hardcoded value from your .env file
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = "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";
                UnityEngine.Debug.Log("Using hardcoded API key as fallback");
            }
            
            UnityEngine.Debug.Log($"API Key is {(string.IsNullOrEmpty(apiKey) ? "empty" : $"{apiKey.Length} characters long")}");
            
            if (string.IsNullOrEmpty(apiKey))
            {
                UnityEngine.Debug.LogWarning("No NVIDIA API key found. Please set it in the inspector or as an environment variable.");
            }
        }
        
        UnityEngine.Debug.Log($"Audio2Face Manager initialized. Using temporary directory: {tempDirectory}");
    }
    
    void Start()
    {
        // Find the animation controller if not assigned
        if (animationController == null)
        {
            animationController = FindObjectOfType<CSVFacialAnimationController>();
            if (animationController == null)
            {
                UnityEngine.Debug.LogError("No CSVFacialAnimationController found in the scene. Animations won't be applied.");
            }
        }

        // Find the TTS manager if not assigned
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<TTSManager>();
            if (ttsManager == null && autoProcessTTS)
            {
                UnityEngine.Debug.LogWarning("No TTSManager found in the scene, but autoProcessTTS is enabled. Auto-processing will not work.");
            }
        }

        // Subscribe to TTS events if available and auto-processing is enabled
        if (ttsManager != null && autoProcessTTS)
        {
            // We'll use a coroutine to periodically check for TTS audio
            StartCoroutine(CheckForTTSAudio());
        }

        UnityEngine.Debug.Log("Audio2FaceManager ready.");
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
                    UnityEngine.Debug.Log($"New TTS audio detected: {currentClip.name}");
                    lastProcessedClip = currentClip;
                    
                    // Wait a short time to ensure the clip is fully generated
                    yield return new WaitForSeconds(0.5f);
                    
                    // Process this clip for facial animation using the safe method
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
            
            // Try the audioSource field if available
            var audioSourceField = ttsManager.GetType().GetField("audioSource", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic);
            if (audioSourceField != null)
            {
                AudioSource src = audioSourceField.GetValue(ttsManager) as AudioSource;
                if (src != null && src.clip != null)
                    return src.clip;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Error accessing TTS clip: {ex.Message}");
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

    // Function to get the correct config file based on the selected model
    public string GetConfigFilePath()
    {
        switch (selectedModel)
        {
            case NvidiaModel.Mark:
                return Path.Combine(configDirectoryPath, "config_mark.yml");
            case NvidiaModel.Claire:
                return Path.Combine(configDirectoryPath, "config_claire.yml");
            case NvidiaModel.James:
                return Path.Combine(configDirectoryPath, "config_james.yml");
            default:
                return Path.Combine(configDirectoryPath, "config_claire.yml"); // Default to Claire
        }
    }

    /// <summary>
    /// Process audio data to generate facial animation through local Python script
    /// </summary>
    /// <param name="audioData">The raw audio data bytes</param>
    /// <returns>True if processing was successful</returns>
    public async Task<bool> ProcessAudioForFacialAnimation(byte[] audioData, string messageContent = null)
    {
        if (audioData == null || audioData.Length == 0)
        {
            UnityEngine.Debug.LogError("Cannot process audio: Audio data is null or empty!");
            return false;
        }
        
        // Double-check API key availability
        if (string.IsNullOrEmpty(apiKey))
        {
            // Try one more time to load the API key as a fallback
            apiKey = "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";
            UnityEngine.Debug.Log("Using hardcoded API key in ProcessAudioForFacialAnimation");
        }
        
        if (string.IsNullOrEmpty(apiKey) && !useDummyData)
        {
            UnityEngine.Debug.LogError("Cannot process audio: No NVIDIA API key provided!");
            return false;
        }
        
        UnityEngine.Debug.Log($"Using API key (first 10 chars): {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
        
        // Make sure temp directory exists
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }
        
        // Save the audio to a temporary file for processing
        tempAudioPath = Path.Combine(tempDirectory, $"temp_audio_{DateTime.Now.Ticks}.wav");
        File.WriteAllBytes(tempAudioPath, audioData);
        
        UnityEngine.Debug.Log($"Saved temporary audio file to {tempAudioPath}");
        
        // Generate a unique name for the CSV output
        string csvFilename = $"facial_animation_{DateTime.Now.Ticks}.csv";
        csvOutputPath = Path.Combine(tempDirectory, csvFilename);
        
        bool success;
        
        // Use dummy data if specified (for testing without API)
        if (useDummyData)
        {
            UnityEngine.Debug.Log("Using dummy data for animation");
            success = GenerateDummyCsvFile(csvOutputPath);
        }
        else
        {
            // Call local Python script to generate animation
            success = await RunPythonScriptForAnimation(tempAudioPath, csvOutputPath);
        }
        
        if (success && autoLoadAnimation)
        {
            // Load the generated animation into the controller
            LoadAnimationIntoController(tempAudioPath, csvOutputPath, messageContent);
        }
        
        return success;
    }
    
    /// <summary>
    /// Integration method to use TTS audio directly from TTSManager
    /// </summary>
    /// <returns>True if processing was successful</returns>
    public async Task<bool> ProcessTTSAudioForFacialAnimation()
    {
        try
        {
            // Find the TTSManager component if it exists in the scene
            if (ttsManager == null)
            {
                ttsManager = FindObjectOfType<TTSManager>();
            }
            
            if (ttsManager == null)
            {
                UnityEngine.Debug.LogError("Cannot process TTS audio: No TTSManager found in the scene!");
                return false;
            }
            
            // Get the audio clip from TTSManager
            AudioClip ttsAudioClip = GetCurrentAudioClipFromTTS();
            
            if (ttsAudioClip == null)
            {
                UnityEngine.Debug.LogError("Cannot process TTS audio: No audio clip available from TTSManager!");
                return false;
            }
            
            UnityEngine.Debug.Log($"Retrieved TTS audio clip: {ttsAudioClip.name}, length: {ttsAudioClip.length}s");
            
            // Convert AudioClip to WAV byte array
            byte[] audioData = await ConvertAudioClipToWav(ttsAudioClip);
            if (audioData == null || audioData.Length == 0)
            {
                UnityEngine.Debug.LogError("Failed to convert TTS audio clip to WAV format!");
                return false;
            }
            
            // Process the audio data
            return await ProcessAudioForFacialAnimation(audioData);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error processing TTS audio: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    private void ConvertAudioClipToWavFile(AudioClip clip, string outputPath)
    {
        try
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
        
            Int16[] intData = new Int16[samples.Length];
            byte[] byteData = new byte[samples.Length * 2];
        
            // Convert to Int16
            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * 32767);
                byteData[i * 2] = (byte)(intData[i] & 0xFF);
                byteData[i * 2 + 1] = (byte)((intData[i] >> 8) & 0xFF);
            }
        
            // Create WAV file with header
            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                using (var writer = new BinaryWriter(fileStream))
                {
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + byteData.Length);
                    writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                    writer.Write(Encoding.ASCII.GetBytes("fmt "));
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)clip.channels);
                    writer.Write(clip.frequency);
                    writer.Write(clip.frequency * clip.channels * 2);
                    writer.Write((short)(clip.channels * 2));
                    writer.Write((short)16);
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write(byteData.Length);
                    writer.Write(byteData);
                }
            }
        
            UnityEngine.Debug.Log($"Alternative WAV creation successful: {outputPath}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Alternative WAV creation failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Modified method to safely process TTS audio for facial animation using WavUtility
    /// This version avoids threading issues with AudioClip.GetData
    /// </summary>
    public void ProcessTTSAudioForAnimationSafe()
    {
        StartCoroutine(ProcessTTSAudioWithWavUtility());
    }

    /// <summary>
    /// Coroutine for processing TTS audio using WavUtility
    /// This approach avoids threading issues with AudioClip.GetData
    /// </summary>
    private IEnumerator ProcessTTSAudioWithWavUtility()
    {
        // Check for TTSManager
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<TTSManager>();
        }
        
        if (ttsManager == null)
        {
            UnityEngine.Debug.LogError("Cannot process TTS audio: No TTSManager found in the scene!");
            yield break;
        }
        
        // Get the audio clip from TTSManager
        AudioClip ttsAudioClip = GetCurrentAudioClipFromTTS();
        
        if (ttsAudioClip == null)
        {
            UnityEngine.Debug.LogError("Cannot process TTS audio: No audio clip available from TTSManager!");
            yield break;
        }
        
        UnityEngine.Debug.Log($"Retrieved TTS audio clip: {ttsAudioClip.name}, length: {ttsAudioClip.length}s, channels: {ttsAudioClip.channels}, frequency: {ttsAudioClip.frequency}");
        
        // Make sure temp directory exists
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }
        
        // Generate paths for our temp files
        string tempWavPath = Path.Combine(tempDirectory, $"temp_audio_{DateTime.Now.Ticks}.wav");
        string csvFilePath = Path.Combine(tempDirectory, $"facial_animation_{DateTime.Now.Ticks}.csv");
        
        // Save the WAV file directly using our PCM 16-bit method (required by Audio2Face)
        try
        {
            UnityEngine.Debug.Log($"Writing WAV file to {tempWavPath}");
            WriteWavPcm16(ttsAudioClip, tempWavPath);
            
            string format = GetAudioFormat(tempWavPath);
            UnityEngine.Debug.Log($"Detected audio format: {format}");
        
            if (format == "MP3")
            {
                // We have an MP3 file - need to convert it
                UnityEngine.Debug.Log("MP3 format detected, converting to WAV...");
                string tempMp3Path = tempWavPath; // Rename for clarity
                string convertedWavPath = Path.Combine(tempDirectory, $"converted_{DateTime.Now.Ticks}.wav");
            
                bool conversionSuccess = ConvertMp3ToWav(tempMp3Path, convertedWavPath);
                if (conversionSuccess && File.Exists(convertedWavPath))
                {
                    UnityEngine.Debug.Log($"Successfully converted MP3 to WAV: {convertedWavPath}");
                    tempWavPath = convertedWavPath; // Update path to use converted file
                }
                else
                {
                    UnityEngine.Debug.LogError("Failed to convert MP3 to WAV");
                    // Continue with fallback options
                }
            }
            else if (format != "WAV")
            {
                UnityEngine.Debug.LogError($"Unexpected audio format: {format}");
                // Try alternative method
                ConvertAudioClipToWavFile(ttsAudioClip, tempWavPath);
            
                // Check again
                format = GetAudioFormat(tempWavPath);
                if (format != "WAV")
                {
                    UnityEngine.Debug.LogError("Failed to create valid WAV file");
                    yield break;
                }
            }
            
            // Verify file was created properly
            if (!File.Exists(tempWavPath) || new FileInfo(tempWavPath).Length < 44)
            {
                UnityEngine.Debug.LogError($"Failed to create valid WAV file at {tempWavPath}");
                
                if (useDummyData)
                {
                    UnityEngine.Debug.Log("Using dummy data as fallback");
                    bool dummySuccess = GenerateDummyCsvFile(csvFilePath);
                    if (dummySuccess && autoLoadAnimation)
                    {
                        LoadAnimationIntoController(tempWavPath, csvFilePath);
                    }
                }
                yield break;
            }
            
            UnityEngine.Debug.Log($"WAV file created successfully: {tempWavPath}, size: {new FileInfo(tempWavPath).Length} bytes");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error writing WAV file: {ex.Message}");
            yield break;
        }
        
        // Save the CSV output path
        csvOutputPath = csvFilePath;
        
        // Now run the Python script with our WAV file
        bool success;
        if (useDummyData)
        {
            UnityEngine.Debug.Log("Using dummy data for animation");
            success = GenerateDummyCsvFile(csvOutputPath);
        }
        else
        {
            UnityEngine.Debug.Log("Running Audio2Face Python script...");
            Task<bool> task = RunPythonScriptForAnimation(tempWavPath, csvOutputPath);
            
            // Show processing indicator
            int dots = 0;
            while (!task.IsCompleted)
            {
                dots = (dots + 1) % 4;
                string indicator = "Processing" + new string('.', dots);
                UnityEngine.Debug.Log(indicator);
                yield return new WaitForSeconds(1.0f);
            }
            
            success = task.Result;
            UnityEngine.Debug.Log($"Python script completed with result: {success}");
        }
        
        if (success && autoLoadAnimation)
        {
            // Load the generated animation into the controller
            UnityEngine.Debug.Log("Loading animation into controller");
            LoadAnimationIntoController(tempWavPath, csvOutputPath);
        }
        else if (!success)
        {
            UnityEngine.Debug.LogError("Failed to process TTS audio for facial animation");
            
            // Try dummy data as a fallback if actual processing failed
            if (!useDummyData)
            {
                UnityEngine.Debug.Log("Falling back to dummy data after failure");
                bool dummySuccess = GenerateDummyCsvFile(csvOutputPath);
                if (dummySuccess && autoLoadAnimation)
                {
                    LoadAnimationIntoController(tempWavPath, csvOutputPath);
                }
            }
        }
    }
    
    // Coroutine to save AudioClip to file and then load animation
    private IEnumerator SaveAudioClipToFile(AudioClip clip, string outputPath, string csvPath)
    {
        AudioClip.PCMReaderCallback pcmReader = (data) => {
            using (FileStream fs = File.Create(outputPath))
            {
                byte[] bytes = new byte[data.Length * 2];
                int i = 0;
                while (i < data.Length)
                {
                    short value = (short)(data[i] * short.MaxValue);
                    bytes[i * 2] = (byte)(value & 0xFF);
                    bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
                    i++;
                }
                fs.Write(bytes, 0, bytes.Length);
            }
        };
        
        clip.GetData(new float[clip.samples * clip.channels], 0);
        yield return new WaitForSeconds(0.1f);
        
        // Now load into controller
        if (File.Exists(outputPath) && File.Exists(csvPath))
        {
            LoadAnimationIntoController(outputPath, csvPath);
        }
    }

    /// <summary>
    /// Converts an AudioClip to WAV format byte array
    /// </summary>
    public async Task<byte[]> ConvertAudioClipToWav(AudioClip clip)
    {
        return await Task.Run(() => {
            try
            {
                if (clip == null)
                {
                    UnityEngine.Debug.LogError("Cannot convert null AudioClip to WAV");
                    return null;
                }
                
                if (clip.samples <= 0 || clip.channels <= 0)
                {
                    UnityEngine.Debug.LogError($"Invalid AudioClip parameters: samples={clip.samples}, channels={clip.channels}");
                    return null;
                }
                
                // Get raw audio data from the clip
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                
                // Convert to 16-bit PCM
                Int16[] intData = new Int16[samples.Length];
                for (int i = 0; i < samples.Length; i++)
                {
                    intData[i] = (short)(samples[i] * 32767);
                }
                
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(memoryStream))
                    {
                        // Write WAV header
                        writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                        writer.Write(36 + intData.Length * 2);
                        writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                        writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                        writer.Write(16);
                        writer.Write((short)1); // PCM format
                        writer.Write((short)clip.channels);
                        writer.Write(clip.frequency);
                        writer.Write(clip.frequency * clip.channels * 2); // Byte rate
                        writer.Write((short)(clip.channels * 2)); // Block align
                        writer.Write((short)16); // Bits per sample
                        writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                        writer.Write(intData.Length * 2);
                        
                        // Write sample data
                        foreach (short sample in intData)
                        {
                            writer.Write(sample);
                        }
                    }
                    
                    UnityEngine.Debug.Log($"Successfully converted AudioClip to WAV format ({memoryStream.Length} bytes)");
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error converting AudioClip to WAV: {ex.Message}");
                return null;
            }
        });
    }
    
    /// <summary>
    /// Creates a dummy CSV file for testing without the API
    /// </summary>
    public bool GenerateDummyCsvFile(string outputPath)
    {
        try
        {
            UnityEngine.Debug.Log("Generating dummy CSV file for testing");
            
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
            
            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Write to file
            File.WriteAllText(outputPath, sb.ToString());
            UnityEngine.Debug.Log($"Dummy CSV file created at: {outputPath}");
            
            return true;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error generating dummy CSV file: {ex.Message}");
            return false;
        }
    }
    
    private string GetAudioFormat(string filePath)
    {
        try
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] header = new byte[4];
                stream.Read(header, 0, 4);
                string headerStr = Encoding.ASCII.GetString(header);
            
                if (headerStr == "RIFF")
                    return "WAV";
                return headerStr.StartsWith("ID3") ? "MP3" : "UNKNOWN";
            }
        }
        catch
        {
            return "ERROR";
        }
    }
    
    private bool ConvertMp3ToWav(string inputMp3Path, string outputWavPath)
{
    UnityEngine.Debug.Log($"Converting MP3 to WAV: {inputMp3Path} -> {outputWavPath}");
    
    // Create a very simple Python script to handle the conversion
    string pythonScript = @"
import sys
import os
import wave
from pydub import AudioSegment

def convert_mp3_to_wav(input_path, output_path):
    try:
        audio = AudioSegment.from_mp3(input_path)
        audio = audio.set_channels(1)  # Convert to mono
        audio = audio.set_frame_rate(16000)  # Set to 16kHz
        audio.export(output_path, format='wav')
        print(f'Successfully converted {input_path} to {output_path}')
        return True
    except Exception as e:
        print(f'Error converting MP3 to WAV: {e}')
        return False

if __name__ == '__main__':
    if len(sys.argv) < 3:
        print('Usage: python script.py input.mp3 output.wav')
        sys.exit(1)
    
    success = convert_mp3_to_wav(sys.argv[1], sys.argv[2])
    sys.exit(0 if success else 1)
";

    string tempDir = Path.Combine(Application.temporaryCachePath, "Audio2FaceTemp");
    string converterScriptPath = Path.Combine(tempDir, "mp3_to_wav_converter.py");
    
    try
    {
        // Create converter script
        File.WriteAllText(converterScriptPath, pythonScript);
        
        // Run Python script to convert MP3 to WAV
        var psi = new ProcessStartInfo
        {
            FileName = pythonExecutablePath,
            Arguments = $"\"{converterScriptPath}\" \"{inputMp3Path}\" \"{outputWavPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using (var process = new Process { StartInfo = psi })
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            UnityEngine.Debug.Log($"Conversion output: {output}");
            if (!string.IsNullOrEmpty(error))
                UnityEngine.Debug.LogError($"Conversion error: {error}");
                
            return process.ExitCode == 0;
        }
    }
    catch (Exception ex)
    {
        UnityEngine.Debug.LogError($"Error in MP3 to WAV conversion: {ex.Message}");
        return false;
    }
}
    
    private void WriteWavPcm16(AudioClip clip, string path)
    {
        try
        {
            // Validate clip
            if (clip == null || clip.samples <= 0 || clip.channels <= 0)
            {
                UnityEngine.Debug.LogError($"Invalid AudioClip: {(clip == null ? "null" : $"samples={clip.samples}, channels={clip.channels}")}");
                return;
            }
            
            UnityEngine.Debug.Log($"Writing WAV file for clip: {clip.name}, samples={clip.samples}, channels={clip.channels}, frequency={clip.frequency}");
            
            // Get the raw PCM float data
            float[] samples = new float[clip.samples * clip.channels];
            bool success = clip.GetData(samples, 0);
            
            if (!success || samples.Length == 0)
            {
                UnityEngine.Debug.LogError("Failed to get audio sample data from clip");
                return;
            }
            
            // Write proper WAV file with RIFF header
            using (var fileStream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(fileStream))
            {
                // RIFF header
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2); // File size - 8
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                
                // Format chunk
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Chunk size
                writer.Write((short)1); // PCM format
                writer.Write((short)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2); // Byte rate
                writer.Write((short)(clip.channels * 2)); // Block align
                writer.Write((short)16); // Bits per sample
                
                // Data chunk
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2); // Chunk size
                
                // Convert float samples to 16-bit PCM
                for (int i = 0; i < samples.Length; i++)
                {
                    // Clamp to [-1.0, 1.0] and convert to short
                    short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                    writer.Write(value);
                }
            }
            
            // Verify file was written correctly
            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] header = new byte[4];
                    stream.Read(header, 0, 4);
                    string headerStr = Encoding.ASCII.GetString(header);
                    UnityEngine.Debug.Log($"WAV file header check: {headerStr}");
                    
                    if (headerStr != "RIFF")
                    {
                        UnityEngine.Debug.LogError($"Invalid WAV header: {headerStr}");
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Successfully wrote WAV file: {path}, size: {new FileInfo(path).Length} bytes");
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to create WAV file at {path}");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error writing WAV file: {ex.Message}\n{ex.StackTrace}");
        }
    }    

    private bool IsValidWavFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;
            
            using (var stream = File.OpenRead(filePath))
            {
                if (stream.Length < 44) // Minimum WAV header size
                    return false;
                
                byte[] header = new byte[4];
                stream.Read(header, 0, 4);
                string headerStr = Encoding.ASCII.GetString(header);
            
                // Skip file size
                stream.Seek(4, SeekOrigin.Current);
            
                // Check WAVE format
                byte[] waveHeader = new byte[4];
                stream.Read(waveHeader, 0, 4);
                string waveStr = Encoding.ASCII.GetString(waveHeader);
            
                return headerStr == "RIFF" && waveStr == "WAVE";
            }
        }
        catch
        {
            return false;
        }
    }
    
    // Simplified version that focuses on the correct location for animation_frames.csv
public async Task<bool> RunPythonScriptForAnimation(string audioFilePath, string outputCsvPath)
{
    try
    {
        // Skip all Python execution and use dummy data if configured
        if (useDummyData)
        {
            UnityEngine.Debug.Log("Using dummy data as configured");
            return GenerateDummyCsvFile(outputCsvPath);
        }

        if (!File.Exists(audioFilePath))
        {
            UnityEngine.Debug.LogError($"Audio file not found: {audioFilePath}");
            return GenerateDummyCsvFile(outputCsvPath);
        }

        if (!File.Exists(pythonScriptPath))
        {
            UnityEngine.Debug.LogError($"Python script not found: {pythonScriptPath}");
            return GenerateDummyCsvFile(outputCsvPath);
        }

        string configFilePath = GetConfigFilePath();
        if (!File.Exists(configFilePath))
        {
            UnityEngine.Debug.LogError($"Config file not found: {configFilePath}");
            return GenerateDummyCsvFile(outputCsvPath);
        }

        UnityEngine.Debug.Log($"Running actual Audio2Face Python client with script: {pythonScriptPath}");
        UnityEngine.Debug.Log($"Audio file: {audioFilePath}");
        UnityEngine.Debug.Log($"Config file: {configFilePath}");
        UnityEngine.Debug.Log($"Function ID: {GetFunctionId()}");

        // Run the real NVIDIA Python script
        var psi = new ProcessStartInfo
        {
            FileName = pythonExecutablePath,
            Arguments = $"\"{pythonScriptPath}\" \"{audioFilePath}\" \"{configFilePath}\" --apikey \"{apiKey}\" --function-id {GetFunctionId()}",
            WorkingDirectory = Path.GetDirectoryName(pythonScriptPath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        UnityEngine.Debug.Log($"Running command: {psi.FileName} {psi.Arguments}");
        UnityEngine.Debug.Log($"Working directory: {psi.WorkingDirectory}");

        // Create and start the process
        currentProcess = new Process { StartInfo = psi };
        
        // Capture output
        StringBuilder outputBuilder = new StringBuilder();
        StringBuilder errorBuilder = new StringBuilder();
        
        currentProcess.OutputDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data))
            {
                outputBuilder.AppendLine(args.Data);
                UnityEngine.Debug.Log($"Python output: {args.Data}");
            }
        };
        
        currentProcess.ErrorDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data))
            {
                errorBuilder.AppendLine(args.Data);
                UnityEngine.Debug.LogError($"Python error: {args.Data}");
            }
        };
        
        try
        {
            currentProcess.Start();
            currentProcess.BeginOutputReadLine();
            currentProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to start Python process: {ex.Message}");
            return GenerateDummyCsvFile(outputCsvPath);
        }

        // Wait for process to complete
        bool completedInTime = await Task.Run(() => {
            return currentProcess.WaitForExit(120000); // 2 minute timeout
        });
        
        if (!completedInTime)
        {
            try { currentProcess.Kill(); } catch {}
            UnityEngine.Debug.LogError("Python script timed out and was terminated.");
            return GenerateDummyCsvFile(outputCsvPath);
        }

        // Process is complete
        int exitCode = currentProcess.ExitCode;
        currentProcess.Close();
        currentProcess = null;
        
        UnityEngine.Debug.Log($"Python process completed with exit code: {exitCode}");
        
        if (exitCode != 0)
        {
            UnityEngine.Debug.LogError($"Python script failed with exit code {exitCode}");
            UnityEngine.Debug.LogError($"Error details: {errorBuilder.ToString()}");
            return GenerateDummyCsvFile(outputCsvPath);
        }
        
        // Look for timestamp directories directly in the script directory path
        string scriptDir = Path.GetDirectoryName(pythonScriptPath);
        UnityEngine.Debug.Log($"Looking for timestamp directories in: {scriptDir}");
        
        // Get all directories in the script path
        var allDirectories = Directory.GetDirectories(scriptDir);
        
        // Filter for timestamp directories (format: YYYYMMDD_HHMMSS_XXXXXX) and sort by creation time
        var timestampDirs = allDirectories
            .Where(d => {
                string dirName = Path.GetFileName(d);
                return System.Text.RegularExpressions.Regex.IsMatch(dirName, @"^\d{8}_\d{6}_\d+$");
            })
            .OrderByDescending(Directory.GetCreationTime)
            .ToList();
        
        UnityEngine.Debug.Log($"Found {timestampDirs.Count} timestamp directories");
        foreach (var dir in timestampDirs.Take(3))
        {
            UnityEngine.Debug.Log($"Timestamp dir: {dir}, Created: {Directory.GetCreationTime(dir)}");
        }
        
        // Look for animation_frames.csv in the most recent timestamp directory
        string csvFile = null;
        if (timestampDirs.Count > 0)
        {
            string mostRecentDir = timestampDirs[0];
            string potentialCsvFile = Path.Combine(mostRecentDir, "animation_frames.csv");
            
            if (File.Exists(potentialCsvFile))
            {
                csvFile = potentialCsvFile;
                UnityEngine.Debug.Log($"Found animation CSV in most recent timestamp directory: {csvFile}");
                
                // Print the first few lines for debugging
                try
                {
                    using (StreamReader reader = new StreamReader(csvFile))
                    {
                        UnityEngine.Debug.Log("CSV content preview (first 3 lines):");
                        for (int i = 0; i < 3 && !reader.EndOfStream; i++)
                        {
                            UnityEngine.Debug.Log(reader.ReadLine());
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error reading CSV preview: {ex.Message}");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"animation_frames.csv not found in most recent directory: {mostRecentDir}");
            }
        }
        
        // If we found a timestamp directory with animation_frames.csv
        if (csvFile != null && File.Exists(csvFile))
        {
            try
            {
                // Create output directory if it doesn't exist
                string outputDir = Path.GetDirectoryName(outputCsvPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                File.Copy(csvFile, outputCsvPath, true);
                UnityEngine.Debug.Log($"Successfully copied animation CSV to: {outputCsvPath}");
        
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error processing CSV file: {ex.Message}");
                return GenerateDummyCsvFile(outputCsvPath);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("No valid animation_frames.csv found in timestamp directories");
            return GenerateDummyCsvFile(outputCsvPath);
        }
    }
    catch (Exception ex)
    {
        UnityEngine.Debug.LogError($"Exception in RunPythonScriptForAnimation: {ex.Message}\n{ex.StackTrace}");
        return GenerateDummyCsvFile(outputCsvPath);
    }
}

    /// <summary>
    /// Loads the generated animation and audio into the facial animation controller
    /// </summary>
    public void LoadAnimationIntoController(string audioPath, string csvPath, string messageContent = null)
    {
        if (animationController == null)
        {
            UnityEngine.Debug.LogError("Can't load animation: No CSVFacialAnimationController reference");
            return;
        }
        
        if (!File.Exists(audioPath) || !File.Exists(csvPath))
        {
            UnityEngine.Debug.LogError($"Cannot load animation: Missing files. Audio exists: {File.Exists(audioPath)}, CSV exists: {File.Exists(csvPath)}");
            return;
        }
        
        StartCoroutine(LoadAnimationCoroutine(audioPath, csvPath, messageContent));
    }
    
    /// <summary>
    /// Coroutine to load animation safely without try/catch around yield statements
    /// </summary>
    // Properly fixed version that avoids yield inside try blocks
private IEnumerator LoadAnimationCoroutine(string audioPath, string csvPath, string messageContent = null)
{
    // Log file information
    UnityEngine.Debug.Log($"Loading animation - Audio file exists: {File.Exists(audioPath)}, Size: {(File.Exists(audioPath) ? new FileInfo(audioPath).Length : 0)} bytes");
    UnityEngine.Debug.Log($"Loading animation - CSV file exists: {File.Exists(csvPath)}, Size: {(File.Exists(csvPath) ? new FileInfo(csvPath).Length : 0)} bytes");
    
    // Check if animation controller is null
    if (animationController == null)
    {
        UnityEngine.Debug.LogError("Animation controller is null!");
        yield break;
    }
    
    AudioClip clip = null;
    bool shouldPlayAudio = true;
    
    // Load the audio as a proper AudioClip
    if (File.Exists(audioPath))
    {
        // Use UnityWebRequest to load the audio file
        UnityEngine.Debug.Log($"Loading audio file from {audioPath}");
        string uriPath = "file://" + audioPath;

        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.WAV);
        if (www == null)
        {
            UnityEngine.Debug.LogError("UnityWebRequest is null!");
            shouldPlayAudio = false;
        }
        else
        {
            // Wait for request without a try-catch
            yield return www.SendWebRequest();

            // Process result after the yield completes
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip == null)
                    {
                        UnityEngine.Debug.LogError("Failed to get audio clip from web request!");
                        shouldPlayAudio = false;
                    }
                    else
                    {
                        UnityEngine.Debug.Log(
                            $"Successfully loaded audio clip: Name={clip.name}, Length={clip.length}s, Channels={clip.channels}, Frequency={clip.frequency}Hz");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error getting audio content: {ex.Message}");
                    shouldPlayAudio = false;
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to load audio as WAV: {www.error}");
                shouldPlayAudio = false;
            }
        }
    }
    else
    {
        UnityEngine.Debug.LogError($"Audio file does not exist: {audioPath}");
        shouldPlayAudio = false;
    }
    
    // Create a silent clip as fallback if we didn't load one
    if (clip == null)
    {
        try
        {
            UnityEngine.Debug.Log("Creating silent fallback audio clip");
            clip = AudioClip.Create("SilentFallback", 48000, 1, 48000, false);
            if (clip == null)
            {
                UnityEngine.Debug.LogError("Failed to create silent fallback clip!");
                yield break;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error creating silent clip: {ex.Message}");
            yield break;
        }
    }
    
    // Load the CSV file
    TextAsset csvAsset = null;
    if (File.Exists(csvPath))
    {
        try
        {
            string csvContent = File.ReadAllText(csvPath);
            if (string.IsNullOrEmpty(csvContent))
            {
                UnityEngine.Debug.LogError("CSV file is empty!");
            }
            else
            {
                csvAsset = new TextAsset(csvContent);
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error reading CSV file: {ex.Message}");
        }
    }
    else
    {
        UnityEngine.Debug.LogError($"CSV file does not exist: {csvPath}");
    }
    
    // Continue with loading the animation if we have both an audio clip and CSV asset
    if (clip != null && csvAsset != null)
    {
        try
        {
            UnityEngine.Debug.Log("Loading animation with audio and CSV");
            
            //
            if (ttsManager != null && ttsManager.audioSource != null && ttsManager.audioSource.isPlaying)
            {
                ttsManager.audioSource.Stop();
                UnityEngine.Debug.Log("Stopped existing audio playback");
            }
            
            StopAllCoroutines();
            animationController.StopAllCoroutines();
            //
            
            // Assign the audio clip to the animation controller
            UnityEngine.Debug.Log("Setting audioClip on animation controller");
            animationController.audioClip = clip;
            
            // Assign the CSV asset to the animation controller
            UnityEngine.Debug.Log("Setting animationCSV on animation controller");
            animationController.animationCSV = csvAsset;
            
            // Set animation playback speed
            UnityEngine.Debug.Log($"Setting playback speed to {animationPlaybackSpeed * 30f}");
            animationController.playbackSpeed = animationPlaybackSpeed * 30f;
            
            // Set animation scale
            UnityEngine.Debug.Log($"Setting animation scale to {animationStrength}");
            animationController.animationScale = animationStrength;
            
            // Update emotion if we have message content
            if (ttsManager != null && !string.IsNullOrEmpty(messageContent))
            {
                UnityEngine.Debug.Log($"Updating animation with message content: {messageContent}");
    
                try
                {
                    // Check if animationController exists in TTSManager
                    if (ttsManager.GetComponent<CharacterAnimationController>() != null)
                    {
                        ttsManager.UpdateAnimation(messageContent);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("TTSManager lacks CharacterAnimationController, skipping UpdateAnimation call");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error in ttsManager.UpdateAnimation: {ex.Message}");
                    // Continue with animation despite emotion update failure
                }
            }
            else if (ttsManager == null)
            {
                UnityEngine.Debug.LogWarning("TTSManager is null, cannot update animation!");
            }
            
            // Play the audio through the TTSManager's audio source
            if (shouldPlayAudio && ttsManager != null)
            {
                if (ttsManager.audioSource != null)
                {
                    UnityEngine.Debug.Log("Playing audio through TTSManager.audioSource");
                    ttsManager.audioSource.loop = false;
                    ttsManager.audioSource.clip = clip;
                    ttsManager.audioSource.Play();
                    UnityEngine.Debug.Log($"Audio playing, length: {clip.length}s");
                }
                else
                {
                    UnityEngine.Debug.LogError("TTSManager.audioSource is null!");
                }
            }
            
            // Restart the animation with new data
            UnityEngine.Debug.Log("Calling RestartAnimation on animation controller");
            animationController.RestartAnimation();
            UnityEngine.Debug.Log("Animation started with the real generated data");
            
            // Delete temporary files after loading if enabled
            if (deleteTempFiles)
            {
                float clipLength = clip.length;
                UnityEngine.Debug.Log($"Scheduling temp file deletion after {clipLength + 1f} seconds");
                StartCoroutine(DeleteTempFilesAfterDelay(audioPath, csvPath, clipLength + 1f));
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error loading animation into controller: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }
    else
    {
        UnityEngine.Debug.LogError($"Failed to load animation - clip is {(clip == null ? "null" : "valid")} and csvAsset is {(csvAsset == null ? "null" : "valid")}");
    }
    
    yield return null;
}
    
    /// <summary>
    /// Coroutine to delete temporary files after a delay
    /// </summary>
    public IEnumerator DeleteTempFilesAfterDelay(string audioPath, string csvPath, float delay)
    {
        // float safetyBuffer = 5.0f; // 5 seconds safety buffer
        // float totalDelay = delay + safetyBuffer;
        //
        // UnityEngine.Debug.Log($"Will delete temporary files after {totalDelay} seconds (audio length: {delay-1f}s + safety buffer: {safetyBuffer}s)");
        
        // Wait until animation is finished playing
        yield return new WaitForSeconds(delay);
        
        // Check if animation controller is still using the animation
        bool isAnimationPlaying = false;
        
        // Use the public method to check if animation is playing
        if (animationController != null)
        {
            isAnimationPlaying = animationController.IsAnimationPlaying();
            
            if (isAnimationPlaying)
            {
                // Wait a bit more if still playing
                yield return new WaitForSeconds(2.0f);
            }
        }
        
        // Double-check that the files still exist and aren't being used by other processes
        if (!IsFileInUse(audioPath) && !IsFileInUse(csvPath))
        {
            try
            {
                // Delete the temporary files
                if (File.Exists(audioPath))
                {
                    File.Delete(audioPath);
                    UnityEngine.Debug.Log($"Deleted temporary audio file: {audioPath}");
                }
                
                if (File.Exists(csvPath))
                {
                    File.Delete(csvPath);
                    UnityEngine.Debug.Log($"Deleted temporary CSV file: {csvPath}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to delete temporary files: {ex.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Files still in use, skipping deletion");
        }
    }

    /// <summary>
    /// Check if a file is currently in use
    /// </summary>
    private bool IsFileInUse(string filePath)
    {
        if (!File.Exists(filePath))
            return false;
            
        try
        {
            // Try to open the file exclusively
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // File is not in use
                return false;
            }
        }
        catch (IOException)
        {
            // File is in use
            return true;
        }
        catch (Exception)
        {
            // Some other error occurred, assume not in use
            return false;
        }
    }
    
    /// <summary>
    /// Clean up temporary files on destruction
    /// </summary>
    private void OnDestroy()
    {
        // Cancel any running process
        if (currentProcess != null && !currentProcess.HasExited)
        {
            try
            {
                currentProcess.Kill();
                UnityEngine.Debug.Log("Terminated running Python process");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to terminate Python process: {ex.Message}");
            }
        }
        
        // Clean up temporary directory if it exists
        if (Directory.Exists(tempDirectory))
        {
            try
            {
                string[] files = Directory.GetFiles(tempDirectory);
                foreach (string file in files)
                {
                    try
                    {
                        if (!IsFileInUse(file))
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Failed to delete file {file}: {ex.Message}");
                    }
                }
                
                try
                {
                    Directory.Delete(tempDirectory, true);
                    UnityEngine.Debug.Log($"Cleaned up temporary directory: {tempDirectory}");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Failed to delete temporary directory: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to clean up temporary directory: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Cancel the current job if it's still processing
    /// </summary>
    public async Task CancelCurrentJob()
    {
        if (currentProcess != null && !currentProcess.HasExited)
        {
            try
            {
                currentProcess.Kill();
                UnityEngine.Debug.Log("Terminated running Python process");
                currentProcess = null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error terminating Python process: {ex.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.Log("No active process to cancel");
        }
    }
}