using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.Timeline;
using Xceed.Document.NET;

public class EmotionController : MonoBehaviour
{
    public PlayableDirector director;
    public bool setEmotionCode = true;
    public int currentEmotionCode;
    private int previousEmotionCode = 0;
    public Animator animator;
    public List<TrackAsset> allTracks = new();

    public string[] emotionNames = {"Neutral", "Discomfort", "Happy", "Pain", "Sad", "Anger"};
    
    private List<TTSManager.WordTiming> charTimings;
    private List<TTSManager.WordTiming> wordTimings;

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
    
    public void SyncAnimationsWithWordTimings(List<TTSManager.WordTiming> timings)
    {
        charTimings = timings;
        wordTimings = BuildWordsFromCharTimings(charTimings);
        StartCoroutine(TriggerAnimationsWithTiming());
    }

    private List<TTSManager.WordTiming> BuildWordsFromCharTimings(List<TTSManager.WordTiming> timings)
    {
        List<TTSManager.WordTiming> words = new List<TTSManager.WordTiming>();
        if (timings == null || timings.Count == 0)
        {
            Debug.LogWarning("No character timings provided.");
            return words;
        }

        TTSManager.WordTiming currentWord = null;
        foreach (var timing in timings)
        {
            if (timing.Word == "... ")
            {
                continue;
            }

            if (currentWord == null || timing.Word.EndsWith(" "))
            {
                currentWord = new TTSManager.WordTiming
                {
                    Word = timing.Word.Trim(),
                    StartTime = timing.StartTime,
                    EndTime = timing.EndTime
                };
                words.Add(currentWord);
            }
            else
            {
                currentWord.Word += timing.Word;
                currentWord.EndTime = timing.EndTime;
            }
        }

        Debug.Log($"Built {words.Count} words from character timings.");
        return words;
    }


        private IEnumerator TriggerAnimationsWithTiming()
    {
        if (wordTimings == null || wordTimings.Count == 0)
        {
            yield break;
        }

        foreach (var wordTiming in wordTimings)
        {
            float delay = wordTiming.EndTime - wordTiming.StartTime;
            Debug.Log($"Triggering animation for word: {wordTiming.Word} after delay: {delay}");
            yield return new WaitForSeconds(delay);
            
            int emotionCode = MapWordToEmotion(wordTiming.Word);
            if (emotionCode != 0)
            {
                TrackAsset selectedTrack = allTracks[emotionCode];
                foreach (var track in allTracks.Where(track => track.name != "Blink Track"))
                {
                    track.muted = (track != selectedTrack);
                }

                PlayEmotion();

                var animationDelay = selectedTrack.duration;
                yield return new WaitForSeconds((float)animationDelay);
                
                selectedTrack = allTracks[currentEmotionCode];
                foreach (var track in allTracks.Where(track => track.name != "Blink Track"))
                {
                    track.muted = (track != selectedTrack);
                }
                PlayEmotion();
            }
            
            //float delay = wordTiming.EndTime - wordTiming.StartTime;
            //Debug.Log($"Triggering animation for word: {wordTiming.Word} after delay: {delay}");
            //yield return new WaitForSeconds(delay);
        }
    }


    private int MapWordToEmotion(string word)
    {
        if (word.ToLower().Contains("thanks")) return 2;
        if (word.ToLower().Contains("thank")) return 2;
        return 0; 
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

        foreach (var track in allTracks.Where(track => track.name != "Blink Track"))
        {
            track.muted = (track != selectedTrack);
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
