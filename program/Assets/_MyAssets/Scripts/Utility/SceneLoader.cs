
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : GenericSingleton<SceneLoader> {
    public object[] Param { get; private set; } = null;

    public enum SceneType {
        None,
        Main, 
        Game, 
    }

    public bool IsLoading { get { return loadSceneCoroutine != null; } }
    public SceneType CurrentLoadedScene { get; private set; }

    private IEnumerator loadSceneCoroutine = null;
    //private IEnumerator loadingTextAnimationCoroutine = null;



    #region Utility
    public void LoadScene(SceneType scene, object[] param = null) {
        if(loadSceneCoroutine == null) {
            Param = param;

            loadSceneCoroutine = LoadSceneCoroutine(scene);
            StartCoroutine(loadSceneCoroutine);
        }
    }

    /// <summary>
    /// 컴포넌트에 선언된 "CurrentLoadedScene"의 값만 바꾸고 실제로 Scene을 이동하지는 않음
    /// </summary>
    public void ChangeCurrentLoadedSceneImmediately(SceneType type) {
        CurrentLoadedScene = type;
    }
    #endregion

    private IEnumerator LoadSceneCoroutine(SceneType scene) {
        //if(loadingTextAnimationCoroutine == null) {
        //    loadingTextAnimationCoroutine = LoadingTextAnimationCoroutine();
        //    StartCoroutine(loadingTextAnimationCoroutine);
        //}

        StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(true, 0.5f));
        yield return UtilObjects.Instance.SetActiveLoadingAction(true, 0.5f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene.ToString());
        while(!asyncLoad.isDone) {


            yield return null;
        }
        yield return new WaitForSeconds(1.0f); //Fake Wait

        //StopCoroutine(loadingTextAnimationCoroutine);
        //UtilObjects.Instance.LoadingText = string.Empty;

        StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(false, 0.5f));
        yield return UtilObjects.Instance.SetActiveLoadingAction(false, 0.5f);

        CurrentLoadedScene = scene;

        loadSceneCoroutine = null;
    }

    //private IEnumerator LoadingTextAnimationCoroutine() {
    //    const string loadingText = "Loading";
    //    const string addChar = ".";
    //    const int addCount = 3;
    //    int addCountChecker = 1;

    //    const float textChangeTime = 0.5f;
    //    float textChangeTimer = 0.0f;

    //    StringBuilder loadingTextBuilder = new StringBuilder();
    //    loadingTextBuilder.Append(loadingText).Append(addChar);
    //    UtilObjects.Instance.LoadingText = loadingTextBuilder.ToString();

    //    while(true) {
    //        textChangeTimer += Time.deltaTime;
    //        if(textChangeTimer > textChangeTime) {
    //            loadingTextBuilder.Append(addChar);
    //            UtilObjects.Instance.LoadingText = loadingTextBuilder.ToString();

    //            addCountChecker++;
    //            if(addCountChecker >= addCount) {
    //                loadingTextBuilder.Remove(loadingTextBuilder.Length - addCount, addCount);

    //                addCountChecker = 0;
    //            }

    //            textChangeTimer = 0.0f;
    //        }

    //        yield return null;
    //    }

    //    loadingTextAnimationCoroutine = null;
    //}
}
