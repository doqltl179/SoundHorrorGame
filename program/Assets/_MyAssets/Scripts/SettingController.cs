using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour {
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Sound")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;

    [Header("Microphone")]
    [SerializeField] private TMP_Dropdown micDeviceDropdown;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private TMP_Dropdown displayResolutionDropdown;
    [SerializeField] private TMP_Dropdown displayFPSDropdown;
    [SerializeField] private Slider displayFOVSlider;
    [SerializeField] private TextMeshProUGUI displayFOVText;
    [SerializeField] private Slider displayBrightnessSlider;
    [SerializeField] private TextMeshProUGUI displayBrightnessText;
    [SerializeField] private Slider displaySensitiveSlider;
    [SerializeField] private TextMeshProUGUI displaySensitiveText;

    private readonly string[] DisplayModeOptions = new string[] { "FullScreen", "Borderless", "Window" };
    private readonly int[] FPSOptions = new int[] { 60, 75, 120, 144, 165, 240 };

    private string[] currentMicDeviceOptions = null;

    /// <summary>
    /// <br/>ScrollBar의 설정이 'Top to Bottom'으로 되어있지만 실제로는 위아래가 반전되어 동작하므로
    /// <br/>알기 쉽게 보기 위한 변수를 선언
    /// </summary>
    private float ScrollValue { 
        get => 1.0f - scrollView.verticalScrollbar.value;
        set => scrollView.verticalScrollbar.value = 1.0f - value;
    }



    private void Awake() {
        micDeviceDropdown.onValueChanged.AddListener(OnMicDeviceChanged);
    }

    private void OnDestroy() {
        micDeviceDropdown.onValueChanged.RemoveListener(OnMicDeviceChanged);
    }

    private void OnEnable() {
        UserSettingsHelper currentSettings = new UserSettingsHelper();

        masterVolumeSlider.value = currentSettings.MasterVolumeRatio;
        masterVolumeText.text = string.Format("{0:0.0}", currentSettings.MasterVolume);

        InitMicDeviceOptions();

        displayModeDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> displayModeList = new List<TMP_Dropdown.OptionData>();
        foreach(string mode in DisplayModeOptions) {
            displayModeList.Add(new TMP_Dropdown.OptionData(mode));
        }
        displayModeDropdown.AddOptions(displayModeList);

        displayResolutionDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> displayResolutionList = new List<TMP_Dropdown.OptionData>();
        Resolution currentResolution = Screen.currentResolution;
        Resolution[] availableResolutions = Screen.resolutions.Where(t =>
            t.width <= currentResolution.width && t.height <= currentResolution.height).ToArray();
        Array.Reverse(availableResolutions);
        foreach(Resolution r in availableResolutions) {
            displayResolutionList.Add(new TMP_Dropdown.OptionData($"{r.width}x{r.height}"));
        }
        displayResolutionDropdown.AddOptions(displayResolutionList);
        displayResolutionDropdown.value = Array.FindIndex(availableResolutions, t =>
            t.width == Screen.currentResolution.width && t.height == Screen.currentResolution.height);

        displayFPSDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> displayFPSList = new List<TMP_Dropdown.OptionData>();
        int currentFPS = (int)Screen.currentResolution.refreshRateRatio.value;
        int[] availableFPS = FPSOptions.Where(t => t <= currentFPS).ToArray();
        Array.Reverse(availableFPS);
        foreach(int fps in availableFPS) {
            displayFPSList.Add(new TMP_Dropdown.OptionData($"{fps}"));
        }
        displayFPSDropdown.AddOptions(displayFPSList);
        displayFPSDropdown.value = Array.FindIndex(availableFPS, t => t == currentFPS);

        displayFOVSlider.value = currentSettings.FOVRatio;
        displayBrightnessSlider.value = currentSettings.DisplayBrightnessRatio;
        displaySensitiveSlider.value = currentSettings.DisplaySensitiveRatio;

        StartCoroutine(DelayOpen());
    }

    private IEnumerator DelayOpen() {
        yield return null;

        ScrollValue = 0.0f;
    }

    #region Action
    public void OnMicDeviceChanged(int index) {
        string changeDevice = currentMicDeviceOptions[index];
        int deviceIndex = Array.FindIndex(Microphone.devices, t => t == changeDevice);
        if(deviceIndex >= 0) {
            UserSettings.MicDevice = Microphone.devices[deviceIndex];
            if(Microphone.devices.Length != currentMicDeviceOptions.Length) {
                InitMicDeviceOptions();
            }
        }
        else {
            InitMicDeviceOptions();
        }
    }
    #endregion

    private void InitMicDeviceOptions() {
        micDeviceDropdown.ClearOptions();
        string currentDevice = "";
        int currentIndex = 0;

        List<TMP_Dropdown.OptionData> micList = new List<TMP_Dropdown.OptionData>();
        if(Microphone.devices.Length > 0) {
            currentMicDeviceOptions = new string[Microphone.devices.Length];
            // Microphone.devices를 불러오면 현재 사용중인 기본장치가 index 0의 자리로 온다.
            Array.Copy(Microphone.devices, 0, currentMicDeviceOptions, 0, currentMicDeviceOptions.Length);

            currentIndex = Array.FindIndex(currentMicDeviceOptions, t => t == UserSettings.MicDevice);
            if(currentIndex < 0) {
                currentDevice = currentMicDeviceOptions[0];
                UserSettings.MicDevice = currentDevice;
            }
            else {
                currentDevice = UserSettings.MicDevice;
            }

            foreach(string d in currentMicDeviceOptions) {
                micList.Add(new TMP_Dropdown.OptionData(d));
            }
        }
        micDeviceDropdown.AddOptions(micList);
        // Dropdown의 value가 기본적으로 0이기 때문에 0은 제외
        if(currentIndex > 0) micDeviceDropdown.value = currentIndex;
    }
}
