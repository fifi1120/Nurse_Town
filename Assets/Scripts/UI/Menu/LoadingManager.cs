using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Menu
{

    public class LoadingManager : MonoBehaviour
    {
        public static LoadingManager Instance;
        [SerializeField] private Canvas _loadingScreen;

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }

            _loadingScreen = GameObject.Find("Loading Screen").GetComponent<Canvas>();
            _loadingScreen.enabled = false; // Ensure loading screen is initially hidden
        }

        public void LoadScene(string sceneName)
        {
            Debug.Log("Loading scene: " + sceneName);
            SceneManager.LoadScene(sceneName);
            //_loadingScreen.enabled = true;
            //Debug.Log("Loading scene: " + sceneName);
            //StartCoroutine(LoadingScreen(sceneName));
        }

        public IEnumerator LoadingScreen(string sceneName)
        {
            Debug.Log("Starting loading coroutine for scene: " + sceneName);
            yield return new WaitForSeconds(2);
            Debug.Log("Loading scene after delay: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }
}