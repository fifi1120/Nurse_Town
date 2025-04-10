using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this component to any character that needs a specific voice profile
/// </summary>
public class CharacterVoiceComponent : MonoBehaviour
{
    [Tooltip("The voice profile asset for this character")]
    public CharacterVoiceProfile voiceProfile;
    
    [Tooltip("Reference to the TTSManager (optional, will find it if not set)")]
    public TTSManager ttsManager;
    
    [Tooltip("Reference to this character's facial animation controller (optional)")]
    public CSVFacialAnimationController facialAnimationController;
    
    // Track if this character is currently speaking
    private bool isSpeaking = false;
    
    void Start()
    {
        // Find TTSManager if not assigned
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<TTSManager>();
            if (ttsManager == null)
            {
                Debug.LogError("No TTSManager found in the scene!");
                return;
            }
        }
        
        // Find facial animation controller if not assigned
        if (facialAnimationController == null)
        {
            facialAnimationController = GetComponentInChildren<CSVFacialAnimationController>();
        }
        
        // Validate voice profile
        if (voiceProfile == null)
        {
            Debug.LogError($"No voice profile assigned to character: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Make this character speak the provided text
    /// </summary>
    /// <param name="text">Text to speak</param>
    /// <param name="emotionCode">Optional emotion code (0-10)</param>
    public void Speak(string text, int emotionCode = 0)
    {
        if (voiceProfile == null || ttsManager == null) return;
        
        // Format the text with emotion code if provided
        string formattedText = text;
        if (emotionCode >= 0 && emotionCode <= 10)
        {
            formattedText += $" [{emotionCode}]";
        }
        
        // Apply this character's voice settings
        ApplyVoiceProfile();
        
        // Trigger the speech
        ttsManager.ConvertTextToSpeech(formattedText);
        
        // Set speaking flag
        isSpeaking = true;
        
        // Start a coroutine to detect when speech ends
        StartCoroutine(WaitForSpeechToEnd());
    }
    
    /// <summary>
    /// Apply this character's voice profile to the TTS system
    /// </summary>
    private void ApplyVoiceProfile()
    {
        if (voiceProfile == null || ttsManager == null) return;
        
        // Apply voice settings to TTSManager
        ttsManager.voiceId = voiceProfile.voiceId;
        ttsManager.modelId = voiceProfile.modelId;
        ttsManager.stability = voiceProfile.stability;
        ttsManager.similarityBoost = voiceProfile.similarityBoost;
        ttsManager.styleExaggeration = voiceProfile.styleExaggeration;
        
        // Apply animation settings if we have a facial animation controller
        if (facialAnimationController != null)
        {
            facialAnimationController.animationScale = voiceProfile.animationScale;
            // You could also set other animation parameters here
        }
    }
    
    /// <summary>
    /// Monitor the audio source to detect when speech has ended
    /// </summary>
    private IEnumerator WaitForSpeechToEnd()
    {
        // Wait until the audio starts playing
        yield return new WaitForSeconds(0.5f);
        
        AudioSource audioSource = ttsManager.audioSource;
        if (audioSource == null)
        {
            isSpeaking = false;
            yield break;
        }
        
        // Wait until the audio stops playing
        while (audioSource.isPlaying)
        {
            yield return null;
        }
        
        // Speech has ended
        isSpeaking = false;
        
        // Notify listeners that speech has ended
        OnSpeechEnded();
    }
    
    /// <summary>
    /// Check if this character is currently speaking
    /// </summary>
    public bool IsSpeaking()
    {
        return isSpeaking;
    }
    
    /// <summary>
    /// Event called when speech has ended
    /// </summary>
    protected virtual void OnSpeechEnded()
    {
        // Can be overridden in subclasses to trigger actions when speech ends
    }
}