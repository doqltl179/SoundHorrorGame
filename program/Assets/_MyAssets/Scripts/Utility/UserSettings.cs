using System;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class UserSettings {
    #region Program
    private static readonly string m_isFirstStart_pref = "IsFirstStart";
    public static bool IsFirstStart {
        get => !PlayerPrefs.HasKey(m_isFirstStart_pref) || PlayerPrefs.GetInt(m_isFirstStart_pref) == 1;
        set => PlayerPrefs.SetInt(m_isFirstStart_pref, value ? 1 : 0);
    }
    #endregion

    #region IsCleared
    private static readonly string m_levelClear_pref = "LevelClear";
    public static void SetGameLevelClear(int gameLevel, bool isClear) {
        string clear_pref = m_levelClear_pref + gameLevel.ToString().PadLeft(2, '0');
        PlayerPrefs.SetInt(clear_pref, isClear ? 1 : 0);
    }
    public static bool GetGameLevelClearBoolean(int gameLevel) {
        string clear_pref = m_levelClear_pref + gameLevel.ToString().PadLeft(2, '0');
        return PlayerPrefs.HasKey(clear_pref) && PlayerPrefs.GetInt(clear_pref) == 1;
    }
    public static int GetClearLevelMax() {
        int gameLevel = 0;
        string clear_pref;
        while(true) {
            clear_pref = m_levelClear_pref + gameLevel.ToString().PadLeft(2, '0');
            if(!PlayerPrefs.HasKey(clear_pref) || PlayerPrefs.GetInt(clear_pref) != 1) {
                gameLevel--;

                break;
            }

            gameLevel++;
        }

        return gameLevel;
    }
    public static void SetEndingClear(bool isBadEnding) {
        string clear_pref = m_levelClear_pref + (isBadEnding ? "BadEnding" : "HappyEnding");
        PlayerPrefs.SetInt(clear_pref, 1);
    }
    public static bool GetEndingClearBoolean(bool isBadEnding) {
        string clear_pref = m_levelClear_pref + (isBadEnding ? "BadEnding" : "HappyEnding");
        return PlayerPrefs.HasKey(clear_pref) && PlayerPrefs.GetInt(clear_pref) == 1;
    }
    #endregion

    #region GameLevel
    private static readonly string m_gameLevel_pref = "GameLevel";
    private static readonly int m_standardGameLevel = 0;
    public static int GameLevel {
        get => PlayerPrefs.HasKey(m_gameLevel_pref) ? PlayerPrefs.GetInt(m_gameLevel_pref) : m_standardGameLevel;
        set => PlayerPrefs.SetInt(m_gameLevel_pref, value);
    }
    #endregion

    #region Tutorial
    private static readonly string m_isFirstPlay_pref = "IsFistPlay";
    private static readonly int m_standardIsFirstPlay = 1;
    public static int IsFirstPlay {
        get => PlayerPrefs.HasKey(m_isFirstPlay_pref) ? PlayerPrefs.GetInt(m_isFirstPlay_pref) : m_standardIsFirstPlay;
        set => PlayerPrefs.SetInt(m_isFirstPlay_pref, value);
    }
    public static bool IsFirstPlayBoolean {
        get => IsFirstPlay == 1 ? true : false;
        set {
            if(value) IsFirstPlay = 1;
            else IsFirstPlay = 0;
        }
    }
    #endregion

    #region Language
    public static Action<string> OnLocaleChanged;
    private static readonly string m_languageCode_pref = "LanguageCode";
    private static readonly string m_standardLanguageCode = "en";
    public static string LanguageCode {
        get => PlayerPrefs.HasKey(m_languageCode_pref) ? PlayerPrefs.GetString(m_languageCode_pref) : m_standardLanguageCode;
        set {
            PlayerPrefs.SetString(m_languageCode_pref, value);
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(value);
            OnLocaleChanged?.Invoke(value);
        }
    }
    #endregion

    #region Sound
    public static Action<float> OnMasterVolumeChanged;
    private static readonly string m_masterVolume_pref = "MasterVolume";
    private static readonly float m_standardMasterVolume = 50.0f;
    private static readonly float m_masterVolumeMin = 0.0f;
    private static readonly float m_masterVolumeMax = 100.0f;
    public static float MasterVolume {
        get => PlayerPrefs.HasKey(m_masterVolume_pref) ? PlayerPrefs.GetFloat(m_masterVolume_pref) : m_standardMasterVolume;
        set {
            PlayerPrefs.SetFloat(m_masterVolume_pref, Mathf.Clamp(value, m_masterVolumeMin, m_masterVolumeMax));
            OnMasterVolumeChanged?.Invoke(value);
        }
    }
    public static float MasterVolumeRatio {
        get => Mathf.InverseLerp(m_masterVolumeMin, m_masterVolumeMax, MasterVolume);
        set => MasterVolume = Mathf.Lerp(m_masterVolumeMin, m_masterVolumeMax, value);
    }
    #endregion

    #region Microphone
    public static Action<bool> OnUseMicChanged;
    private static readonly string m_useMic_pref = "UseMic";
    private static readonly int m_standardUseMic = 1;
    public static int UseMic {
        get => PlayerPrefs.HasKey(m_useMic_pref) ? PlayerPrefs.GetInt(m_useMic_pref) : m_standardUseMic;
        set {
            PlayerPrefs.SetInt(m_useMic_pref, value);
            OnUseMicChanged?.Invoke(value == 1);
        }
    }
    public static bool UseMicBoolean {
        get => UseMic == 1;
        set => UseMic = value ? 1 : 0;
    }

    public static Action<string> OnMicDeviceChanged;
    private static readonly string m_micDevice_pref = "MicDevice";
    private static readonly string m_standardMicDevice = "";
    public static string MicDevice {
        get => PlayerPrefs.HasKey(m_micDevice_pref) ? PlayerPrefs.GetString(m_micDevice_pref) : m_standardMicDevice;
        set {
            PlayerPrefs.SetString(m_micDevice_pref, value);
            OnMicDeviceChanged?.Invoke(value);
        }
    }

    public static Action<float> OnMicSensitiveChanged;
    private static readonly string m_micSensitive_pref = "MicSensitive";
    private static readonly float m_standardMicSensitive = 0.65f;
    public static float MicSensitive {
        get => PlayerPrefs.HasKey(m_micSensitive_pref) ? PlayerPrefs.GetFloat(m_micSensitive_pref) : m_standardMicSensitive;
        set {
            PlayerPrefs.SetFloat(m_micSensitive_pref, value);
            OnMicSensitiveChanged?.Invoke(value);
        }
    }
    #endregion

    #region Display
    public static FullScreenMode DisplayMode {
        get => Screen.fullScreenMode;
        set => Screen.fullScreenMode = value;
    }
    public static Resolution DisplayResolution { get { return Screen.currentResolution; } }

    public static Action<int> VSyncChanged;
    private static readonly string m_vSync_pref = "VSync";
    private static readonly int m_standardVSync = 1;
    public static int VSync {
        get => PlayerPrefs.HasKey(m_vSync_pref) ? PlayerPrefs.GetInt(m_vSync_pref) : m_standardVSync;
        set {
            PlayerPrefs.SetInt(m_vSync_pref, value);
            VSyncChanged?.Invoke(value);
        }
    }

    public static Action<int> OnFPSChanged;
    private static readonly string m_displayFPS_pref = "DisplayFPS";
    //private static readonly float m_standardDisplayFPS = 60.0f;
    public static int FPS {
        get => PlayerPrefs.HasKey(m_displayFPS_pref) ? PlayerPrefs.GetInt(m_displayFPS_pref) : (int)DisplayResolution.refreshRateRatio.value;
        set {
            PlayerPrefs.SetInt(m_displayFPS_pref, value);
            OnFPSChanged?.Invoke(value);
        }
    }

    public static Action<float> OnDisplayFOVChanged;
    private static readonly string m_displayFOV_pref = "DisplayFOV";
    private static readonly float m_standardDisplayFOV = 60.0f;
    private static readonly float m_displayFOVMin = 45.0f;
    private static readonly float m_displayFOVMax = 90.0f;
    public static float FOV {
        get => PlayerPrefs.HasKey(m_displayFOV_pref) ? PlayerPrefs.GetFloat(m_displayFOV_pref) : m_standardDisplayFOV;
        set {
            PlayerPrefs.SetFloat(m_displayFOV_pref, value);
            OnDisplayFOVChanged?.Invoke(value);
        }
    }
    public static float FOVRatio {
        get => Mathf.InverseLerp(m_displayFOVMin, m_displayFOVMax, FOV);
        set => FOV = Mathf.Lerp(m_displayFOVMin, m_displayFOVMax, value);
    }

    public static Action<float> OnDisplayBrightnessChanged;
    private static readonly string m_displayBrightness_pref = "DisplayBrightness";
    private static readonly float m_standardDisplayBrightness = 1.0f;
    private static readonly float m_displayBrightnessMin = 0.1f;
    private static readonly float m_displayBrightnessMax = 3.0f;
    public static float DisplayBrightness {
        get => PlayerPrefs.HasKey(m_displayBrightness_pref) ? PlayerPrefs.GetFloat(m_displayBrightness_pref) : m_standardDisplayBrightness;
        set {
            PlayerPrefs.SetFloat(m_displayBrightness_pref, Mathf.Clamp(value, m_displayBrightnessMin, m_displayBrightnessMax));
            OnDisplayBrightnessChanged?.Invoke(value);
        }
    }
    public static float DisplayBrightnessRatio {
        get => Mathf.InverseLerp(m_displayBrightnessMin, m_displayBrightnessMax, DisplayBrightness);
        set => DisplayBrightness = Mathf.Lerp(m_displayBrightnessMin, m_displayBrightnessMax, value);
    }

    public static Action<float> OnDisplaySensitiveChanged;
    private static readonly string m_displaySensitive_pref = "DisplaySensitive";
    private static readonly float m_standardDisplaySensitive = 1.0f;
    private static readonly float m_displaySensitiveMin = 0.01f;
    private static readonly float m_displaySensitiveMax = 5.0f;
    public static float DisplaySensitive {
        get => PlayerPrefs.HasKey(m_displaySensitive_pref) ? PlayerPrefs.GetFloat(m_displaySensitive_pref) : m_standardDisplaySensitive;
        set {
            PlayerPrefs.SetFloat(m_displaySensitive_pref, Mathf.Clamp(value, m_displaySensitiveMin, m_displaySensitiveMax));
            OnDisplaySensitiveChanged?.Invoke(value);
        }
    }
    public static float DisplaySensitiveRatio {
        get => Mathf.InverseLerp(m_displaySensitiveMin, m_displaySensitiveMax, DisplaySensitive);
        set => DisplaySensitive = Mathf.Lerp(m_displaySensitiveMin, m_displaySensitiveMax, value);
    }
    #endregion

    #region Utility
    public static float CalculateMasterVolume(float ratio) => Mathf.Lerp(m_masterVolumeMin, m_masterVolumeMax, ratio);
    public static float CalculateMasterVolumeRatio(float volume) => Mathf.InverseLerp(m_masterVolumeMin, m_masterVolumeMax, volume);
    public static float CalculateFOV(float ratio) => Mathf.Lerp(m_displayFOVMin, m_displayFOVMax, ratio);
    public static float CalculateFOVRatio(float fov) => Mathf.InverseLerp(m_displayFOVMin, m_displayFOVMax, fov);
    public static float CalculateBrightness(float ratio) => Mathf.Lerp(m_displayBrightnessMin, m_displayBrightnessMax, ratio);
    public static float CalculateBrightnessRatio(float brightness) => Mathf.InverseLerp(m_displayBrightnessMin, m_displayBrightnessMax, brightness);
    public static float CalculateSensitive(float ratio) => Mathf.Lerp(m_displaySensitiveMin, m_displaySensitiveMax, ratio);
    public static float CalculateSensitiveRatio(float sensitive) => Mathf.InverseLerp(m_displaySensitiveMin, m_displaySensitiveMax, sensitive);
    #endregion
}

//public class UserSettingsHelper {
//    #region Sound
//    public float MasterVolume;
//    public float MasterVolumeRatio {
//        get => UserSettings.CalculateMasterVolumeRatio(MasterVolume);
//        set => MasterVolume = UserSettings.CalculateMasterVolume(value);
//    }
//    #endregion

//    #region Microphone
//    public int UseMic;
//    public string MicDevice;
//    #endregion

//    #region Display
//    public FullScreenMode DisplayMode;
//    public Resolution DisplayResolution;
//    public float FPS;
//    public float FOV;
//    public float FOVRatio {
//        get => UserSettings.CalculateFOVRatio(FOV);
//        set => FOV = UserSettings.CalculateFOV(value);
//    }
//    public float DisplayBrightness;
//    public float DisplayBrightnessRatio {
//        get => UserSettings.CalculateBrightnessRatio(DisplayBrightness);
//        set => DisplayBrightness = UserSettings.CalculateBrightness(value);
//    }
//    public float DisplaySensitive;
//    public float DisplaySensitiveRatio {
//        get => UserSettings.CalculateSensitiveRatio(DisplayBrightness);
//        set => DisplaySensitive = UserSettings.CalculateSensitive(value);
//    }
//    #endregion



//    public UserSettingsHelper(UserSettingsHelper copyObject = null) {
//        if(copyObject == null) {
//            MasterVolume = UserSettings.MasterVolume;
//            MasterVolumeRatio = UserSettings.MasterVolumeRatio;

//            UseMic = UserSettings.UseMic;
//            MicDevice = UserSettings.MicDevice;

//            FPS = UserSettings.FPS;
//            FOV = UserSettings.FOV;
//            FOVRatio = UserSettings.FOVRatio;
//            DisplayBrightness = UserSettings.DisplayBrightness;
//            DisplayBrightnessRatio = UserSettings.DisplayBrightnessRatio;
//            DisplaySensitive = UserSettings.DisplaySensitive;
//            DisplaySensitiveRatio = UserSettings.DisplaySensitiveRatio;
//        }
//        else {
//            MasterVolume = copyObject.MasterVolume;
//            MasterVolumeRatio = copyObject.MasterVolumeRatio;

//            UseMic = copyObject.UseMic;
//            MicDevice = copyObject.MicDevice;

//            FPS = copyObject.FPS;
//            FOV = copyObject.FOV;
//            FOVRatio = copyObject.FOVRatio;
//            DisplayBrightness = copyObject.DisplayBrightness;
//            DisplayBrightnessRatio = copyObject.DisplayBrightnessRatio;
//            DisplaySensitive = copyObject.DisplaySensitive;
//            DisplaySensitiveRatio = copyObject.DisplaySensitiveRatio;
//        }
//    }
//}
