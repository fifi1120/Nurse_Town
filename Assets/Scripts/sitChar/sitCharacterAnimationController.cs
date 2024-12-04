using UnityEngine;
using System.Collections;

public class sitCharacterAnimationController : MonoBehaviour
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

    public void PlayBend()
    {
        StartCoroutine(PlayAnimationWithDelay("bend"));
    }

    public void PlayRubArm()
    {
        StartCoroutine(PlayAnimationWithDelay("rub_arm"));
    }

    public void PlaySad()
    {
        StartCoroutine(PlayAnimationWithDelay("sad"));
    }

    public void PlayThumbUp()
    {
        StartCoroutine(PlayAnimationWithDelay("thumb_up"));
    }

    public void PlayBloodPressure()
    {
        StartCoroutine(PlayAnimationWithDelay("BP"));
    }

    private IEnumerator PlayAnimationWithDelay(string triggerName, float delay = 0.0f)
    {
        yield return new WaitForSeconds(delay);
        animator.SetTrigger(triggerName);
    }

}