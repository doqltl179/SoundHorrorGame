using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering.PostProcessing;

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
    public Vector3 CamLocalPos {
        get => Cam.transform.localPosition;
        set => Cam.transform.localPosition = value;
    }
    public Quaternion CamRotation {
        get => Cam.transform.rotation;
        set => Cam.transform.rotation = value;
    }
    public Quaternion CamLocalRotation {
        get => Cam.transform.localRotation;
        set => Cam.transform.localRotation = value;
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
        ConfirmNotice = 1 << 3,
        ButtonSelectMenu = 1 << 4,
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

            if(changedPageList.Count > 0) {
                // 우선 순위가 높은 페이지를 먼저 처리하기 위함
                changedPageList.Reverse();

                foreach(Page page in changedPageList) {
                    bool isOn = ((value & page) == page);
                    OnPageChanged(page, isOn);
                }
            }

            currentPages = value;
        }
    }

    [Header("-------------------- UI --------------------")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RayBlock rayBlock;
    public bool RayBlockActive { get { return rayBlock.gameObject.activeSelf; } }
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
    public bool IsGamePaused { get; private set; }
    #endregion

    #region Settings
    [Header("Settings")]
    [SerializeField] private SettingController settingController;
    #endregion

    #region Key Guide
    [Header("Key Guide")]
    [SerializeField] private KeyGuideController keyGuideController;
    #endregion

    #region Confirm Notice
    [Header("Confirm Notice")]
    [SerializeField] private ConfirmNoticeController confirmNoticeController;
    #endregion

    #region Cursor
    [Header("Cursor")]
    [SerializeField] private CursorImageController cursorImageController;
    private bool menuActiveStartCursorChecker;
    #endregion

    #region Button Select Menu
    [Header("Button Select Menu")] 
    [SerializeField] private ButtonSelectMenuController buttonSelectMenuController;

    #region FPS Pannel
    [Header("FPS Pannel")]
    [SerializeField] private GameObject fpsPannel;
    #endregion
    #endregion

    private KeyCode Key_Escape = KeyCode.Escape;
    #endregion

    public Action<bool> OnGamePaused;



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
        if(UserSettings.IsFirstStart) {
            string[] enCode = new string[] { "en", "US" };
            string[] koCode = new string[] { "ko", "KR" };
            string[] jaCode = new string[] { "ja", "JP" };
            string[] zhCode = new string[] { "zh", "CN" };
            string[] currentCode = CultureInfo.InstalledUICulture.Name.Split('-');

            if(currentCode.Any(t => koCode.Contains(t))) UserSettings.LanguageCode = koCode[0];
            else if(currentCode.Any(t => jaCode.Contains(t))) UserSettings.LanguageCode = jaCode[0];
            else UserSettings.LanguageCode = enCode[0];

            if(Display.displays.Length > 1) {
                int mostLongerIndex = 0;
                for(int i = 1; i < Display.displays.Length; i++) {
                    if(Display.displays[i].systemWidth > Display.displays[mostLongerIndex].systemWidth) {
                        mostLongerIndex = i;
                    }
                }
                Display.displays[mostLongerIndex].Activate();
            }

            UserSettings.IsFirstStart = false;
        }

        loadingGroup.gameObject.SetActive(false);
        pauseMenuController.gameObject.SetActive(false);
        settingController.gameObject.SetActive(false);
        keyGuideController.gameObject.SetActive(false);
        confirmNoticeController.gameObject.SetActive(false);
        buttonSelectMenuController.gameObject.SetActive(false);

#if Show_FPS
        fpsPannel.SetActive(true);
#else
        fpsPannel.SetActive(false);
#endif

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
                        if(currentPages == Page.None) {
                            SetActiveSettings(true);
                        }
                        else {
                            if(currentPages.HasFlag(Page.Settings)) {
                                SetActiveSettings(false);
                            }
                            else if(currentPages.HasFlag(Page.KeyGuide)) {
                                SetActiveKeyGuide(false);
                            }
                            else if(currentPages.HasFlag(Page.ButtonSelectMenu)) {
                                SetActiveButtonSelectMenu(false);
                            }
                            else if(currentPages.HasFlag(Page.ConfirmNotice)) {
                                SetActiveConfirmNotice(false);
                            }
                        }
                    }
                    break;
                case SceneLoader.SceneType.Credits:
                case SceneLoader.SceneType.Game: {
                        if(currentPages == Page.None) {
                            SetActivePauseMenu(true);
                        }
                        else {
                            if(currentPages.HasFlag(Page.Settings)) {
                                SetActiveSettings(false);
                            }
                            else if(currentPages.HasFlag(Page.KeyGuide)) {
                                SetActiveKeyGuide(false);
                            }
                            else if(currentPages.HasFlag(Page.ConfirmNotice)) {
                                SetActiveConfirmNotice(false);
                            }
                            else if(currentPages.HasFlag(Page.PauseMenu)) {
                                SetActivePauseMenu(false);
                            }
                        }
                    }
                    break;
            }
        }
    }

    #region Utility

    public void ResetPages() {
        CurrentPages = Page.None;
    }

    #region RayBlock
    public IEnumerator SetActiveRayBlockAction(bool active, float fadeTime = 0.0f, Color? color = null, float delay = 0.0f) {
        if(delay > 0.0f) {
            yield return new WaitForSeconds(delay);
        }

        if(active) {
            rayBlock.gameObject.SetActive(true);
            if(fadeTime > 0.0f) {
                rayBlock.Color = color != null ? color.Value : standardRayBlockColor;
                rayBlock.Alpha = 0.0f;
                yield return StartCoroutine(FadeIn(rayBlock.CanvasGroup, fadeTime));
            }
            else {
                rayBlock.Color = color != null ? color.Value : standardRayBlockColor;
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

    private IEnumerator SetActivePauseMenuAction(bool active, float fadeTime = 0.0f) {
        if(active) {
            pauseMenuController.gameObject.SetActive(true);
            if(fadeTime > 0.0f) {
                pauseMenuController.Alpha = 0.0f;
                yield return StartCoroutine(FadeIn(pauseMenuController.CanvasGroup, fadeTime));
            }
            else {
                pauseMenuController.Alpha = 1.0f;
            }
        }
        else {
            if(fadeTime > 0.0f) {
                yield return StartCoroutine(FadeOut(pauseMenuController.CanvasGroup, fadeTime, false, null, () => {
                    pauseMenuController.gameObject.SetActive(false);
                }));
            }
            else {
                pauseMenuController.gameObject.SetActive(false);
            }
        }
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

    #region Confirm Notice
    public void SetActiveConfirmNotice(bool active) {
        if(active) CurrentPages |= Page.ConfirmNotice;
        else CurrentPages &= ~Page.ConfirmNotice;
    }

    public void InitConfirmNotice(string messageKey, string key1, Action action1, string key2 = null, Action action2 = null) {
        confirmNoticeController.Init(messageKey, key1, action1, key2, action2);
    }

    private IEnumerator SetActiveConfirmNoticeAction(bool active, float fadeTime = 0.0f) {
        if(active) {
            confirmNoticeController.gameObject.SetActive(true);
            if(fadeTime > 0.0f) {
                confirmNoticeController.Alpha = 0.0f;
                yield return StartCoroutine(FadeIn(confirmNoticeController.CanvasGroup, fadeTime));
            }
            else {
                confirmNoticeController.Alpha = 1.0f;
            }
        }
        else {
            if(fadeTime > 0.0f) {
                yield return StartCoroutine(FadeOut(confirmNoticeController.CanvasGroup, fadeTime, false, null, () => {
                    confirmNoticeController.gameObject.SetActive(false);
                }));
            }
            else {
                confirmNoticeController.gameObject.SetActive(false);
            }
        }
    }
    #endregion

    #region Cursor
    public void SetActiveCursorImage(bool active) {
        if(active) {
            cursorImageController.gameObject.SetActive(true);
        }
        else {
            cursorImageController.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Button Select Menu
    public void SetActiveButtonSelectMenu(bool active, ButtonSelectMenuStruct[] structs = null) {
        buttonSelectMenuController.InitMenu(structs);

        if(active) CurrentPages |= Page.ButtonSelectMenu;
        else CurrentPages &= ~Page.ButtonSelectMenu;
    }

    private IEnumerator SetActiveButtonSelectMenuAction(bool active, float fadeTime = 0.0f) {
        if(active) {
            buttonSelectMenuController.gameObject.SetActive(true);
            if(fadeTime > 0.0f) {
                buttonSelectMenuController.Alpha = 0.0f;
                yield return StartCoroutine(FadeIn(buttonSelectMenuController.CanvasGroup, fadeTime));
            }
            else {
                buttonSelectMenuController.Alpha = 1.0f;
            }
        }
        else {
            if(fadeTime > 0.0f) {
                yield return StartCoroutine(FadeOut(buttonSelectMenuController.CanvasGroup, fadeTime, false, null, () => {
                    buttonSelectMenuController.gameObject.SetActive(false);
                }));
            }
            else {
                buttonSelectMenuController.gameObject.SetActive(false);
            }
        }
        yield return null;
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
            case SceneLoader.SceneType.Credits:
            case SceneLoader.SceneType.Game: 
                OnPageChangedInGame(page, active); 
                break;
        }
    }

    private void OnPageChangedInMain(Page page, bool active) {
        switch(page) {
            case Page.Settings: {
                    if(active) {
                        StartCoroutine(SetActiveRayBlockAction(true, 0.0f, new Color(0, 0, 0, 210f / 255f)));

                        StartCoroutine(SetActiveSettingsAction(true, 0.0f));
                    }
                    else {
                        StartCoroutine(SetActiveRayBlockAction(false, 0.0f));

                        StartCoroutine(SetActiveSettingsAction(false, 0.0f));
                    }
                }
                break;
            case Page.KeyGuide: {
                    if(active) {
                        StartCoroutine(SetActiveRayBlockAction(true, 0.0f, new Color(0, 0, 0, 210f / 255f)));

                        StartCoroutine(SetActiveKeyGuideAction(true, 0.0f));
                    }
                    else {
                        StartCoroutine(SetActiveRayBlockAction(false, 0.0f));

                        StartCoroutine(SetActiveKeyGuideAction(false, 0.0f));
                    }
                }
                break;
            case Page.ButtonSelectMenu: {
                    if(active) {
                        StartCoroutine(SetActiveRayBlockAction(true, 0.0f, new Color(0, 0, 0, 210f / 255f)));

                        StartCoroutine(SetActiveButtonSelectMenuAction(true, 0.0f));
                    }
                    else {
                        StartCoroutine(SetActiveRayBlockAction(false, 0.0f));

                        StartCoroutine(SetActiveButtonSelectMenuAction(false, 0.0f));
                    }
                }
                break;
            case Page.ConfirmNotice: {
                    if(active) {
                        StartCoroutine(SetActiveRayBlockAction(true, 0.0f, new Color(0, 0, 0, 210f / 255f)));

                        StartCoroutine(SetActiveConfirmNoticeAction(true, 0.0f));
                    }
                    else {
                        StartCoroutine(SetActiveRayBlockAction(false, 0.0f));

                        StartCoroutine(SetActiveConfirmNoticeAction(false, 0.0f));
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

                        menuActiveStartCursorChecker = cursorImageController.gameObject.activeSelf;
                        SetActiveCursorImage(false);

                        StartCoroutine(SetActiveRayBlockAction(true, 0.0f, new Color(0, 0, 0, 210f / 255f)));

                        StartCoroutine(SetActivePauseMenuAction(true, 0.0f));

                        IsGamePaused = true;
                        OnGamePaused?.Invoke(true);
                    }
                    else {
                        SoundManager.Instance.UnPauseAllSound();
                        Time.timeScale = 1.0f;

                        SetActiveCursorImage(menuActiveStartCursorChecker);

                        StartCoroutine(SetActiveRayBlockAction(false, 0.0f, new Color(0, 0, 0, 210f / 255f)));

                        StartCoroutine(SetActivePauseMenuAction(false, 0.0f));

                        IsGamePaused = false;
                        OnGamePaused?.Invoke(false);
                    }
                }
                break;
            case Page.Settings: {
                    if(active) {
                        StartCoroutine(SetActivePauseMenuAction(false, 0.0f));

                        StartCoroutine(SetActiveSettingsAction(true, 0.0f));
                    }
                    else {
                        StartCoroutine(SetActivePauseMenuAction(true, 0.0f));

                        StartCoroutine(SetActiveSettingsAction(false, 0.0f));
                    }
                }
                break;
            case Page.KeyGuide: {
                    if(active) {
                        StartCoroutine(SetActivePauseMenuAction(false, 0.0f));

                        StartCoroutine(SetActiveKeyGuideAction(true, 0.0f));
                    }
                    else {
                        StartCoroutine(SetActivePauseMenuAction(true, 0.0f));

                        StartCoroutine(SetActiveKeyGuideAction(false, 0.0f));
                    }
                }
                break;
            case Page.ConfirmNotice: {
                    if(active) {
                        StartCoroutine(SetActivePauseMenuAction(false, 0.0f));

                        StartCoroutine(SetActiveConfirmNoticeAction(true, 0.0f));
                    }
                    else {
                        StartCoroutine(SetActivePauseMenuAction(true, 0.0f));

                        StartCoroutine(SetActiveConfirmNoticeAction(false, 0.0f));
                    }
                }
                break;
        }
    }
    #endregion
}
