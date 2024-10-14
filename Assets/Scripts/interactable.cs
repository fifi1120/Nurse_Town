using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string promptMessage;
    // Start is called before the first frame update
    public void BaseInteract()
    {
        Interact();
    }
    protected virtual void Interact()
    {
        
    }
}
