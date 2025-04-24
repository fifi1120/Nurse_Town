using UnityEngine;

namespace UI
{
    public class SceneCompletion : MonoBehaviour
    {
        void Update()
        {
        
            if (Input.GetKeyDown(KeyCode.Return))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Menu/ScenarioSelect");
            }
        }
    }
}
