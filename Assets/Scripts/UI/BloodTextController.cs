using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BloodTextController : MonoBehaviour
{
    private TextMeshProUGUI pressF_Text;
    
    [SerializeField] 
    private bool showBlood = false;
    
    void Start()
    {

        pressF_Text = GameObject.Find("bloodText").GetComponent<TextMeshProUGUI>();
        if (pressF_Text == null)
        {
            Debug.LogError("PressF Text component not found!");
        }
        else
        {
            Debug.Log("PressF Text found successfully!");
        }

        if (pressF_Text != null)
        {
            pressF_Text.enabled = showBlood;
        }
    }

    void Update()
    {
        // Add null checks before accessing components


        if (pressF_Text != null)
        {
            pressF_Text.enabled = showBlood;
        }
    }

    public void SetBloodTextVisibility(bool show)
    {
        showBlood = show;
        Debug.Log("Showing blood called: " + showBlood);
    }

}