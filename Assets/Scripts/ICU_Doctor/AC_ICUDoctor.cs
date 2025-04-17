using UnityEngine;
using System.Collections;

public class AIDoctorAnimationController : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private int motionState = 0;
    
    // Priority: Gesturing (eg. wristband)
    private bool isHighPriorityAnimationPlaying = false;
    // Adjust the duration of priority animation 
    [SerializeField] private float highPriorityDuration = 1.5f; 

    void Start()
    {
        animator = GetComponent<Animator>();
        // default setting: Frustrated（
        PlayFrustrated();
    }

    // 
    public void UpdateAnimationState(int newState)
    {
        motionState = newState;
        animator.SetInteger("Motion", motionState);
    }

    // play idle: code 0
    public void PlayIdle()
    {
        isHighPriorityAnimationPlaying = false;
        UpdateAnimationState(0);
    }

    /// <summary>
    /// trigger gesturing according to the doctor's speech，eg. "wrist band" or "yes" (priority)
    /// </summary>
    /// <param name="speech">doctor's speech</param>
    public void ProcessDoctorSpeech(string speech)
    {
        string lowerSpeech = speech.ToLower();

        if (lowerSpeech.Contains("wrist band"))
        {
            StartCoroutine(PlayHighPriorityAnimation("wrist_band"));
        }
        else if (lowerSpeech.Contains("yes") || lowerSpeech.Contains("yea") || lowerSpeech.Contains("yeah"))
        {
            StartCoroutine(PlayHighPriorityAnimation("head_nod"));
        }
    }

    private IEnumerator PlayHighPriorityAnimation(string triggerName, float delay = 0.0f)
    {
        isHighPriorityAnimationPlaying = true;
        yield return new WaitForSeconds(delay);
        animator.SetTrigger(triggerName);
        // after completing the priority animation, flag it to false.
        yield return new WaitForSeconds(highPriorityDuration);
        isHighPriorityAnimationPlaying = false;
    }

    /// <summary>
    /// before playing animation, check whether there are gesturing
    /// </summary>
    private IEnumerator PlayAnimationWithDelay(string triggerName, float delay = 0.0f)
    {
        if (isHighPriorityAnimationPlaying)
            yield break;

        yield return new WaitForSeconds(delay);
        animator.SetTrigger(triggerName);
    }

    // interface for WD_EmotionAnimation

    public void PlaySad()
    {
        if (isHighPriorityAnimationPlaying)
            return;
        StartCoroutine(PlayAnimationWithDelay("sad"));
    }

    public void PlayHappy()
    {
        if (isHighPriorityAnimationPlaying)
            return;
        StartCoroutine(PlayAnimationWithDelay("happy"));
    }

    public void PlayFrustrated()
    {
        if (isHighPriorityAnimationPlaying)
            return;
        StartCoroutine(PlayAnimationWithDelay("frustrated"));
    }
}