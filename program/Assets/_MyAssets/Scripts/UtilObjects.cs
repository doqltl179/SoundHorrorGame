using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UtilObjects : ResourceGenericSingleton<UtilObjects> {
    #region Camera
    private Camera cam = null;
    public Camera Cam {
        get {
            if(cam == null) {
                cam = GetComponentInChildren<Camera>();
                if(cam == null) {
                    Debug.LogError("Camera not found.");
                }
            }
            return cam;
        }
    }

    public Vector3 CamPos {
        get => Cam.transform.position;
        set {
            Cam.transform.position = value;
        }
    }

    public Vector3 CamForward {
        get => Cam.transform.forward;
        set {
            Cam.transform.forward = value;
        }
    }
    #endregion

    #region UI
    [Header("-------------------- UI --------------------")]
    [SerializeField] private Canvas canvas;

    #region Loading
    [Header("Loading")]
    [SerializeField] private CanvasGroup loadingGroup;
    [SerializeField] private TextMeshProUGUI loadingText;

    public string LoadingText {
        get => loadingText.text;
        set {
            loadingText.text = value;
        }
    }

    public float LoadingAlpha {
        get => loadingGroup.alpha;
        set {
            loadingGroup.alpha = value;
        }
    }
    #endregion

    #region Settings
    [Header("Settings")]
    [SerializeField] private SettingController settingController;

    public Action<UserSettingsHelper> OnSettingChanged;
    #endregion
    #endregion



    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        loadingGroup.gameObject.SetActive(false);
    }

    #region Utility


    #region Loading
    public void SetActiveLoadingUI(bool active) => loadingGroup.gameObject.SetActive(active);

    public IEnumerator FadeInLoadingUI(float fadeTime) {
        float timeChecker = 0.0f;
        while(timeChecker < fadeTime) {
            timeChecker += Time.deltaTime;
            LoadingAlpha = Mathf.Clamp01(timeChecker / fadeTime);

            yield return null;
        }
    }

    public IEnumerator FadeOutLoadingUI(float fadeTime) {
        float timeChecker = 0.0f;
        while(timeChecker < fadeTime) {
            timeChecker += Time.deltaTime;
            LoadingAlpha = 1.0f - Mathf.Clamp01(timeChecker / fadeTime);

            yield return null;
        }
    }
    #endregion
    #endregion
}
