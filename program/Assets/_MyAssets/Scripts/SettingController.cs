using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingController : MonoBehaviour {
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Sound")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;

    [Header("Microphone")]
    [SerializeField] private Toggle useMicToggleYes;
    [SerializeField] private Toggle useMicToggleNo;
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

    struct DisplayModeKeyValue {
        public string Key;
        public FullScreenMode Value;
    }
    private readonly DisplayModeKeyValue[] DisplayModeOptions = new DisplayModeKeyValue[] {
        new DisplayModeKeyValue() { Key = "FullScreen", Value = FullScreenMode.ExclusiveFullScreen },
        new DisplayModeKeyValue() { Key = "Borderless", Value = FullScreenMode.FullScreenWindow },
        new DisplayModeKeyValue() { Key = "Window", Value = FullScreenMode.Windowed },
    };
    private readonly int[] FPSOptions = new int[] { 60, 75, 120, 144, 165, 240 };

    private string[] currentMicDeviceOptions = null;
    private Resolution[] currentDisplayResolutionOptions = null;
    private int[] currentDisplayFPSOptions = null;

    /// <summary>
    /// <br/>ScrollBar�� ������ 'Top to Bottom'���� �Ǿ������� �����δ� ���Ʒ��� �����Ǿ� �����ϹǷ�
    /// <br/>�˱� ���� ���� ���� ������ ����
    /// </summary>
    private float ScrollValue { 
        get => 1.0f - scrollView.verticalScrollbar.value;
        set => scrollView.verticalScrollbar.value = 1.0f - value;
    }



    private void OnEnable() {
        InitMasterVolume();

        InitUseMicToggles();
        InitMicDeviceOptions();

        InitDisplayModeOptions();
        InitDisplayResolutionOptions();
        InitDisplayFPSOptions();
        InitDisplayFOV();
        InitDisplayBrightness();
        InitDisplaySensitive();

        StartCoroutine(DelayOpen());
    }

    private IEnumerator DelayOpen() {
        yield return null;

        ScrollValue = 0.0f;
    }

    #region Action
    public void OnMasterVolumeDrag(BaseEventData data) {
        float volume = UserSettings.CalculateMasterVolume(masterVolumeSlider.value);
        masterVolumeText.text = string.Format("{0:0.0}", volume);
    }

    public void OnMasterVolumeDragEnd(BaseEventData data) {
        float volume = UserSettings.CalculateMasterVolume(masterVolumeSlider.value);
        if(volume != UserSettings.MasterVolume) {
            UserSettings.MasterVolume = volume;
        }
    }

    public void OnUseMicYesChanged(bool changedValue) {
        useMicToggleNo.SetIsOnWithoutNotify(!changedValue);
        UserSettings.UseMic = changedValue ? 1 : 0;
    }

    public void OnUseMicNoChanged(bool changedValue) {
        useMicToggleYes.SetIsOnWithoutNotify(!changedValue);
        UserSettings.UseMic = changedValue ? 1 : 0;
    }

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

    public void OnDisplayModeChanged(int index) {
        FullScreenMode changedMode = DisplayModeOptions[index].Value;
        if(UserSettings.DisplayMode != changedMode) {
            UserSettings.DisplayMode = changedMode;
        }
    }

    public void OnDisplayResolutionChanged(int index) {
        Resolution changedResolution = currentDisplayResolutionOptions[index];
        if(UserSettings.DisplayResolution.width !=  changedResolution.width || UserSettings.DisplayResolution.height != changedResolution.height) {
            Screen.SetResolution(changedResolution.width, changedResolution.height, UserSettings.DisplayMode);
        }
    }

    public void OnDisplayFPSChanged(int index) {
        int changedFPS = currentDisplayFPSOptions[index];
        if(UserSettings.FPS != changedFPS) {
            Application.targetFrameRate = changedFPS;
            UserSettings.FPS = changedFPS;

            //QualitySettings.vSyncCount = 0;
        }
    }

    public void OnDisplayFOVDrag(BaseEventData data) {
        displayFOVText.text = string.Format("{0:0.0}", UserSettings.CalculateFOV(displayFOVSlider.value));
    }

    public void OnDisplayFOVDragEnd(BaseEventData data) {
        float fov = UserSettings.CalculateFOV(displayFOVSlider.value);
        if(fov != UserSettings.FOV) {
            UserSettings.FOV = fov;
        }
    }

    public void OnDisplayBrightnessDrag(BaseEventData data) {
        displayBrightnessText.text = string.Format("{0:0.0}", UserSettings.CalculateBrightness(displayBrightnessSlider.value));
    }

    public void OnDisplayBrightnessDragEnd(BaseEventData data) {
        float brightness = UserSettings.CalculateBrightness(displayBrightnessSlider.value);
        if(UserSettings.DisplayBrightness != brightness) {
            UserSettings.DisplayBrightness = brightness;
        }
    }

    public void OnDisplaySensitiveDrag(BaseEventData data) {
        displaySensitiveText.text = string.Format("{0:0.0}", UserSettings.CalculateSensitive(displaySensitiveSlider.value));
    }

    public void OnDisplaySensitiveDragEnd(BaseEventData data) {
        float sensitive = UserSettings.CalculateSensitive(displaySensitiveSlider.value);
        if(UserSettings.DisplaySensitive != sensitive) {
            UserSettings.DisplaySensitive = sensitive;
        }
    }
    #endregion

    private void InitMasterVolume() {
        masterVolumeSlider.value = UserSettings.MasterVolumeRatio;
        masterVolumeText.text = string.Format("{0:0.0}", UserSettings.MasterVolume);
    }

    private void InitUseMicToggles() {
        useMicToggleYes.isOn = UserSettings.UseMic == 1 ? true : false;
        useMicToggleNo.isOn = !useMicToggleYes.isOn;
    }

    private void InitMicDeviceOptions() {
        micDeviceDropdown.ClearOptions();
        string currentDevice = "";
        int currentIndex = 0;

        List<TMP_Dropdown.OptionData> micList = new List<TMP_Dropdown.OptionData>();
        if(Microphone.devices.Length > 0) {
            currentMicDeviceOptions = new string[Microphone.devices.Length];
            // Microphone.devices�� �ҷ����� ���� ������� �⺻��ġ�� index 0�� �ڸ��� �´�.
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
        // Dropdown�� value�� �⺻������ 0�̱� ������ 0�� ����
        if(currentIndex > 0) micDeviceDropdown.value = currentIndex;
    }

    private void InitDisplayModeOptions() {
        displayModeDropdown.ClearOptions();
        int currentIndex = Array.FindIndex(DisplayModeOptions, t => t.Value == UserSettings.DisplayMode);
        if(currentIndex < 0) {
            Screen.fullScreenMode = DisplayModeOptions[0].Value;
            currentIndex = 0;
        }

        List<TMP_Dropdown.OptionData> displayModeList = new List<TMP_Dropdown.OptionData>();
        foreach(DisplayModeKeyValue option in DisplayModeOptions) {
            displayModeList.Add(new TMP_Dropdown.OptionData(option.Key));
        }
        displayModeDropdown.AddOptions(displayModeList);
        // Dropdown�� value�� �⺻������ 0�̱� ������ 0�� ����
        if(currentIndex > 0) displayModeDropdown.value = currentIndex;
    }

    private void InitDisplayResolutionOptions() {
        displayResolutionDropdown.ClearOptions();

        currentDisplayResolutionOptions = new Resolution[Screen.resolutions.Length];
        Array.Copy(Screen.resolutions, 0, currentDisplayResolutionOptions, 0, currentDisplayResolutionOptions.Length);
        // Screen.resolution�� ȭ���� ���� ������ ���ĵǾ� �����Ƿ�
        // �ɼǿ����� ȭ���� ū ������� ���̰� �ϱ� ���� Reverse�� ���ش�.
        Array.Reverse(currentDisplayResolutionOptions); 

        Resolution currentResolution = UserSettings.DisplayResolution;
        int currentResolutionIndex = Array.FindIndex(currentDisplayResolutionOptions,
            t => t.width == currentResolution.width && t.height == currentResolution.height);
        if(currentResolutionIndex < 0) {
            Debug.LogWarning("Display Resolution Options are broken.");

            return;
        }

        List<TMP_Dropdown.OptionData> displayResolutionList = new List<TMP_Dropdown.OptionData>();
        foreach(Resolution r in currentDisplayResolutionOptions) {
            displayResolutionList.Add(new TMP_Dropdown.OptionData($"{r.width}x{r.height}"));
        }
        displayResolutionDropdown.AddOptions(displayResolutionList);
        if(currentResolutionIndex > 0) displayResolutionDropdown.value = currentResolutionIndex;
    }

    private void InitDisplayFPSOptions() {
        displayFPSDropdown.ClearOptions();
        int optionIndex = Array.FindIndex(FPSOptions, t => t == UserSettings.FPS);
        if(optionIndex < 0) {
            int maxFPS = FPSOptions.Where(t => t <= UserSettings.FPS).Max();
            UserSettings.FPS = maxFPS;

            optionIndex = Array.FindIndex(FPSOptions, t => t == maxFPS);
        }
        int currentFPS = FPSOptions[optionIndex];

        List<TMP_Dropdown.OptionData> displayFPSList = new List<TMP_Dropdown.OptionData>();
        currentDisplayFPSOptions = FPSOptions.Where(t => t <= UserSettings.DisplayResolution.refreshRateRatio.value).ToArray();
        Array.Reverse(currentDisplayFPSOptions);
        foreach(int fps in currentDisplayFPSOptions) {
            displayFPSList.Add(new TMP_Dropdown.OptionData($"{fps}"));
        }
        displayFPSDropdown.AddOptions(displayFPSList);

        int currentIndex = Array.FindIndex(currentDisplayFPSOptions, t => t == currentFPS);
        if(currentIndex > 0) displayFPSDropdown.value = currentIndex;
    }

    private void InitDisplayFOV() {
        displayFOVSlider.value = UserSettings.FOVRatio;
        displayFOVText.text = string.Format("{0:0.0}", UserSettings.FOV);
    }

    private void InitDisplayBrightness() {
        displayBrightnessSlider.value = UserSettings.DisplayBrightnessRatio;
        displayBrightnessText.text = string.Format("{0:0.0}", UserSettings.DisplayBrightness);
    }

    private void InitDisplaySensitive() {
        displaySensitiveSlider.value = UserSettings.DisplaySensitiveRatio;
        displaySensitiveText.text = string.Format("{0:0.0}", UserSettings.DisplaySensitive);
    }
}
