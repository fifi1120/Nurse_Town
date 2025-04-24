using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class SceneTracker : MonoBehaviour
{
    public Button finishButton;
    private HashSet<string> visitedScenes = new HashSet<string>();
    private int totalScenes = 5;

    private void Start()
    {
        finishButton.interactable = false; // Disable Finish at start
        
        // Load previously visited scenes from PlayerPrefs
        for (int i = 1; i <= totalScenes; i++)
        {
            if (PlayerPrefs.GetInt("Scene" + i, 0) == 1) // Check if visited
            {
                visitedScenes.Add("Scene" + i);
            }
        }
        
        Debug.Log("Visited Scenes Count: " + visitedScenes.Count);

        CheckCompletion(); // Check if all scenes were visited
    }

    // Separate functions for each scene (to be assigned in Unity Inspector)
    public void LoadScene1() { MarkSceneAsVisited("Scenes/ICU_Interview/Primary Nurse", 1); }
    public void LoadScene2() { MarkSceneAsVisited("Scenes/ICU_Interview/ICU Anesthesiologist", 2); }
    public void LoadScene3() { MarkSceneAsVisited("Scenes/ICU_Interview/MedicalStudent", 3); }
    public void LoadScene4() { MarkSceneAsVisited("Scenes/ICU_Interview/ICU Nurse", 4); }
    public void LoadScene5() { MarkSceneAsVisited("Scenes/ICU_Interview/ICU Doctor", 5); }

    private void MarkSceneAsVisited(string sceneName, int sceneNumber)
    {
        PlayerPrefs.SetInt("Scene" + sceneNumber, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneName);
    }

    public void ReturnToGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void CheckCompletion()
    {
        if (visitedScenes.Count >= totalScenes)
        {
            finishButton.interactable = true; // Enable Finish button
        }
    }

    public void FinishGame()
    {
        // Clear all scene visit data
        // for (int i = 1; i <= totalScenes; i++)
        // {
        //     PlayerPrefs.DeleteKey("Scene" + i);
        // }

        // PlayerPrefs.Save(); // Ensure changes are saved

        // Optionally, reload the game scene or go to a main menu
        SceneManager.LoadScene("Assessment");
    }
}
