using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour {
    [Header("Microphone")]
    [SerializeField] private RectTransform micGageRect;
    [SerializeField] private CanvasGroup micGageCanvasGroup;
    [SerializeField] private Slider micGageSlider;
    [SerializeField] private Image micGageSliderFillImage;
    [SerializeField] private Color micGageSliderStartColor;
    [SerializeField] private Color micGageSliderEndColor;
    [SerializeField] private RectTransform micGageSliderLimitLine;
    /// <summary>
    /// Editor상에서 Gage의 위아래로 4만큼의 padding이 들어가 있다.
    /// </summary>
    private const float micGageRectHeightOffset = -8;

    public bool MicGageActive {
        get => micGageCanvasGroup.gameObject.activeSelf;
        set => micGageCanvasGroup.gameObject.SetActive(value);
    }
    public float MicGageAlpha {
        get => micGageCanvasGroup.alpha;
        set => micGageCanvasGroup.alpha = value;
    }

    [Header("Item")]
    [SerializeField] private CanvasGroup collectItemCanvasGroup;
    [SerializeField] private TextMeshProUGUI itemCountText;

    public bool CollectItemActive {
        get => collectItemCanvasGroup.gameObject.activeSelf;
        set => collectItemCanvasGroup.gameObject.SetActive(value);
    }
    public float CollectItemAlpha {
        get => collectItemCanvasGroup.alpha;
        set => collectItemCanvasGroup.alpha = value;
    }

    [Header("Run")]
    [SerializeField] private CanvasGroup runGageCanvasGroup;
    [SerializeField] private Image[] runGageImages;
    [SerializeField] private Color runGageStartColor;
    [SerializeField] private Color runGageEndColor;
    [SerializeField, Range(0.0f, 1.0f)] private float runGageMaxAlpha = 1.0f;

    public bool RunGageActive {
        get => runGageCanvasGroup.gameObject.activeSelf;
        set => runGageCanvasGroup.gameObject.SetActive(value);
    }
    public float RunGageAlpha {
        get => runGageCanvasGroup.alpha;
        set => runGageCanvasGroup.alpha = value;
    }
    public float RunGage {
        get => runGageImages[0].fillAmount;
        set {
            foreach(Image image in runGageImages) {
                image.fillAmount = value;
            }
        }
    }

    [Header("Head Message")]
    [SerializeField] private CanvasGroup headMessageCanvasGroup;
    [SerializeField] private TextMeshProUGUI headMessageText;
    [SerializeField] private LocalizeStringEvent headMessageLocalizeEvent;
    public bool HeadMessageActive {
        get => headMessageCanvasGroup.gameObject.activeSelf;
        set => headMessageCanvasGroup.gameObject.SetActive(value);
    }
    public float HeadMessageAlpha {
        get => headMessageCanvasGroup.alpha;
        set => headMessageCanvasGroup.alpha = value;
    }
    public string HeadMessageText {
        get => headMessageText.text;
        set => headMessageText.text = value;
    }
    public string HeadMessageKey {
        get => headMessageLocalizeEvent.StringReference.TableEntryReference.Key;
        set => headMessageLocalizeEvent.StringReference.TableEntryReference = value;
    }

    private IEnumerator headMessageFadeAnimationCoroutine = null;

    [Header("Message Box")]
    [SerializeField] private CanvasGroup messageCanvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private LocalizeStringEvent messageLocalizeEvent;
    [SerializeField] private Button[] messageButtons; //Yes, No
    [SerializeField] private Button[] scenarioButtons; //Previous, Next
    [SerializeField] private Button messageBoxButton;
    public bool MessageActive {
        get => messageCanvasGroup.gameObject.activeSelf;
        set => messageCanvasGroup.gameObject.SetActive(value);
    }
    public float MessageAlpha {
        get => messageCanvasGroup.alpha;
        set => messageCanvasGroup.alpha = value;
    }
    public string MessageText {
        get => messageText.text;
        set => messageText.text = value;
    }
    public float MessageSize {
        get => messageText.fontSize;
        set => messageText.fontSize = value;
    }
    public Color MessageColor {
        get => messageText.color;
        set => messageText.color = value;
    }
    public string MessageKey {
        get => messageLocalizeEvent.StringReference.TableEntryReference.Key;
        set => messageLocalizeEvent.StringReference.TableEntryReference = value;
    }
    public Button.ButtonClickedEvent MessageBoxClicked {
        get => messageBoxButton.onClick;
    }

    [Header("Warning")]
    [SerializeField] private CanvasGroup warningCanvasGroup;
    [SerializeField, Range(0.0f, 1.0f)] private float warningAlphaMax = 0.5f;
    public bool WarningActive {
        get => warningCanvasGroup.gameObject.activeSelf;
        set => warningCanvasGroup.gameObject.SetActive(value);
    }
    public float WarningAlpha {
        get => warningCanvasGroup.alpha;
        set => warningCanvasGroup.alpha = value;
    }



    private void Awake() {
        PlayerController.Instance.OnOverHitChanged += OnOverHitChanged;

        UserSettings.OnUseMicChanged += OnUseMicrophoneChanged;
    }

    private void OnDestroy() {
        PlayerController.Instance.OnOverHitChanged -= OnOverHitChanged;

        UserSettings.OnUseMicChanged -= OnUseMicrophoneChanged;
    }

    private void Start() {
        micGageSlider.value = 0.0f;
        RunGage = 0.0f;

        //itemCountText.text = "x" + LevelLoader.Instance.CollectedItemCount;
        messageText.text = "";

        micGageSliderLimitLine.anchoredPosition =
            Vector2.up *
            (micGageRect.sizeDelta.y + micGageRectHeightOffset) *
            MicrophoneRecorder.Instance.DecibelCriticalRatio;

        collectItemCanvasGroup.gameObject.SetActive(false);
        runGageCanvasGroup.gameObject.SetActive(false);
        messageCanvasGroup.gameObject.SetActive(false);
        warningCanvasGroup.gameObject.SetActive(false);
        micGageCanvasGroup.gameObject.SetActive(UserSettings.UseMicBoolean);
    }

    private void LateUpdate() {
        #region Microphone
        micGageSlider.value = Mathf.Lerp(micGageSlider.value, MicrophoneRecorder.Instance.DecibelRatio, Time.deltaTime * Mathf.Pow(2, 4));
        if(MicrophoneRecorder.Instance.OverCritical) {
            float halfOverRatio = 0.5f + 0.5f * MicrophoneRecorder.Instance.DecibelCriticalRatio;
            if(micGageSlider.value > halfOverRatio) {
                micGageSliderFillImage.color = micGageSliderEndColor;
            }
            else {
                float ratio = Mathf.InverseLerp(MicrophoneRecorder.Instance.DecibelCriticalRatio, halfOverRatio, micGageSlider.value);
                micGageSliderFillImage.color = Color.Lerp(micGageSliderStartColor, micGageSliderEndColor, ratio);
            }
        }
        else {
            micGageSliderFillImage.color = micGageSliderStartColor;
        }
        #endregion

        #region Player Run
        RunGage = PlayerController.Instance.NormalizedRunTime;

        if(RunGage < 0.15) {
            runGageCanvasGroup.alpha = Mathf.InverseLerp(0.0f, 0.15f, RunGage) * runGageMaxAlpha;
        }
        else {
            runGageCanvasGroup.alpha = runGageMaxAlpha;
        }

        if(!PlayerController.Instance.OverHit) {
            Color runGageColor = Color.Lerp(runGageStartColor, runGageEndColor, RunGage);
            foreach(Image image in runGageImages) {
                image.color = runGageColor;
            }
        }
        else {
            // 정규화 * RunGage * AlphaMax 
            float warningAlpha = (Mathf.Cos(Mathf.Lerp(Mathf.PI * 10, 0.0f, RunGage)) + 1) * 0.5f * RunGage * warningAlphaMax;
            warningCanvasGroup.alpha = Mathf.Lerp(warningCanvasGroup.alpha, warningAlpha, Time.deltaTime * 4);
        }
        #endregion
    }

    #region Action
    public void OnOverHitChanged(bool value) {
        warningCanvasGroup.gameObject.SetActive(value);

        if(value) {
            warningCanvasGroup.alpha = 0.0f;
        }
    }

    public void OnUseMicrophoneChanged(bool value) {
        micGageRect.gameObject.SetActive(value);
    }
    #endregion

    #region Utility
    public void SetHeadMessage(string key, float time) {
        if(headMessageFadeAnimationCoroutine != null) {
            StopCoroutine(headMessageFadeAnimationCoroutine);
        }

        headMessageFadeAnimationCoroutine = SetHeadMessageCoroutine(key, time);
        StartCoroutine(headMessageFadeAnimationCoroutine);
    }

    public void RemoveHeadMessage() {
        if(headMessageFadeAnimationCoroutine != null) {
            StopCoroutine(headMessageFadeAnimationCoroutine);

            headMessageText.text = "";
        }
    }

    private IEnumerator SetHeadMessageCoroutine(string key, float time) {
        HeadMessageKey = key;
        headMessageCanvasGroup.alpha = 0.0f;

        const float fadeTime = 0.3f;
        float fadeTimeChecker = 0.0f;
        while(fadeTimeChecker < fadeTime) {
            fadeTimeChecker += Time.deltaTime;

            headMessageCanvasGroup.alpha = fadeTimeChecker / fadeTime;

            yield return null;
        }

        yield return new WaitForSeconds(time - fadeTime * 2);

        fadeTimeChecker = 0.0f;
        while(fadeTimeChecker < fadeTime) {
            fadeTimeChecker += Time.deltaTime;

            headMessageCanvasGroup.alpha = 1.0f - fadeTimeChecker / fadeTime;

            yield return null;
        }

        headMessageFadeAnimationCoroutine = null;
    }

    public void SetItemCount(int maxCount, int collectCount) {
        itemCountText.text = $"({collectCount}/{maxCount})";
    }

    public void SetMessageBoxButton(Action action = null) {
        messageBoxButton.onClick.RemoveAllListeners();

        if(action != null) {
            messageBoxButton.onClick.AddListener(() => {
                SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

                action?.Invoke();
            });
        }
    }

    public void SetScenarioButtons(Action action1 = null, Action action2 = null) {
        scenarioButtons[0].onClick.RemoveAllListeners();
        scenarioButtons[1].onClick.RemoveAllListeners();

        if(action1 != null) {
            scenarioButtons[0].gameObject.SetActive(true);
            scenarioButtons[0].onClick.AddListener(() => {
                SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

                action1?.Invoke();
            });
        }
        else {
            scenarioButtons[0].gameObject.SetActive(false);
        }

        if(action2 != null) {
            scenarioButtons[1].gameObject.SetActive(true);
            scenarioButtons[1].onClick.AddListener(() => {
                SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

                action2?.Invoke();
            });
        }
        else {
            scenarioButtons[1].gameObject.SetActive(false);
        }
    }

    public void SetMessageButtons(string textKey1 = "", Action action1 = null, string textKey2 = "", Action action2 = null) {
        messageButtons[0].onClick.RemoveAllListeners();
        messageButtons[1].onClick.RemoveAllListeners();

        if(action1 != null) {
            messageButtons[0].gameObject.SetActive(true);
            messageButtons[0].onClick.AddListener(() => {
                SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

                action1?.Invoke();
            });
            messageButtons[0].GetComponentInChildren<LocalizeStringEvent>().StringReference.TableEntryReference = textKey1;
        }
        else {
            messageButtons[0].gameObject.SetActive(false);
        }

        if(action2 != null) {
            messageButtons[1].gameObject.SetActive(true);
            messageButtons[1].onClick.AddListener(() => {
                SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

                action2?.Invoke();
            });
            messageButtons[1].GetComponentInChildren<LocalizeStringEvent>().StringReference.TableEntryReference = textKey2;
        }
        else {
            messageButtons[1].gameObject.SetActive(false);
        }
    }

    public void SetLocalVariables(Dictionary<string, string>[] arguments) {
        if(arguments == null || arguments.Length <= 0) messageLocalizeEvent.StringReference.Arguments.Clear();
        else messageLocalizeEvent.StringReference.Arguments = arguments;
    }
    #endregion
}
