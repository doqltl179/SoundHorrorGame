using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [Header("Item")]
    [SerializeField] private TextMeshProUGUI itemCountText;

    [Header("Run")]
    [SerializeField] private CanvasGroup runGageCanvasGroup;
    [SerializeField] private Image[] runGageImages;
    [SerializeField] private Color runGageStartColor;
    [SerializeField] private Color runGageEndColor;
    [SerializeField, Range(0.0f, 1.0f)] private float runGageMaxAlpha = 1.0f;
    public float RunGage {
        get => runGageImages[0].fillAmount;
        set {
            foreach(Image image in runGageImages) {
                image.fillAmount = value;
            }
        }
    }

    [Header("Warning")]
    [SerializeField] private CanvasGroup warningCanvasGroup;



    private void Awake() {
        LevelLoader.Instance.OnItemCollected += OnitemCollected;
    }

    private void OnDestroy() {
        LevelLoader.Instance.OnItemCollected -= OnitemCollected;
    }

    private void Start() {
        micGageSlider.value = 0.0f;
        RunGage = 0.0f;

        itemCountText.text = "x" + LevelLoader.Instance.CollectedItemCount;

        runGageCanvasGroup.alpha = 0.0f;
        warningCanvasGroup.alpha = 0.0f;

        micGageSliderLimitLine.anchoredPosition =
            Vector2.up *
            (micGageRect.sizeDelta.y + micGageRectHeightOffset) *
            MicrophoneRecorder.Instance.DecibelCriticalRatio;
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
        #endregion
    }

    #region Action
    private void OnitemCollected() {

    }
    #endregion
}
