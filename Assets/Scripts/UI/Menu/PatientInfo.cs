using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatientInfo : MonoBehaviour
{
    public GameObject patientInfoUI;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInfo();
        }
    }
    
    public void ToggleInfo()
    {
        if (patientInfoUI.activeSelf)
        {
            patientInfoUI.SetActive(false);
        }
        else
        {
            patientInfoUI.SetActive(true);
        }
    }
}
