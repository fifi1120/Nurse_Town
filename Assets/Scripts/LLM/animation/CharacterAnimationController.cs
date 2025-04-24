using UnityEngine;
using System.Collections;

public class CharacterAnimationController : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private int motionState = 0;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        UpdateAnimationState(motionState);
    }

    public void UpdateAnimationState(int newState)
    {
        motionState = newState;
        animator.SetInteger("Motion", motionState);
    }

    public void PlayIdle()
    {
        UpdateAnimationState(0);
    }

    public void PlayHeadPain()
    {
        StartCoroutine(PlayAnimationWithDelay("pain"));
    }

    public void PlayHappy()
    {
        StartCoroutine(PlayAnimationWithDelay("happy"));
    }

    public void PlayShrug()
    {
        StartCoroutine(PlayAnimationWithDelay("shrug"));
    }

    public void PlayHeadNod()
    {
        StartCoroutine(PlayAnimationWithDelay("head_nod"));
    }

    public void PlayHeadShake()
    {
        StartCoroutine(PlayAnimationWithDelay("head_shake"));
    }

    public void PlayWrithingInPain()
    {
        StartCoroutine(PlayAnimationWithDelay("writhing_pain"));
    }

    public void PlaySad()
    {
        StartCoroutine(PlayAnimationWithDelay("sad"));
    }

    public void PlayArmStretch()
    {
        StartCoroutine(PlayAnimationWithDelay("arm_stretch"));
    }

    public void PlayNeckStretch()
    {
        StartCoroutine(PlayAnimationWithDelay("neck_stretch"));
    }

    public void PlayBloodPressure()
    {
        StartCoroutine(PlayAnimationWithDelay("blood_pre"));
    }

    public void PlaySittingTalking()
    {
        StartCoroutine(PlayAnimationWithDelay("sitting_talking"));
    }

    private IEnumerator PlayAnimationWithDelay(string triggerName, float delay = 0.0f)
    {
        yield return new WaitForSeconds(delay);
        animator.SetTrigger(triggerName);
    }

}