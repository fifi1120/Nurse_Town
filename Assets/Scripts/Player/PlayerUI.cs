using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;

    public void UpdateText(string promptMessage)
    {
        promptText.text = promptMessage;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }


}
