using System;
using UnityEngine;

/// <summary>
/// Defines a voice profile for a character using ElevenLabs TTS
/// </summary>
[CreateAssetMenu(fileName = "New Character Voice Profile", menuName = "Audio/Character Voice Profile")]
public class CharacterVoiceProfile : ScriptableObject
{
    [Header("Character Information")]
    [Tooltip("Name of the character (for reference only)")]
    public string characterName = "Character";
    
    [Tooltip("Description of this voice (for reference only)")]
    [TextArea(2, 5)]
    public string voiceDescription = "";
    
    [Header("ElevenLabs Voice Settings")]
    [Tooltip("Voice ID from ElevenLabs")]
    public string voiceId = "Bz0vsNJm8uY1hbd4c4AE"; // Default voice ID
    
    [Tooltip("Model ID from ElevenLabs")]
    public string modelId = "eleven_multilingual_v2"; // Default model
    
    [Header("Voice Characteristics")]
    [Range(0f, 1f)]
    [Tooltip("Stability value (0-1). Lower values make voice more spontaneous, higher values more stable.")]
    public float stability = 0.4f;
    
    [Range(0f, 1f)]
    [Tooltip("Similarity boost value (0-1). Higher values make voice sound more like the original voice.")]
    public float similarityBoost = 0.75f;
    
    [Range(0f, 1f)]
    [Tooltip("Style exaggeration value (0-1). Higher values amplify the speaking style.")]
    public float styleExaggeration = 0.3f;
    
    [Header("Advanced Settings")]
    [Range(0.5f, 2.0f)]
    [Tooltip("Playback speed multiplier. 1.0 is normal speed.")]
    public float playbackSpeed = 1.0f;
    
    [Range(0.5f, 2.0f)]
    [Tooltip("Animation scaling for facial movements. Higher values make expressions more pronounced.")]
    public float animationScale = 1f;
}