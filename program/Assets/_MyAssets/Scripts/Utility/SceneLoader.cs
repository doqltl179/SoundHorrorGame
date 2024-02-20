
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : GenericSingleton<SceneLoader> {
    public enum SceneType {
        None,
        Main, 
        Game, 
    }

    public bool IsLoading { get { return loadSceneCoroutine != null; } }
    public SceneType CurrentLoadScene { get; private set; }

    private IEnumerator loadSceneCoroutine = null;
    private IEnumerator loadingTextAnimationCoroutine = null;



    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    #region Utility
    public void LoadScene(SceneType scene) {
        if(loadSceneCoroutine == null) {
            loadSceneCoroutine = LoadSceneCoroutine(scene);
            StartCoroutine(loadSceneCoroutine);
        }
    }
    #endregion

    private IEnumerator LoadSceneCoroutine(SceneType scene) {
        if(loadingTextAnimationCoroutine == null) {
            loadingTextAnimationCoroutine = LoadingTextAnimationCoroutine();
            StartCoroutine(loadingTextAnimationCoroutine);
        }

        UtilObjects.Instance.SetActiveLoadingUI(true);
        yield return UtilObjects.Instance.FadeInLoadingUI(0.5f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene.ToString());
        while(!asyncLoad.isDone) {


            yield return null;
        }
        yield return new WaitForSeconds(1.0f); //Fake Wait

        StopCoroutine(loadingTextAnimationCoroutine);
        UtilObjects.Instance.LoadingText = string.Empty;

        yield return UtilObjects.Instance.FadeOutLoadingUI(0.5f);
        UtilObjects.Instance.SetActiveLoadingUI(false);

        CurrentLoadScene = scene;

        loadSceneCoroutine = null;
    }

    private IEnumerator LoadingTextAnimationCoroutine() {
        const string loadingText = "Loading";
        const string addChar = ".";
        const int addCount = 3;
        int addCountChecker = 1;

        const float textChangeTime = 0.5f;
        float textChangeTimer = 0.0f;

        StringBuilder loadingTextBuilder = new StringBuilder();
        loadingTextBuilder.Append(loadingText).Append(addChar);
        UtilObjects.Instance.LoadingText = loadingTextBuilder.ToString();

        while(true) {
            textChangeTimer += Time.deltaTime;
            if(textChangeTimer > textChangeTime) {
                loadingTextBuilder.Append(addChar);
                UtilObjects.Instance.LoadingText = loadingTextBuilder.ToString();

                addCountChecker++;
                if(addCountChecker >= addCount) {
                    loadingTextBuilder.Remove(loadingTextBuilder.Length - addCount, addCount);

                    addCountChecker = 0;
                }

                textChangeTimer = 0.0f;
            }

            yield return null;
        }

        loadingTextAnimationCoroutine = null;
    }
}
