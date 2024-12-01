using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BloodEffectController : MonoBehaviour
{
    private Image blood;
    [SerializeField] private bool showBlood = false;
    private TextMeshProUGUI bloodPressureText;
    private BloodTextController bloodTextController;
    private bool canMeasureBloodPressure = false;
    private Animator animator;
    
    void Start()
    {
        blood = GetComponent<Image>();
        blood.enabled = showBlood;
        bloodTextController = FindObjectOfType<BloodTextController>();
        animator = FindObjectOfType<Animator>();
        
        // Find blood pressure text by name
        bloodPressureText = GameObject.Find("bloodPresureVal").GetComponent<TextMeshProUGUI>();
        if (bloodPressureText == null)
        {
            Debug.LogError("Blood Pressure Text component not found!");
        }
        else
        {
            bloodPressureText.text = "Patient blood pressure: Unknown";
        }
    }

    void Update()
    {
        blood.enabled = showBlood;
        
        if (canMeasureBloodPressure && Input.GetKeyDown(KeyCode.F))
        {
            MeasureBloodPressure();
        }
    }

    public void SetBloodVisibility(bool show)
    {
        showBlood = show;
        canMeasureBloodPressure = show;
        Debug.Log("Showing blood called: " + showBlood);
    }

    private void MeasureBloodPressure()
    {
        // Hide blood effect and text
        showBlood = false;
        bloodTextController.SetBloodTextVisibility(false);
        
        // Update blood pressure text
        if (bloodPressureText != null)
        {
            bloodPressureText.text = "Patient blood pressure: 150 mmHg";
            Debug.Log("Blood pressure text updated");
        }
        else
        {
            Debug.LogError("Blood Pressure Text is null when trying to measure!");
        }
        
        // Trigger the animation
        if (animator != null)
        {
            animator.SetTrigger("after_blood_mea");
            Debug.Log("Animation trigger set: after_blood_mea");
        }
        else
        {
            Debug.LogError("Animator not found!");
        }
        
        // Disable measurement ability
        canMeasureBloodPressure = false;
    }

    public void SetAlpha(float alpha)
    {
        Color color = blood.color;
        color.a = alpha;
        blood.color = color;
    }
}