using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    public class Credits : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject credits;
        [SerializeField] private Button backButton;

        private Canvas _mainMenuCanvas;
        private Canvas _creditsCanvas;
        private Canvas _backCanvas;
        
        void Start()
        {
            _mainMenuCanvas = mainMenu.GetComponent<Canvas>();
            _creditsCanvas = credits.GetComponent<Canvas>();
            Menu.ButtonAction(backButton, Back);
        }
        
        public void Back()
        {
            Menu.CanvasTransition(_creditsCanvas, _mainMenuCanvas);
        }
        
    }
}