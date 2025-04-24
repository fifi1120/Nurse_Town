using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    public class PauseHelp : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject helpMenu;
        [SerializeField] private Button backButton;

        private Canvas _pauseMenuCanvas;
        private Canvas _helpCanvas;
        private Canvas _backCanvas;
        
        void Start()
        {
            _pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
            _helpCanvas = helpMenu.GetComponent<Canvas>();
            Menu.ButtonAction(backButton, Back);
        }
        
        public void Back()
        {
            //Menu.CanvasTransition(_helpCanvas, _pauseMenuCanvas);
            helpMenu.SetActive(false);
        }
        
    }
}