using UnityEngine;
using TMPro;

public class BloodTextController : MonoBehaviour
{
    private TextMeshProUGUI pressF_Text;
    [SerializeField] private bool showBlood = false;
    
    void Start()
    {
        pressF_Text = GameObject.Find("bloodText").GetComponent<TextMeshProUGUI>();
        if (pressF_Text == null)
        {
            Debug.LogError("PressF Text component not found!");
        }
        else
        {
            pressF_Text.text = "Press F to measure blood pressure";
            pressF_Text.enabled = showBlood;
        }
    }

    void Update()
    {
        if (pressF_Text != null)
        {
            pressF_Text.enabled = showBlood;
        }
    }

    public void SetBloodTextVisibility(bool show)
    {
        showBlood = show;
        Debug.Log("Showing blood text called: " + showBlood);
    }
}