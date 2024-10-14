using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Interactable
{
    [SerializeField] private GameObject door;
    private bool doorOpen;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    protected override void Interact()
    {
        doorOpen = !doorOpen;
        Debug.Log("Interacting with Door");
        door.GetComponent<Animator>().SetBool("IsOpen", doorOpen);
        
    }
}
