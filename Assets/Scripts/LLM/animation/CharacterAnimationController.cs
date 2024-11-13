using UnityEngine;

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
        // UpdateAnimationState(1);
        animator.SetTrigger("pain");

    }

    public void PlayHappy()
    {
        animator.SetTrigger("happy");
    }

}