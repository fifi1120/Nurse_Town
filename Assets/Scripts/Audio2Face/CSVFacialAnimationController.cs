using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This script controls the facial animation using CSV data with timecode support
public class CSVFacialAnimationController : MonoBehaviour
{
    [Header("Animation Setup")]
    [Tooltip("Your model with blendshapes")]
    public SkinnedMeshRenderer characterFace;
    
    [Tooltip("CSV file with animation data")]
    public TextAsset animationCSV;
    
    [Tooltip("Audio file to sync with")]
    public AudioClip audioClip;
    
    [Range(0.5f, 2.0f)]
    [Tooltip("Multiply animation values by this amount")]
    public float animationScale = 1.5f;
    
    [Header("Sync Settings")]
    [Tooltip("Name of the column containing time information")]
    public string timeColumnName = "timeCode";
    
    [Range(1f, 100f)]
    [Tooltip("Playback speed multiplier (use 30 for typical 30fps animation)")]
    public float playbackSpeed = 30f;
    
    [Tooltip("Offset to apply to all time values (in seconds)")]
    public float timeOffset = 0.0f;
    
    [Header("Debug Settings")]
    [Tooltip("Show debug logs for first few frames")]
    public bool showDebugLogs = true;
    
    // Add a public property to check if animation is playing
    public bool IsPlaying => isPlaying;
    
    // Private variables
    private Dictionary<string, int> blendShapeMapping;
    private List<KeyValuePair<float, Dictionary<string, float>>> timeOrderedFrames;
    private AudioSource audioSource;
    private bool isPlaying = false;
    private float animationDuration = 0f;
    private bool isInitialized = false;

    void Start()
    {
        // Create audio source if needed
        if (audioClip != null && GetComponent<AudioSource>() == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.playOnAwake = false;
        }
        else
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // Initialize the animation system if we have the required components
        if (characterFace != null && animationCSV != null)
        {
            InitializeAnimation();
            StartCoroutine(PlayAnimation());
        }
        else
        {
            Debug.LogWarning("CSVFacialAnimationController: Missing required components (Character Face or Animation CSV). Animation will not play.");
        }
    }
    
    private void InitializeAnimation()
    {
        if (characterFace == null)
        {
            Debug.LogError("No character face mesh assigned!");
            return;
        }
        
        if (animationCSV == null)
        {
            Debug.LogError("No animation CSV file assigned!");
            return;
        }
        
        // Create blendshape mapping
        CreateBlendShapeMapping();
        
        // Initialize frames list
        timeOrderedFrames = new List<KeyValuePair<float, Dictionary<string, float>>>();
        
        // Load animation data from CSV
        LoadAnimationFromCSV();
        
        // Sort frames by time
        if (timeOrderedFrames.Count > 0)
        {
            timeOrderedFrames.Sort((a, b) => a.Key.CompareTo(b.Key));
            
            // Calculate animation duration from the last time point
            animationDuration = timeOrderedFrames[timeOrderedFrames.Count - 1].Key;
            Debug.Log($"Animation duration based on time points: {animationDuration} seconds");
            Debug.Log($"With playback speed of {playbackSpeed}x, animation will play in {animationDuration / playbackSpeed} seconds");
            
            Debug.Log($"Loaded {timeOrderedFrames.Count} frames of animation data");
            Debug.Log($"Found {blendShapeMapping.Count} blendshape mappings");
            
            if (audioClip != null)
            {
                Debug.Log($"Audio duration: {audioClip.length} seconds");
                float expectedAnimationPlaytime = animationDuration / playbackSpeed;
                if (Math.Abs(audioClip.length - expectedAnimationPlaytime) > 1.0f)
                {
                    Debug.LogWarning($"Audio length ({audioClip.length}s) and expected animation playback time ({expectedAnimationPlaytime}s) differ significantly!");
                    Debug.LogWarning($"You may need to adjust the playbackSpeed parameter (currently {playbackSpeed}).");
                    
                    // Suggest a value
                    float suggestedSpeed = animationDuration / audioClip.length;
                    Debug.LogWarning($"Suggested playbackSpeed value: {suggestedSpeed}");
                }
            }
            
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("No animation frames loaded from CSV!");
        }
    }
    
    private void CreateBlendShapeMapping()
    {
        blendShapeMapping = new Dictionary<string, int>();
        
        if (characterFace == null || characterFace.sharedMesh == null)
        {
            Debug.LogError("Cannot create blendshape mapping: Character face or shared mesh is missing!");
            return;
        }
        
        // Get all available blendshapes in the mesh
        int blendShapeCount = characterFace.sharedMesh.blendShapeCount;
        
        Debug.Log($"Character has {blendShapeCount} blend shapes");
        
        // Create mapping between CSV column names and mesh blendshape indices
        for (int i = 0; i < blendShapeCount; i++)
        {
            string blendShapeName = characterFace.sharedMesh.GetBlendShapeName(i);
            
            // Try different naming conventions for mapping
            string csvNameWithPrefix = "blendShapes." + char.ToUpper(blendShapeName[0]) + blendShapeName.Substring(1);
            string csvNameExact = "blendShapes." + blendShapeName;
            string csvNameLower = "blendShapes." + blendShapeName.ToLower();
            
            blendShapeMapping[csvNameWithPrefix] = i;
            blendShapeMapping[csvNameExact] = i;
            blendShapeMapping[csvNameLower] = i;
            
            if (showDebugLogs && i < 5)
            {
                Debug.Log($"Mapped blendshape {i}: {blendShapeName} to CSV names including {csvNameWithPrefix}");
            }
        }
    }
    
    private void LoadAnimationFromCSV()
    {
        if (animationCSV == null)
        {
            Debug.LogError("Cannot load animation: Animation CSV is missing!");
            return;
        }
        
        // Parse CSV data
        string[] lines = animationCSV.text.Split('\n');
        
        if (lines.Length < 2)
        {
            Debug.LogError("CSV file has insufficient data!");
            return;
        }
        
        // Get header line and parse column names
        string[] headers = lines[0].Split(',');
        
        // Find the index of the time column
        int timeColumnIndex = -1;
        for (int i = 0; i < headers.Length; i++)
        {
            string headerName = headers[i].Trim();
            if (headerName == timeColumnName || i == 0) // Try to use first column as fallback
            {
                timeColumnIndex = i;
                if (headerName == timeColumnName)
                {
                    Debug.Log($"Found time column: {headerName} at index {i}");
                }
                else
                {
                    Debug.Log($"Using first column as time column: {headerName}");
                    timeColumnName = headerName;
                }
                break;
            }
        }
        
        if (timeColumnIndex == -1)
        {
            Debug.LogError($"Could not find time column named '{timeColumnName}' or use first column as fallback!");
            return;
        }
        
        // Print some header names for debugging
        if (showDebugLogs)
        {
            Debug.Log("CSV Header Analysis:");
            for (int i = 0; i < Math.Min(10, headers.Length); i++)
            {
                Debug.Log($"Column {i}: '{headers[i].Trim()}'");
            }
        }
        
        // Skip first row (headers) and load all frames
        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] values = line.Split(',');
            if (values.Length <= timeColumnIndex)
            {
                Debug.LogWarning($"Skipping line {lineIndex}: Not enough values to read time column");
                continue;
            }
            
            // Try to parse the time value
            float timeValue;
            if (!float.TryParse(values[timeColumnIndex], out timeValue))
            {
                Debug.LogWarning($"Skipping line {lineIndex}: Could not parse time value '{values[timeColumnIndex]}'");
                continue;
            }
            
            // Apply time offset
            timeValue = timeValue + timeOffset;
            
            Dictionary<string, float> frameData = new Dictionary<string, float>();
            
            // Parse each value and add to frame data if it's a blendshape column
            for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
            {
                // Skip the time column
                if (i == timeColumnIndex) continue;
                
                string header = headers[i].Trim();
                
                // Skip non-blendshape columns and empty values
                if (!header.StartsWith("blendShapes.") || string.IsNullOrEmpty(values[i]))
                {
                    continue;
                }
                
                // Try to parse the value
                float value;
                if (float.TryParse(values[i], out value) && value > 0)
                {
                    frameData[header] = value;
                }
            }
            
            // Store the frame data with its time value
            timeOrderedFrames.Add(new KeyValuePair<float, Dictionary<string, float>>(timeValue, frameData));
            
            // Log first few frames for debugging
            if (showDebugLogs && lineIndex <= 5)
            {
                Debug.Log($"Frame at time {timeValue}: {frameData.Count} blendshape values");
            }
        }
        
        // Debug log for first and last frames
        if (showDebugLogs && timeOrderedFrames.Count > 0)
        {
            var firstFrame = timeOrderedFrames[0];
            var lastFrame = timeOrderedFrames[timeOrderedFrames.Count - 1];
            
            Debug.Log($"First frame at time {firstFrame.Key}, last frame at time {lastFrame.Key}");
            Debug.Log($"Total animation duration: {lastFrame.Key - firstFrame.Key} seconds");
            
            // Print time differences between first few frames to verify frame rate
            if (timeOrderedFrames.Count >= 5)
            {
                Debug.Log("Frame time analysis (first 5 frames):");
                for (int i = 1; i < 5; i++)
                {
                    float timeDiff = timeOrderedFrames[i].Key - timeOrderedFrames[i-1].Key;
                    Debug.Log($"Time between frame {i-1} and {i}: {timeDiff} seconds (approx {1.0f/timeDiff} fps)");
                }
            }
        }
    }
    
    IEnumerator PlayAnimation()
    {
        if (!isInitialized || timeOrderedFrames == null || timeOrderedFrames.Count == 0)
        {
            Debug.LogWarning("Cannot play animation: Animation data not initialized or empty!");
            yield break;
        }
        
        // Reset all blendshapes first
        ResetBlendShapes();
        
        // Start audio playback
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
        isPlaying = true;
        float startTime = Time.time;
        float animationPlaybackDuration = animationDuration / playbackSpeed;
        
        Debug.Log($"Starting animation playback. Expected duration: {animationPlaybackDuration} seconds");
        
        // Main animation loop
        while (isPlaying)
        {
            // Calculate elapsed time since animation started
            float elapsedTime = Time.time - startTime;
            
            // Stop if we've reached the end of the animation
            if (elapsedTime > animationPlaybackDuration)
            {
                break;
            }
            
            // Convert actual time to animation time (scaled by playback speed)
            float animationTime = elapsedTime * playbackSpeed;
            
            // Find the appropriate frame to display based on the current time
            ApplyFrameAtTime(animationTime);
            
            // Wait until next frame
            yield return null;
        }
        
        // Make sure we apply the last frame
        if (timeOrderedFrames.Count > 0)
        {
            ApplyFrameData(timeOrderedFrames[timeOrderedFrames.Count - 1].Value);
        }
        
        Debug.Log("Animation playback completed");
        isPlaying = false;
    }
    
    private void ApplyFrameAtTime(float time)
    {
        if (timeOrderedFrames == null || timeOrderedFrames.Count == 0)
        {
            return;
        }
        
        // Find the closest frame that's less than or equal to the current time
        int index = timeOrderedFrames.FindIndex(frame => frame.Key > time) - 1;
        
        // If time is before first frame, use first frame
        if (index < 0) index = 0;
        
        // If time is after last frame, use last frame
        if (index >= timeOrderedFrames.Count) index = timeOrderedFrames.Count - 1;
        
        // Apply the frame
        if (index >= 0 && index < timeOrderedFrames.Count)
        {
            ApplyFrameData(timeOrderedFrames[index].Value);
        }
    }
    
    private void ApplyFrameData(Dictionary<string, float> frameData)
    {
        if (characterFace == null || frameData == null || blendShapeMapping == null)
        {
            return;
        }
        
        int shapesApplied = 0;
        
        // Apply each blendshape value from the frame data
        foreach (var entry in frameData)
        {
            string csvName = entry.Key;
            float value = entry.Value * animationScale; // Apply scaling
            
            // Find corresponding blendshape index
            if (blendShapeMapping.TryGetValue(csvName, out int blendShapeIndex))
            {
                // Apply the value to the blendshape
                characterFace.SetBlendShapeWeight(blendShapeIndex, value * 100f); // Unity uses 0-100 range
                shapesApplied++;
            }
        }
    }
    
    private void ResetBlendShapes()
    {
        if (characterFace == null || characterFace.sharedMesh == null)
        {
            return;
        }
        
        // Reset all blendshapes to zero
        for (int i = 0; i < characterFace.sharedMesh.blendShapeCount; i++)
        {
            characterFace.SetBlendShapeWeight(i, 0);
        }
    }
    
    // Restart the animation (useful for UI button)
    public void RestartAnimation()
    {
        StopAllCoroutines();
        
        if (characterFace != null && animationCSV != null)
        {
            // Re-initialize if needed
            if (!isInitialized)
            {
                InitializeAnimation();
            }
            
            if (isInitialized)
            {
                StartCoroutine(PlayAnimation());
            }
            else
            {
                Debug.LogWarning("Cannot restart animation: Animation data not initialized!");
            }
        }
        else
        {
            Debug.LogWarning("Cannot restart animation: Missing required components!");
        }
    }
    
    // Cleanup when script is disabled
    private void OnDisable()
    {
        StopAllCoroutines();
        isPlaying = false;
        
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    // For debug visualization
    void OnGUI()
    {
        // Add proper null checks to prevent NullReferenceExceptions
        if (showDebugLogs && isPlaying && isInitialized && timeOrderedFrames != null && timeOrderedFrames.Count > 0)
        {
            float elapsedTime = Time.time - Time.timeSinceLevelLoad + Time.deltaTime;
            GUI.Label(new Rect(10, 10, 300, 20), $"Animation Time: {elapsedTime:F2}s");
            
            float animTime = elapsedTime * playbackSpeed;
            GUI.Label(new Rect(10, 30, 300, 20), $"Actual Animation Position: {animTime:F2}s / {animationDuration:F2}s");
            
            int frameIndex = timeOrderedFrames.FindIndex(frame => frame.Key > animTime) - 1;
            if (frameIndex < 0) frameIndex = 0;
            if (frameIndex >= timeOrderedFrames.Count) frameIndex = timeOrderedFrames.Count - 1;
            
            GUI.Label(new Rect(10, 50, 300, 20), $"Current Frame: {frameIndex} / {timeOrderedFrames.Count}");
        }
    }
    
    // Add a public method to check if animation is still playing
    public bool IsAnimationPlaying()
    {
        return isPlaying;
    }
}