using UnityEngine;
using UnityEngine.UI;

public class BloodEffectController : MonoBehaviour
{
    private Image blood;
    
    // This will show up in the Inspector for testing
    [SerializeField] 
    private bool showBlood = false;
    
    void Start()
    {
        blood = GetComponent<Image>();
        // Initialize based on the showBlood variable
        blood.enabled = showBlood;
    }

    void Update()
    {
        // Check if state changed
        blood.enabled = showBlood;
    }

    // Method to change the visibility from other scripts
    public void SetBloodVisibility(bool show)
    {
        showBlood = show;
        Debug.Log("Showing blood called: " + showBlood);
    }

    // Keep your fade effect method
    public void SetAlpha(float alpha)
    {
        Color color = blood.color;
        color.a = alpha;
        blood.color = color;
    }
}