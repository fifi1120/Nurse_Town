using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

public class EmotionController : MonoBehaviour
{
    public PlayableDirector director;
    public bool setEmotionCode = true;
    public int currentEmotionCode;
    public Animator animator;
    public List<TrackAsset> allTracks = new List<TrackAsset>();
    public List<AnimatorControllerParameter> animatorParameters = new List<AnimatorControllerParameter>();
    
   /*
        "Neutral", // 0
        "Discomfort", // 1
        "Happy", // 2
        "Pain", // 3
        "Sad", // 4
        "Anger" // 5
    */
    
    void Start()
    {
        if (director == null)
        {
            Debug.LogError("PlayableDirector not assigned.");
        }
        
        TimelineAsset timeline = director.playableAsset as TimelineAsset;
        if (timeline == null)
        {
            Debug.LogError("No TimelineAsset assigned to the PlayableDirector.");
            return;
        }
        allTracks = timeline.GetOutputTracks().ToList();
        animatorParameters = animator.parameters.ToList();
    }
    
    public void HandleEmotionCode(int emotionCode)
    {
        if (!setEmotionCode) { currentEmotionCode = emotionCode;}
        
        if (currentEmotionCode < 0 || currentEmotionCode >= allTracks.Count)
        {
            Debug.LogError("Track index out of bounds.");
            return;
        }
        
        TrackAsset selectedTrack = allTracks[currentEmotionCode];
        
        foreach (var parameter in animatorParameters)
        {
            animator.SetBool(parameter.name, parameter.name == animatorParameters[currentEmotionCode].name);
        }
        
        Debug.Log("Emotion Code: " + emotionCode);
        Debug.Log($"Selected track: {selectedTrack.name}");

        foreach (var track in allTracks)
        {
            if (track.name != "Blink Track") track.muted = (track != selectedTrack);
        }
    }

    public void PlayEmotion()
    {
        director.RebuildGraph();
        director.Play();
    }
}
