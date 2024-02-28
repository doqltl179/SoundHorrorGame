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
    #endregion
    #endregion



    private void Awake() {
        DontDestroyOnLoad(gameObject);

        UserSettings.OnMasterVolumeChanged += OnMasterVolumeChanged;
        UserSettings.OnDisplayFOVChanged += OnDisplayFOVChanged;
    }

    private void OnDestroy() {
        UserSettings.OnMasterVolumeChanged -= OnMasterVolumeChanged;
        UserSettings.OnDisplayFOVChanged -= OnDisplayFOVChanged;
    }

    private void Start() {
        loadingGroup.gameObject.SetActive(false);
        settingController.gameObject.SetActive(false);

        AudioListener.volume = UserSettings.MasterVolumeRatio;
        Cam.fieldOfView = UserSettings.FOV;
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

    #region Settings
    public void SetActiveSettingUI(bool active) => settingController.gameObject.SetActive(active);
    #endregion
    #endregion

    #region Action
    private void OnDisplayFOVChanged(float fov) {
        Cam.fieldOfView = fov;
    }

    private void OnMasterVolumeChanged(float value) {
        float ratio = UserSettings.CalculateMasterVolumeRatio(value);
        AudioListener.volume = ratio;
    }
    #endregion
}
