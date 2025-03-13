using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Menu
{
    public static class Menu
    {
        public static void ButtonAction(Button button, UnityAction functionName)
        {
            try
            {
                button.onClick.AddListener(functionName);
            }
            catch
            {
                Debug.LogError("\tRegisterFunc with button "
                               + button.name
                               + "\n\tand function "
                               + functionName.Method.Name
                               + "\n\thas failed by "
                               + (button == null ? "null button" : "add listener"));
            }
        }

        public static void CanvasTransition(Canvas currentCanvas, Canvas nextCanvas)
        {
            currentCanvas.enabled = false;
            nextCanvas.enabled = true;
        }
        
        
    }
}