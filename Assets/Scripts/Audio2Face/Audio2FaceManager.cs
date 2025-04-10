using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

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
    public string pythonScriptPath = "/Users/zhenzhenqin/Documents/NEU/Research/facial-animation/Audio2Face-3D-Samples/scripts/audio2face_3d_api_client/nim_a2f_3d_client.py";
    
    [Tooltip("Path to the config directory")]
    public string configDirectoryPath = "/Users/zhenzhenqin/Documents/NEU/Research/facial-animation/Audio2Face-3D-Samples/scripts/audio2face_3d_api_client/config";
    
    [Tooltip("Path to Python executable")]
    public string pythonExecutablePath = "python";
    
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
    private HttpClient httpClient;
    private string tempDirectory;
    
    // For tracking API job status
    private string currentJobId = "";
    private float pollInterval = 2.0f; // seconds between status checks
    private Process currentProcess;
    
    void Awake()
    {
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
        
        // Initialize HTTP client
        httpClient = new HttpClient();
        
        // Setup temp directory
        tempDirectory = Path.Combine(Application.temporaryCachePath, "Audio2Face_Temp");
        
        // Create the temporary directory if it doesn't exist
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
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
                    Debug.Log("Tried loading API key directly from System.Environment");
                } catch (Exception ex) {
                    Debug.LogWarning($"Error accessing environment variable: {ex.Message}");
                }
            }
            
            // If still empty, try the hardcoded value from your .env file
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = "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";
                Debug.Log("Using hardcoded API key as fallback");
            }
            
            Debug.Log($"API Key is {(string.IsNullOrEmpty(apiKey) ? "empty" : $"{apiKey.Length} characters long")}");
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("No NVIDIA API key found. Please set it in the inspector or as an environment variable.");
            }
        }
        
        Debug.Log($"Audio2Face Manager initialized. Using temporary directory: {tempDirectory}");
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

        // Check if the NVIDIA setup has been completed
        NvidiaAudio2FaceSetup setupManager = FindObjectOfType<NvidiaAudio2FaceSetup>();
        if (setupManager != null)
        {
            // Wait for the setup to complete before starting TTS monitoring
            StartCoroutine(WaitForSetupCompletion(setupManager));
        }
        else
        {
            // No setup manager, proceed with default settings
            Debug.Log("No NvidiaAudio2FaceSetup found. Using existing Python settings if available.");
            
            // Subscribe to TTS events if available and auto-processing is enabled
            if (ttsManager != null && autoProcessTTS)
            {
                // We'll use a coroutine to periodically check for TTS audio
                StartCoroutine(CheckForTTSAudio());
            }
        }
    }

    /// Wait for NVIDIA setup to be complete before proceeding
    private IEnumerator WaitForSetupCompletion(NvidiaAudio2FaceSetup setupManager)
    {
        Debug.Log("Waiting for NVIDIA Audio2Face setup to complete...");
        
        // Wait for the setup to be initialized
        float startTime = Time.time;
        float timeout = 120f; // 2 minute timeout
        
        while (!setupManager.IsSetupComplete() && Time.time - startTime < timeout)
        {
            yield return new WaitForSeconds(1f);
        }
        
        if (setupManager.IsSetupComplete())
        {
            // Update paths from the setup manager
            pythonExecutablePath = setupManager.GetPythonPath();
            pythonScriptPath = setupManager.GetClientScriptPath();
            configDirectoryPath = setupManager.GetConfigDirectoryPath();
            
            Debug.Log($"Using NVIDIA Audio2Face setup paths:");
            Debug.Log($"Python: {pythonExecutablePath}");
            Debug.Log($"Script: {pythonScriptPath}");
            Debug.Log($"Config: {configDirectoryPath}");
            
            // Now we can proceed with TTS audio monitoring
            if (ttsManager != null && autoProcessTTS)
            {
                // We'll use a coroutine to periodically check for TTS audio
                StartCoroutine(CheckForTTSAudio());
            }
        }
        else
        {
            Debug.LogWarning("NVIDIA Audio2Face setup timed out or failed. Using dummy data for animations.");
            useDummyData = true; // Fall back to dummy data
            
            // Still start the TTS monitoring, but it will use dummy data
            if (ttsManager != null && autoProcessTTS)
            {
                StartCoroutine(CheckForTTSAudio());
            }
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
            
            // Try audioClip field
            var field = ttsManager.GetType().GetField("audioClip", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                clip = field.GetValue(ttsManager) as AudioClip;
                if (clip != null)
                    return clip;
            }
            
            // Try to get from audioSource component
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
            // Try one more time to load the API key as a fallback
            apiKey = "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";
            Debug.Log("Using hardcoded API key in ProcessAudioForFacialAnimation");
        }
        
        if (string.IsNullOrEmpty(apiKey) && !useDummyData)
        {
            Debug.LogError("Cannot process audio: No NVIDIA API key provided!");
            return false;
        }
        
        Debug.Log($"Using API key (first 10 chars): {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
        
        // Make sure temp directory exists
        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }
        
        // Save the audio to a temporary file for processing
        tempAudioPath = Path.Combine(tempDirectory, $"temp_audio_{DateTime.Now.Ticks}.wav");
        File.WriteAllBytes(tempAudioPath, audioData);
        
        Debug.Log($"Saved temporary audio file to {tempAudioPath}");
        
        // Generate a unique name for the CSV output
        string csvFilename = $"facial_animation_{DateTime.Now.Ticks}.csv";
        csvOutputPath = Path.Combine(tempDirectory, csvFilename);
        
        bool success;
        
        // Use dummy data if specified (for testing without API)
        if (useDummyData)
        {
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
            LoadAnimationIntoController(tempAudioPath, csvOutputPath);
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
                Debug.LogError("Cannot process TTS audio: No TTSManager found in the scene!");
                return false;
            }
            
            // Get the audio clip from TTSManager
            AudioClip ttsAudioClip = GetCurrentAudioClipFromTTS();
            
            if (ttsAudioClip == null)
            {
                Debug.LogError("Cannot process TTS audio: No audio clip available from TTSManager!");
                return false;
            }
            
            Debug.Log($"Retrieved TTS audio clip: {ttsAudioClip.name}, length: {ttsAudioClip.length}s");
            
            // Convert AudioClip to WAV byte array
            byte[] audioData = await ConvertAudioClipToWav(ttsAudioClip);
            if (audioData == null || audioData.Length == 0)
            {
                Debug.LogError("Failed to convert TTS audio clip to WAV format!");
                return false;
            }
            
            // Process the audio data
            return await ProcessAudioForFacialAnimation(audioData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing TTS audio: {ex.Message}\n{ex.StackTrace}");
            return false;
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
        
        Debug.Log($"Retrieved TTS audio clip: {ttsAudioClip.name}, length: {ttsAudioClip.length}s, channels: {ttsAudioClip.channels}, frequency: {ttsAudioClip.frequency}");
        
        // Generate a temporary WAV file path
        string tempWavPath = Path.Combine(tempDirectory, $"temp_audio_{DateTime.Now.Ticks}.wav");
        string wavFilePath = null;
        bool conversionCompleted = false;
        
        try
        {
            // Use FromAudioClip to save the AudioClip as a WAV file
            byte[] wavBytes = WavUtility.FromAudioClip(ttsAudioClip);
            
            if (wavBytes != null && wavBytes.Length > 0)
            {
                // Save the bytes to the file
                File.WriteAllBytes(tempWavPath, wavBytes);
                wavFilePath = tempWavPath;
                conversionCompleted = true;
                Debug.Log($"Successfully saved TTS audio to WAV file: {wavFilePath}, size: {wavBytes.Length} bytes");
            }
            else
            {
                Debug.LogError("Failed to convert AudioClip to WAV bytes");
                yield break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error converting AudioClip to WAV: {ex.Message}");
            yield break;
        }
        
        // Check if the file was created successfully
        if (string.IsNullOrEmpty(wavFilePath) || !File.Exists(wavFilePath))
        {
            Debug.LogError("Failed to create WAV file from TTS audio!");
            yield break;
        }
        
        // Generate a unique name for the CSV output
        string csvFilename = $"facial_animation_{DateTime.Now.Ticks}.csv";
        csvOutputPath = Path.Combine(tempDirectory, csvFilename);
        
        // Call local Python script to generate animation, passing the WAV file path
        bool success = false;
        Task<bool> task = RunPythonScriptForAnimation(wavFilePath, csvOutputPath);
        
        // Wait for processing to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        success = task.Result;
        
        if (success && autoLoadAnimation)
        {
            // Load the generated animation into the controller
            LoadAnimationIntoController(wavFilePath, csvOutputPath);
        }
        else if (!success)
        {
            Debug.LogError("Failed to process TTS audio for facial animation");
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
                    
                    Debug.Log($"Successfully converted AudioClip to WAV format ({memoryStream.Length} bytes)");
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting AudioClip to WAV: {ex.Message}");
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
            Debug.Log("Generating dummy CSV file for testing");
            
            // Create a simple CSV with basic animation data
            StringBuilder sb = new StringBuilder();
            
            // Header row
            sb.AppendLine("timeCode,blendShapes.JawOpen,blendShapes.MouthClose,blendShapes.MouthFunnel,blendShapes.MouthPucker");
            
            // Generate 60 frames of dummy animation data (2 seconds at 30fps)
            for (int i = 0; i < 60; i++)
            {
                float time = i / 30f;  // Time in seconds
                float jawValue = 0f;
                float mouthCloseValue = 0f;
                float funnelValue = 0f;
                float puckerValue = 0f;
                
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
                
                sb.AppendLine($"{time:F3},{jawValue:F1},{mouthCloseValue:F1},{funnelValue:F1},{puckerValue:F1}");
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
    /// Runs the local Python script to generate facial animation from audio file
    /// </summary>
    public async Task<bool> RunPythonScriptForAnimation(string audioFilePath, string outputCsvPath)
    {
        try
        {
            if (!File.Exists(audioFilePath))
            {
                Debug.LogError($"Audio file not found at path: {audioFilePath}");
                return false;
            }
            
            if (!File.Exists(pythonScriptPath))
            {
                Debug.LogError($"Python script not found at path: {pythonScriptPath}");
                return false;
            }
            
            string configFilePath = GetConfigFilePath();
            if (!File.Exists(configFilePath))
            {
                Debug.LogError($"Config file not found at path: {configFilePath}");
                return false;
            }
            
            string functionId = GetFunctionId();
            
            // Since we're seeing "python: command not found", let's use a different approach
            // First, try to find the python executable on the system
            string pythonPath = pythonExecutablePath;
            bool isMacOS = Application.platform == RuntimePlatform.OSXEditor || 
                          Application.platform == RuntimePlatform.OSXPlayer;
            
            if (isMacOS)
            {
                // Common Python paths on macOS
                string[] commonPythonPaths = new string[] {
                    "/usr/bin/python3",
                    "/usr/local/bin/python3",
                    "/opt/homebrew/bin/python3",
                    "/usr/bin/python",
                    "/usr/local/bin/python",
                    "/opt/homebrew/bin/python"
                };
                
                // Check if any of these paths exist
                foreach (string path in commonPythonPaths)
                {
                    if (File.Exists(path))
                    {
                        pythonPath = path;
                        Debug.Log($"Found Python at: {pythonPath}");
                        break;
                    }
                }
            }
            
            // Create a shell script that uses the absolute path to Python
            string shellScriptPath = Path.Combine(tempDirectory, "run_a2f.sh");
            string scriptDir = Path.GetDirectoryName(pythonScriptPath);
            
            // Create a more robust shell script
            string scriptContent = $@"#!/bin/bash
# Print environment for debugging
echo ""Current directory: $(pwd)""
echo ""PATH: $PATH""
echo ""PYTHONPATH: $PYTHONPATH""

# Try to locate Python
which python3 || which python || echo ""Python not found in PATH""

# Set up environment
export PATH=""$PATH:{scriptDir}:/usr/local/bin:/usr/bin:/opt/homebrew/bin""
export PYTHONPATH=""$PYTHONPATH:{scriptDir}""

# Try explicit Python path first
if [ -f ""{pythonPath}"" ]; then
    echo ""Using Python at {pythonPath}""
    ""{pythonPath}"" ""{pythonScriptPath}"" ""{audioFilePath}"" ""{configFilePath}"" --output ""{outputCsvPath}"" --apikey ""{apiKey}"" --function-id {functionId}
else
    # Fall back to PATH lookup
    echo ""Trying python3 from PATH""
    python3 ""{pythonScriptPath}"" ""{audioFilePath}"" ""{configFilePath}"" --output ""{outputCsvPath}"" --apikey ""{apiKey}"" --function-id {functionId} || \
    python ""{pythonScriptPath}"" ""{audioFilePath}"" ""{configFilePath}"" --output ""{outputCsvPath}"" --apikey ""{apiKey}"" --function-id {functionId} || \
    echo ""Failed to execute Python script""
fi
";
            File.WriteAllText(shellScriptPath, scriptContent);
            
            // Make the script executable
            Process chmodProcess = new Process();
            chmodProcess.StartInfo.FileName = "chmod";
            chmodProcess.StartInfo.Arguments = $"+x \"{shellScriptPath}\"";
            chmodProcess.StartInfo.UseShellExecute = false;
            chmodProcess.StartInfo.CreateNoWindow = true;
            chmodProcess.Start();
            chmodProcess.WaitForExit();
            
            Debug.Log($"Created shell script at {shellScriptPath}");
            
            // Create process info to run the shell script
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "/bin/bash";
            processStartInfo.Arguments = $"\"{shellScriptPath}\"";
            processStartInfo.WorkingDirectory = scriptDir; // Set working directory to script directory
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            
            // Add environment variables
            processStartInfo.EnvironmentVariables["PATH"] = 
                $"{processStartInfo.EnvironmentVariables["PATH"]}:{scriptDir}:/usr/local/bin:/usr/bin:/opt/homebrew/bin";
            processStartInfo.EnvironmentVariables["PYTHONPATH"] = 
                processStartInfo.EnvironmentVariables.ContainsKey("PYTHONPATH") 
                ? $"{processStartInfo.EnvironmentVariables["PYTHONPATH"]}:{scriptDir}" 
                : scriptDir;
            
            if (detailedLogging)
            {
                Debug.Log($"Running command: {processStartInfo.FileName} {processStartInfo.Arguments}");
                Debug.Log($"Working directory: {processStartInfo.WorkingDirectory}");
                Debug.Log($"PATH: {processStartInfo.EnvironmentVariables["PATH"]}");
                Debug.Log($"PYTHONPATH: {processStartInfo.EnvironmentVariables["PYTHONPATH"]}");
            }
            
            // Start the process
            Debug.Log("Starting shell script for Audio2Face processing...");
            currentProcess = new Process();
            currentProcess.StartInfo = processStartInfo;
            
            // Set up event handlers for output and error
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            
            currentProcess.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                    Debug.Log($"Output: {e.Data}"); // Always log output for debugging
                }
            };
            
            currentProcess.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(e.Data);
                    Debug.LogError($"Error: {e.Data}");
                }
            };
            
            // Start the process and begin reading output
            bool started = currentProcess.Start();
            if (!started)
            {
                Debug.LogError("Failed to start the process");
                Debug.LogError("Failed to start the process");
                return false;
            }
            
            currentProcess.BeginOutputReadLine();
            currentProcess.BeginErrorReadLine();
            
            // Wait for the process to complete asynchronously
            await Task.Run(() => {
                if (!currentProcess.WaitForExit(180000)) // 3-minute timeout
                {
                    try
                    {
                        currentProcess.Kill();
                        Debug.LogError("Process took too long and was terminated");
                    }
                    catch { /* Ignore kill errors */ }
                    return;
                }
            });
            
            // Check the exit code
            int exitCode = currentProcess.ExitCode;
            currentProcess.Close();
            currentProcess = null;
            
            // Log all output and error for debugging
            Debug.Log($"Shell script completed with exit code {exitCode}");
            Debug.Log($"Standard output: {output}");
            if (exitCode != 0)
            {
                Debug.LogError($"Error output: {error}");
            }
            
            // Clean up shell script
            try
            {
                File.Delete(shellScriptPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete temporary shell script: {ex.Message}");
            }
            
            if (exitCode != 0)
            {
                // If the script failed, fall back to dummy data
                Debug.LogWarning("Script failed. Falling back to dummy data generation.");
                return GenerateDummyCsvFile(outputCsvPath);
            }
            
            // Check if the output CSV file was created
            if (!File.Exists(outputCsvPath))
            {
                // If the CSV doesn't exist, create a dummy one as a fallback
                Debug.LogWarning("Script completed but did not create the output CSV file. Generating dummy data as fallback.");
                return GenerateDummyCsvFile(outputCsvPath);
            }
            
            Debug.Log($"Script completed successfully. Animation data saved to {outputCsvPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error running script: {ex.Message}\n{ex.StackTrace}");
            
            // Fall back to dummy data in case of exception
            Debug.LogWarning("Exception occurred. Falling back to dummy data generation.");
            return GenerateDummyCsvFile(outputCsvPath);
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
    /// Coroutine to load animation safely without try/catch around yield statements
    /// </summary>
    private IEnumerator LoadAnimationCoroutine(string audioPath, string csvPath)
    {
        // Create a state tracker for the coroutine
        bool audioLoaded = false;
        AudioClip clip = null;
        
        // Log file existence and size for debugging
        Debug.Log($"Loading animation - Audio file exists: {File.Exists(audioPath)}, Size: {(File.Exists(audioPath) ? new FileInfo(audioPath).Length : 0)} bytes");
        Debug.Log($"Loading animation - CSV file exists: {File.Exists(csvPath)}, Size: {(File.Exists(csvPath) ? new FileInfo(csvPath).Length : 0)} bytes");
        
        // First try WAV format
        string uriPath = "file://" + audioPath;
        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.WAV);
        yield return www.SendWebRequest();
        
        // If WAV format fails, try MPEG format
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Failed to load audio as WAV: {www.error}. Trying MPEG format...");
            www.Dispose();
            
            www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.MPEG);
            yield return www.SendWebRequest();
        }
        
        // Process the result after the yield is complete
        if (www.result == UnityWebRequest.Result.Success)
        {
            clip = DownloadHandlerAudioClip.GetContent(www);
            audioLoaded = (clip != null);
            Debug.Log($"Successfully loaded audio clip: {clip?.name}, Length: {clip?.length}s, Channels: {clip?.channels}");
        }
        else
        {
            Debug.LogError($"Failed to load audio clip: {www.error}");
            
            // Try other common audio formats as a last resort
            www.Dispose();
            
            AudioType[] audioTypes = new AudioType[] { 
                AudioType.OGGVORBIS, 
                AudioType.MPEG,
                AudioType.WAV 
            };
            
            foreach (AudioType audioType in audioTypes)
            {
                Debug.Log($"Trying to load audio as {audioType}...");
                www = UnityWebRequestMultimedia.GetAudioClip(uriPath, audioType);
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    clip = DownloadHandlerAudioClip.GetContent(www);
                    audioLoaded = (clip != null);
                    Debug.Log($"Successfully loaded audio clip as {audioType}: {clip?.name}, Length: {clip?.length}s");
                    break;
                }
                else
                {
                    www.Dispose();
                }
            }
        }
        
        if (www != null)
        {
            www.Dispose();
        }
        
        // Continue with loading the animation if the audio was loaded successfully
        if (audioLoaded && clip != null)
        {
            try
            {
                Debug.Log("Successfully loaded audio clip for animation");
                animationController.audioClip = clip;
                
                // Load the CSV file as a TextAsset
                TextAsset csvAsset = new TextAsset(File.ReadAllText(csvPath));
                animationController.animationCSV = csvAsset;
                
                // Set animation playback speed
                animationController.playbackSpeed = animationPlaybackSpeed * 30f; // Adjust to frames per second
                
                // Set animation scale
                animationController.animationScale = animationStrength;
                
                // Restart the animation with new data
                animationController.RestartAnimation();
                Debug.Log("Animation started with newly generated data");
                
                // Delete temporary files after loading if enabled, after animation is complete
                if (deleteTempFiles)
                {
                    StartCoroutine(DeleteTempFilesAfterDelay(audioPath, csvPath, clip.length + 1f));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading animation into controller: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Failed to load audio clip for animation");
        }
    }
    
    /// <summary>
    /// Coroutine to delete temporary files after a delay
    /// </summary>
    public IEnumerator DeleteTempFilesAfterDelay(string audioPath, string csvPath, float delay)
    {
        // Add a safety buffer to ensure playback and animation are fully complete
        float safetyBuffer = 5.0f; // 5 seconds safety buffer
        float totalDelay = delay + safetyBuffer;
        
        Debug.Log($"Will delete temporary files after {totalDelay} seconds (audio length: {delay-1f}s + safety buffer: {safetyBuffer}s)");
        
        // Wait until animation is finished playing
        yield return new WaitForSeconds(totalDelay);
        
        // Check if animation controller is still using the animation
        bool isAnimationPlaying = false;
        
        // Use the public method to check if animation is playing
        if (animationController != null)
        {
            // Safely call RestartAnimation which implies animation was stopped
            // We'll wait a bit more before deleting
            yield return new WaitForSeconds(2.0f);
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
                    Debug.Log($"Deleted temporary audio file: {audioPath}");
                }
                
                if (File.Exists(csvPath))
                {
                    File.Delete(csvPath);
                    Debug.Log($"Deleted temporary CSV file: {csvPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete temporary files: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Files still in use, skipping deletion");
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
                Debug.Log("Terminated running Python process");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to terminate Python process: {ex.Message}");
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
                    File.Delete(file);
                }
                
                Directory.Delete(tempDirectory, true);
                Debug.Log($"Cleaned up temporary directory: {tempDirectory}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to clean up temporary directory: {ex.Message}");
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
                Debug.Log("Terminated running Python process");
                currentProcess = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error terminating Python process: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("No active process to cancel");
        }
    }
}