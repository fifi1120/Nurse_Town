using UnityEngine;

namespace UI.Menu 
{
    public class PauseController : MonoBehaviour
    {
        public GameObject PauseMenuUI;
        public GameObject HelpMenu;
        public GameObject CreditsMenu;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (Time.timeScale == 1)
            {
                Time.timeScale = 0;
                // Show pause menu UI
                PauseMenuUI.SetActive(true);
            }
            else
            {
                Time.timeScale = 1;
                // Hide pause menu UI
                PauseMenuUI.SetActive(false);
            }
        }
        
        public void ToggleHelpCanvas()
        {
            Debug.Log("Toggling Help Canvas");
            if (HelpMenu.activeSelf)
            {
                PauseMenuUI.SetActive(true);
                HelpMenu.SetActive(false);
            }
            else
            {
                PauseMenuUI.SetActive(false);
                HelpMenu.SetActive(true);
            }
        }
        
        public void ToggleCreditsCanvas()
        {
            Debug.Log("Toggling Credits Canvas");
            if (CreditsMenu.activeSelf)
            {
                PauseMenuUI.SetActive(true);
                CreditsMenu.SetActive(false);
            }
            else
            {
                PauseMenuUI.SetActive(false);
                CreditsMenu.SetActive(true);
            }
        }
        
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Menu/Start");
        }
        
        public void QuitGame()
        {
            #if UNITY_EDITOR
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            #else 
		    {
			    Application.Quit();
		    }
            #endif
        }
    }
}