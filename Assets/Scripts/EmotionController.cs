using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class EmotionController : MonoBehaviour
{
    public PlayableDirector director;    
    private int _currentEmotionCode;
    
   /*
        "Neutral", // 0
        "Discomfort", // 1
        "Happy", // 2
        "Shrug", // 3
        "Nod", // 4
        "Shake Head", // 5
        "Writhing Pain", // 6
        "Sad", // 7
        "Stretch Arms", // 8
        "Stretch Neck", // 9
        "Anger" // 10
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
        _currentEmotionCode = emotionCode;

        if (timeline == null)
        {
            Debug.LogError("No TimelineAsset assigned to the PlayableDirector.");
            return;
        }
        
        var allTracks = timeline.GetOutputTracks().ToList();
        
        /*Debug.Log("All available tracks:");
        foreach (var track in allTracks)
        {
            Debug.Log(" Track: " + track.name);
        }
        */
        
        if (_currentEmotionCode < 0 || _currentEmotionCode >= allTracks.Count)
        {
            Debug.LogError("Track index out of bounds.");
            return;
        }
        
        TrackAsset selectedTrack = allTracks[_currentEmotionCode];

        Debug.Log($"Selected track: {selectedTrack.name}");

        foreach (var track in allTracks)
        {
            track.muted = (track != selectedTrack);
        }
        
        director.RebuildGraph();
        director.Play();
    }
}
