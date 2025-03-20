using UnityEngine;
using UnityEngine.UI;
using UI.Menu;
using Unity.VisualScripting;

namespace UI.Menu
{ 
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject credits;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        private Canvas _mainMenuCanvas;
        private Canvas _startGameCanvas;
        private Canvas _creditsCanvas;
        private Canvas _quitCanvas;
        private Canvas _loadingScreenCanvas;
    
        void Start()
        {
            _mainMenuCanvas = mainMenu.GetComponent<Canvas>();
            _creditsCanvas = credits.GetComponent<Canvas>();
            _loadingScreenCanvas = loadingScreen.GetComponent<Canvas>();
            _creditsCanvas.enabled = false; 
            _loadingScreenCanvas.enabled = false;
            Menu.ButtonAction(startGameButton, NewGame);
            Menu.ButtonAction(creditsButton, Credits);
            Menu.ButtonAction(quitButton, Quit);
        }
    
        private void NewGame()
        {
            Menu.CanvasTransition(_mainMenuCanvas, _startGameCanvas);
        }
        

        private void Credits()
        {
            Menu.CanvasTransition(_mainMenuCanvas, _creditsCanvas);
        }

        private void Quit()
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