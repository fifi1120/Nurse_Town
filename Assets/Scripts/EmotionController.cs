using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

public class EmotionController : MonoBehaviour
{
    public PlayableDirector director;
    public bool setEmotionCode = true;
    public int currentEmotionCode;
    
   /*
        "Neutral", // 0
        "Discomfort", // 1
        "Happy", // 2
        "Writhing Pain", // 3
        "Sad", // 4
        "Anger" // 5
    */
    
    void Start()
    {
        if (director == null)
        {
            Debug.LogError("PlayableDirector not assigned.");
        }
    }
    
    public void HandleEmotionCode(int emotionCode)
    {
        TimelineAsset timeline = director.playableAsset as TimelineAsset;
        
        if (!setEmotionCode) { currentEmotionCode = emotionCode;}

            if (timeline == null)
        {
            Debug.LogError("No TimelineAsset assigned to the PlayableDirector.");
            return;
        }
        
        var allTracks = timeline.GetOutputTracks().ToList();

        if (currentEmotionCode < 0 || currentEmotionCode >= allTracks.Count)
        {
            Debug.LogError("Track index out of bounds.");
            return;
        }
        
        TrackAsset selectedTrack = allTracks[currentEmotionCode];

        Debug.Log($"Selected track: {selectedTrack.name}");

        foreach (var track in allTracks)
        {
            track.muted = (track != selectedTrack);
        }
    }

    public void PlayEmotion()
    {
        director.RebuildGraph();
        director.Play();
    }
}
