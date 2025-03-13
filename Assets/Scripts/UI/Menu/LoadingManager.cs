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
        }

        public void LoadScene(string sceneName)
        {
            _loadingScreen.enabled = true;
            StartCoroutine(LoadingScreen(sceneName));
        }

        public IEnumerator LoadingScreen(string sceneName)
        {
            yield return new WaitForSeconds(2);
            SceneManager.LoadScene(sceneName);
        }
    }
}