using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of character voice profiles for easy switching between different voice settings
/// </summary>
public class VoiceProfileManager : MonoBehaviour
{
    public static VoiceProfileManager Instance { get; private set; }
    
    [Header("Voice Profiles")]
    [Tooltip("Array of character voice profiles to choose from")]
    public CharacterVoiceProfile[] availableProfiles;
    
    [Tooltip("Default profile to use if none is specified")]
    public CharacterVoiceProfile defaultProfile;
    
    [Header("Component References")]
    [Tooltip("Reference to TTSManager")]
    public TTSManager ttsManager;
    
    [Tooltip("Reference to CSVFacialAnimationController")]
    public CSVFacialAnimationController facialAnimationController;
    
    // Dictionary for quick lookup of profiles by name
    private Dictionary<string, CharacterVoiceProfile> profilesByName;
    
    // Currently active profile
    private CharacterVoiceProfile currentProfile;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize profiles dictionary
        profilesByName = new Dictionary<string, CharacterVoiceProfile>();
        foreach (var profile in availableProfiles)
        {
            if (profile != null)
            {
                profilesByName[profile.characterName] = profile;
            }
        }
        
        // Set default profile
        if (defaultProfile != null)
        {
            currentProfile = defaultProfile;
        }
        else if (availableProfiles != null && availableProfiles.Length > 0)
        {
            currentProfile = availableProfiles[0];
        }
        
        // Apply initial profile settings
        if (currentProfile != null)
        {
            ApplyProfileSettings(currentProfile);
        }
    }
    
    void Start()
    {
        // Find TTSManager if not assigned
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<TTSManager>();
            if (ttsManager == null)
            {
                Debug.LogError("No TTSManager found in the scene!");
            }
        }
        
        // Find facial animation controller if not assigned
        if (facialAnimationController == null)
        {
            facialAnimationController = FindObjectOfType<CSVFacialAnimationController>();
        }
    }
    
    /// <summary>
    /// Set the active voice profile by name
    /// </summary>
    public bool SetActiveProfile(string profileName)
    {
        if (profilesByName.TryGetValue(profileName, out CharacterVoiceProfile profile))
        {
            currentProfile = profile;
            ApplyProfileSettings(profile);
            Debug.Log($"Switched to voice profile: {profileName}");
            return true;
        }
        
        Debug.LogWarning($"Voice profile '{profileName}' not found!");
        return false;
    }
    
    /// <summary>
    /// Set the active voice profile by index
    /// </summary>
    public bool SetActiveProfile(int profileIndex)
    {
        if (availableProfiles != null && profileIndex >= 0 && profileIndex < availableProfiles.Length)
        {
            currentProfile = availableProfiles[profileIndex];
            ApplyProfileSettings(currentProfile);
            Debug.Log($"Switched to voice profile: {currentProfile.characterName}");
            return true;
        }
        
        Debug.LogWarning($"Voice profile index {profileIndex} is out of range!");
        return false;
    }
    
    /// <summary>
    /// Set the active voice profile directly
    /// </summary>
    public void SetActiveProfile(CharacterVoiceProfile profile)
    {
        if (profile != null)
        {
            currentProfile = profile;
            ApplyProfileSettings(profile);
            Debug.Log($"Switched to voice profile: {profile.characterName}");
        }
        else
        {
            Debug.LogWarning("Attempted to set null voice profile!");
        }
    }
    
    /// Get the current active profile
    public CharacterVoiceProfile GetCurrentProfile()
    {
        return currentProfile;
    }
    
    /// Apply settings from a voice profile to the relevant components
    private void ApplyProfileSettings(CharacterVoiceProfile profile)
    {
        if (profile == null) return;
        
        // Apply settings to TTSManager
        if (ttsManager != null)
        {
            ttsManager.voiceId = profile.voiceId;
            ttsManager.modelId = profile.modelId;
            ttsManager.stability = profile.stability;
            ttsManager.similarityBoost = profile.similarityBoost;
            ttsManager.styleExaggeration = profile.styleExaggeration;
        }
        
        // Apply settings to facial animation controller
        if (facialAnimationController != null)
        {
            facialAnimationController.animationScale = profile.animationScale;
        }
    }
    
    /// Get a list of all available profile names
    public string[] GetProfileNames()
    {
        if (availableProfiles == null || availableProfiles.Length == 0)
        {
            return new string[0];
        }
        
        string[] names = new string[availableProfiles.Length];
        for (int i = 0; i < availableProfiles.Length; i++)
        {
            names[i] = availableProfiles[i] != null ? availableProfiles[i].characterName : "Unknown";
        }
        
        return names;
    }
}