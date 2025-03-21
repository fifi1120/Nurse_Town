using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.Menu
{
    public class ScenarioSelect : MonoBehaviour
    {
        public Canvas welcomeCanvas;
        public Canvas scenarioSelectCanvas;
        public Canvas scenario1DescriptionCanvas;
        public Canvas scenario2DescriptionCanvas;
        public Canvas scenario3DescriptionCanvas;
        public Canvas scenario4DescriptionCanvas;
        public Canvas scenario5DescriptionCanvas;
        public Canvas loadingScreenCanvas;
        
        public Button scenario1Button;
        public Button scenario2Button;
        public Button scenario3Button;
        public Button scenario4Button;
        public Button scenario5Button;
        public Button nextButton;
        public Button backButton;
        public Button finishButton;
        void Start()
        {
            //welcomeCanvas = GameObject.Find("Welcome Canvas").GetComponent<Canvas>();
            //scenarioSelectCanvas = GameObject.Find("Scenario Canvas").GetComponent<Canvas>();
            
            welcomeCanvas.enabled = true;
            scenarioSelectCanvas.enabled = false;
            scenario1DescriptionCanvas.enabled = false;
            scenario2DescriptionCanvas.enabled = false;
            scenario3DescriptionCanvas.enabled = false;
            scenario4DescriptionCanvas.enabled = false;
            scenario5DescriptionCanvas.enabled = false;
            loadingScreenCanvas.enabled = false;
            
            Menu.ButtonAction(nextButton, NextWindow);
            Menu.ButtonAction(backButton, BackWindow);
            Menu.ButtonAction(finishButton, Finish);
            Menu.ButtonAction(scenario1Button, ShowDescription(scenario1DescriptionCanvas) );
            Menu.ButtonAction(scenario2Button, ShowDescription(scenario2DescriptionCanvas) );
            Menu.ButtonAction(scenario3Button, ShowDescription(scenario3DescriptionCanvas) );
            Menu.ButtonAction(scenario4Button, ShowDescription(scenario4DescriptionCanvas) );
            Menu.ButtonAction(scenario5Button, ShowDescription(scenario5DescriptionCanvas) );
        }
        
        private void NextWindow()
        {
            Menu.CanvasTransition(welcomeCanvas, scenarioSelectCanvas);
        }
        
        private void BackWindow()
        {
            Menu.CanvasTransition(scenarioSelectCanvas, welcomeCanvas);
        }
        private void Finish()
        {
            // Here you would typically load the next scene or perform some action
            Debug.Log("Finish button clicked. Implement your logic here.");
            // For example, you could load a new scene:
            // LoadingManager.Instance.LoadScene("YourNextSceneName");
        }

        private UnityAction ShowDescription(Canvas descriptionCanvas)
        {
            return () => 
            {
                // This lambda function will be called when the button is clicked
                Menu.CanvasTransition(scenarioSelectCanvas, descriptionCanvas);
            };
        }

    }
}