using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    public class StartGame
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject startGame;
        [SerializeField] private Button backButton;

        private Canvas _mainMenuCanvas;
        private Canvas _creditsCanvas;
        private Canvas _backCanvas;
        
        void Start()
        {
            _mainMenuCanvas = mainMenu.GetComponent<Canvas>();
            _creditsCanvas = startGame.GetComponent<Canvas>();
             // Ensure the startGame canvas is initially disabled
        }
        
        public void Back()
        {
            Menu.CanvasTransition(_creditsCanvas, _mainMenuCanvas);
        }
        
    }
}