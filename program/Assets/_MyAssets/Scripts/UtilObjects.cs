using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering.PostProcessing;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

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
        set => Cam.transform.position = value;
    }
    public Quaternion CamRotation {
        get => Cam.transform.rotation;
        set => Cam.transform.rotation = value;
    }
    public Vector3 CamForward {
        get => Cam.transform.forward;
        set => Cam.transform.forward = value;
    }
    public Vector3 CamRight {
        get => Cam.transform.right;
        set => Cam.transform.right = value;
    }
    public Vector3 CamUp {
        get => Cam.transform.up;
        set => Cam.transform.up = value;
    }
    #endregion

    #region Post Process
    [Header("Post Process")]
    [SerializeField] private PostProcessVolume volume;
    private ColorGrading colorGrading = null;
    #endregion

    #region UI
    [Flags]
    public enum Page {
        None = 0, 
        PauseMenu = 1 << 0, 
        Settings = 1 << 1, 
        KeyGuide = 1 << 2, 

    }
    private Page currentPages = Page.None;
    public Page CurrentPages {
        get => currentPages;
        set {
            Page changed = currentPages ^ value; //XOR

            Array pages = Enum.GetValues(typeof(Page));
            List<Page> changedPageList = new List<Page>();
            foreach(Page flag in Enum.GetValues(typeof(Page))) {
                if(changed.HasFlag(flag)) changedPageList.Add(flag);
            }
            
            foreach(Page page in changedPageList) {
                bool isOn = ((value & page) == page);
                OnPageChanged(page, isOn);
            }

            currentPages = value;
        }
    }

    [Header("-------------------- UI --------------------")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RayBlock rayBlock;
    private readonly Color standardRayBlockColor = Color.black;

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

    #region PauseMenu
    [Header("Pause Menu")]
    [SerializeField] private PauseMenuController pauseMenuController;
    #endregion

    #region Settings
    [Header("Settings")]
    [SerializeField] private SettingController settingController;
    #endregion

    #region Key Guide
    [Header("Key Guide")]
    [SerializeField] private KeyGuideController keyGuideController;
    #endregion

    private KeyCode Key_Escape = KeyCode.Escape;
    #endregion



    private void Awake() {
        DontDestroyOnLoad(gameObject);

        volume.profile.TryGetSettings<ColorGrading>(out colorGrading);

        UserSettings.OnMasterVolumeChanged += OnMasterVolumeChanged;
        UserSettings.OnDisplayFOVChanged += OnDisplayFOVChanged;
        UserSettings.OnDisplayBrightnessChanged += OnBrightnessChanged;
    }

    private void OnDestroy() {
        UserSettings.OnMasterVolumeChanged -= OnMasterVolumeChanged;
        UserSettings.OnDisplayFOVChanged -= OnDisplayFOVChanged;
        UserSettings.OnDisplayBrightnessChanged -= OnBrightnessChanged;
    }

    private IEnumerator Start() {
        loadingGroup.gameObject.SetActive(false);
        pauseMenuController.gameObject.SetActive(false);
        settingController.gameObject.SetActive(false);
        keyGuideController.gameObject.SetActive(false);

        rayBlock.gameObject.SetActive(true);
        rayBlock.Color = Color.black;
        rayBlock.Alpha = 1.0f;

        yield return LocalizationSettings.InitializationOperation;

        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(UserSettings.LanguageCode);
        AudioListener.volume = UserSettings.MasterVolumeRatio;
        Cam.fieldOfView = UserSettings.FOV;
        colorGrading.colorFilter.value = Color.white * UserSettings.DisplayBrightness;

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FadeOut(rayBlock.CanvasGroup, 1.0f));
        rayBlock.gameObject.SetActive(false);
    }

    private void Update() {
        if(Input.GetKeyDown(Key_Escape)) {
            if(SceneLoader.Instance.IsLoading) return;

            switch(SceneLoader.Instance.CurrentLoadedScene) {
                case SceneLoader.SceneType.Main: {
                        SetActiveSettings(!currentPages.HasFlag(Page.Settings));
                    }
                    break;
                case SceneLoader.SceneType.Game: {
                        bool isSettingOn = currentPages.HasFlag(Page.Settings);
                        if(isSettingOn) {
                            SetActiveSettings(false);
                        }
                        else {
                            bool isPauseMenuOn = currentPages.HasFlag(Page.PauseMenu);
                            if(isPauseMenuOn) {
                                SetActivePauseMenu(false);
                            }
                            else {
                                SetActivePauseMenu(true);
                            }
                        }
                    }
                    break;
            }
        }
    }

    #region Utility

    #region RayBlock
    public IEnumerator SetActiveRayBlockAction(bool active, float fadeTime = 0.0f, Color? color = null) {
        if(active) {
            rayBlock.gameObject.SetActive(true);
            if(fadeTime > 0.0f) {
                rayBlock.Color = color != null ? color.Value : standardRayBlockColor;
                rayBlock.Alpha = 0.0f;
                yield return StartCoroutine(FadeIn(rayBlock.CanvasGroup, fadeTime));
            }
            else {
                rayBlock.Alpha = 1.0f;
            }
        }
        else {
            if(fadeTime > 0.0f) {
                yield return StartCoroutine(FadeOut(rayBlock.CanvasGroup, fadeTime, false, null, () => {
                    rayBlock.gameObject.SetActive(false);
                }));
            }
            else {
                rayBlock.gameObject.SetActive(false);
            }
        }
    }
    #endregion

    #region Loading
    public IEnumerator SetActiveLoadingAction(bool active, float fadeTime = 0.0f) {
        if(active) {
            loadingGroup.gameObject.SetActive(true);
            if(fadeTime > 0.0f) {
                loadingGroup.alpha = 0.0f;
                yield return StartCoroutine(FadeIn(loadingGroup, fadeTime));
            }
            else {
                loadingGroup.alpha = 1.0f;
            }
        }
        else {
            if(fadeTime > 0.0f) {
                yield return StartCoroutine(FadeOut(loadingGroup, fadeTime, false, null, () => {
                    loadingGroup.gameObject.SetActive(false);
                }));
            }
            else {
                loadingGroup.gameObject.SetActive(false);
            }
        }
    }
    #endregion

    #region PauseMenu
    public void SetActivePauseMenu(bool active) {
        if(active) CurrentPages |= Page.PauseMenu;
        else CurrentPages &= ~Page.PauseMenu;
    }
    #endregion

    #region Settings
    public void SetActiveSettings(bool active) { 
        if(active) CurrentPages |= Page.Settings;
        else CurrentPages &= ~Page.Settings;
    }

    private IEnumerator SetActiveSettingsAction(bool active, float fadeTime = 0.0f) {
        if(active) {
            settingController.gameObject.SetActive(true);
            if(fadeTime > 0.0f) {
                settingController.Alpha = 0.0f;
                yield return StartCoroutine(FadeIn(settingController.CanvasGroup, fadeTime, true, () => {
                    settingController.ScrollValue = 0.0f;
                }, null));
            }
            else {
                settingController.Alpha = 1.0f;
            }
        }
        else {
            if(fadeTime > 0.0f) {
                yield return StartCoroutine(FadeOut(settingController.CanvasGroup, fadeTime, false, null, () => {
                    settingController.gameObject.SetActive(false);
                }));
            }
            else {
                settingController.gameObject.SetActive(false);
            }
        }
    }
    #endregion

    #region Key Guide
    public void SetActiveKeyGuide(bool active) {
        if(active) CurrentPages |= Page.KeyGuide;
        else CurrentPages &= ~Page.KeyGuide;
    }

    private IEnumerator SetActiveKeyGuideAction(bool active, float fadeTime = 0.0f) {
        if(active) {
            keyGuideController.gameObject.SetActive(true);
            if(fadeTime > 0.0f) {
                keyGuideController.Alpha = 0.0f;
                yield return StartCoroutine(FadeIn(keyGuideController.CanvasGroup, fadeTime));
            }
            else {
                keyGuideController.Alpha = 1.0f;
            }
        }
        else {
            if(fadeTime > 0.0f) {
                yield return StartCoroutine(FadeOut(keyGuideController.CanvasGroup, fadeTime, false, null, () => {
                    keyGuideController.gameObject.SetActive(false);
                }));
            }
            else {
                keyGuideController.gameObject.SetActive(false);
            }
        }
    }
    #endregion
    #endregion

    private IEnumerator FadeIn(CanvasGroup canvasGroup, float fadeTime, bool waitOneFrame = false, 
        Action oneFrameCallback = null, Action endCallback = null) {
        if(waitOneFrame) {
            yield return null;
            oneFrameCallback?.Invoke();
        }

        float currentAlpha = canvasGroup.alpha;
        float normalizedAlpha = Mathf.InverseLerp(currentAlpha, 0.0f, 1.0f);
        float timeChecker = Mathf.Lerp(0.0f, 1.0f, normalizedAlpha);
        while(timeChecker < fadeTime) {
            timeChecker += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timeChecker / fadeTime);

            yield return null;
        }

        endCallback?.Invoke();
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup, float fadeTime, bool waitOneFrame = false,
        Action oneFrameCallback = null, Action endCallback = null) {
        if(waitOneFrame) {
            yield return null;
            oneFrameCallback?.Invoke();
        }

        float currentAlpha = canvasGroup.alpha;
        float normalizedAlpha = Mathf.InverseLerp(currentAlpha, 0.0f, 1.0f);
        float timeChecker = Mathf.Lerp(0.0f, 1.0f, normalizedAlpha);
        while(timeChecker < fadeTime) {
            timeChecker += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1.0f - Mathf.Clamp01(timeChecker / fadeTime);

            yield return null;
        }

        endCallback?.Invoke();
    }

    #region Action
    private void OnDisplayFOVChanged(float fov) {
        Cam.fieldOfView = fov;
    }

    private void OnMasterVolumeChanged(float value) {
        float ratio = UserSettings.CalculateMasterVolumeRatio(value);
        AudioListener.volume = ratio;
    }

    private void OnBrightnessChanged(float value) {
        colorGrading.colorFilter.value = Color.white * value;
    }

    private void OnPageChanged(Page page, bool active) {
        switch(SceneLoader.Instance.CurrentLoadedScene) {
            case SceneLoader.SceneType.Main: OnPageChangedInMain(page, active); break;
            case SceneLoader.SceneType.Game: OnPageChangedInGame(page, active); break;
        }
    }

    private void OnPageChangedInMain(Page page, bool active) {
        switch(page) {
            case Page.Settings: {
                    if(active) {
                        StartCoroutine(SetActiveRayBlockAction(true, 0.1f, new Color(0, 0, 0, 210f / 255f)));

                        StartCoroutine(SetActiveSettingsAction(true, 0.1f));
                    }
                    else {
                        StartCoroutine(SetActiveRayBlockAction(false, 0.1f));

                        StartCoroutine(SetActiveSettingsAction(false, 0.1f));
                    }
                }
                break;
            case Page.KeyGuide: {
                    if(active) {
                        StartCoroutine(SetActiveRayBlockAction(true, 0.1f, new Color(0, 0, 0, 210f / 255f)));

                        StartCoroutine(SetActiveKeyGuideAction(true, 0.1f));
                    }
                    else {
                        StartCoroutine(SetActiveRayBlockAction(false, 0.1f));

                        StartCoroutine(SetActiveKeyGuideAction(false, 0.1f));
                    }
                }
                break;
        }
    }

    private void OnPageChangedInGame(Page page, bool active) {
        switch(page) {
            case Page.PauseMenu: {
                    if(active) {
                        SoundManager.Instance.PauseAllSounds();

                        Time.timeScale = 0.0f;
                        //AudioListener.pause = true;

                        rayBlock.gameObject.SetActive(true);
                        rayBlock.Color = new Color(0, 0, 0, 0.85f);
                        rayBlock.Alpha = 1.0f;
                        rayBlock.gameObject.SetActive(true);
                        pauseMenuController.gameObject.SetActive(true);
                    }
                    else {
                        SoundManager.Instance.UnPauseAllSound();

                        Time.timeScale = 1.0f;
                        //AudioListener.pause = false;

                        rayBlock.gameObject.SetActive(false);
                        pauseMenuController.gameObject.SetActive(false);
                    }
                }
                break;
            case Page.Settings: {
                    if(active) {
                        pauseMenuController.CanvasGroup.alpha = 0.0f;
                        StartCoroutine(SetActiveSettingsAction(true, 0.1f));
                    }
                    else {
                        pauseMenuController.CanvasGroup.alpha = 1.0f;
                        StartCoroutine(SetActiveSettingsAction(false, 0.1f));
                    }
                }
                break;
            case Page.KeyGuide: {
                    if(active) {
                        pauseMenuController.CanvasGroup.alpha = 0.0f;
                        StartCoroutine(SetActiveKeyGuideAction(true, 0.1f));
                    }
                    else {
                        pauseMenuController.CanvasGroup.alpha = 1.0f;
                        StartCoroutine(SetActiveKeyGuideAction(false, 0.1f));
                    }
                }
                break;
        }
    }
    #endregion
}
