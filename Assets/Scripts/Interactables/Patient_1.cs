using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class patient_1 : Interactable
{
    [SerializeField] private GameObject patient;
    private bool _patient_bool;
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
        _patient_bool = !_patient_bool;
        Debug.Log("Interacting with Patient");
    }
}
