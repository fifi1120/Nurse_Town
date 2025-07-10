using System;
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
    private int previousEmotionCode = 0;
    public Animator animator;
    public List<TrackAsset> allTracks = new();

    public string[] emotionNames = {"Neutral", "Discomfort", "Happy", "Pain", "Sad", "Anger"};

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
    }
    
    public void HandleEmotionCode(int emotionCode)
    {
        animator.ResetTrigger(emotionNames[previousEmotionCode]);

        previousEmotionCode = emotionCode;
        if (!setEmotionCode) { currentEmotionCode = emotionCode;}
        
        if (currentEmotionCode < 0 || currentEmotionCode >= allTracks.Count)
        {
            Debug.LogError("Track index out of bounds.");
            return;
        }
        
        TrackAsset selectedTrack = allTracks[currentEmotionCode];
        
        Debug.Log("Emotion Code: " + emotionCode);
        Debug.Log($"Selected track: {selectedTrack.name}");

        foreach (var track in allTracks)
        {
            if (track.name != "Blink Track") track.muted = (track != selectedTrack);
        }
        
        animator.SetTrigger(emotionNames[currentEmotionCode]);
        Debug.Log("Set trigger: " + emotionNames[currentEmotionCode]);
    }
    
   

    public void PlayEmotion()
    {
        director.RebuildGraph();
        director.Play();
    }
}
