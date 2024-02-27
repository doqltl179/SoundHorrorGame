using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MicrophoneRecorder : GenericSingleton<MicrophoneRecorder> {
    private const int CaptureLength = 1;

    private const float outputInterval = 0.5f;
    private float outputIntervalChecker = 0.0f;

    //private int currentFreq = 44100;
    private int currentFreq = 256;
    private string currentDevice = "";
    private AudioClip input = null;

    private const float DecibelMin = -50f;
    private const float DecibelMax = 10f;
    private const float DecibelCritical = -20f;
    private float decibel;
    public float Decibel { get { return decibel; } }
    public float DecibelRatio { get { return Mathf.InverseLerp(DecibelMin, DecibelMax, decibel); } }
    public readonly float DecibelCriticalRatio = Mathf.InverseLerp(DecibelMin, DecibelMax, DecibelCritical);
    /// <summary>
    /// Decibel 값이 임계점을 넘었는지 확인하는 변수
    /// </summary>
    public bool OverCritical { get { return decibel >= DecibelCritical; } }



    protected override void Awake() {
        base.Awake();

        UserSettings.OnMicDeviceChanged += OnMicrophoneDeviceChanged;
        UserSettings.OnUseMicChanged += OnUseMicChanged;
    }

    private void OnDestroy() {
        UserSettings.OnMicDeviceChanged -= OnMicrophoneDeviceChanged;
        UserSettings.OnUseMicChanged -= OnUseMicChanged;
    }

    private void OnEnable() {
        //int minFreq;
        //int maxFreq;
        //Microphone.GetDeviceCaps(UserSettings.MicDevice, out minFreq, out maxFreq);
        //if(currentFreq < minFreq || maxFreq < minFreq) {
        //    currentFreq = minFreq;
        //}

        currentDevice = UserSettings.MicDevice;
        input = Microphone.Start(currentDevice, true, CaptureLength, currentFreq);
    }

    private void OnDisable() {
        if(input != null) {
            Microphone.End(UserSettings.MicDevice);

            decibel = DecibelMin;
            currentDevice = "";
            input = null;
        }
    }

    float[] samples;
    float rmsValue;
    private void Update() {
        if(!UserSettings.UseMicBoolean) {
            if(Microphone.IsRecording(currentDevice)) {
                Microphone.End(currentDevice);

                decibel = DecibelMin;
                currentDevice = "";
                input = null;
            }
        }

        if(!Microphone.IsRecording(UserSettings.MicDevice)) {
            if(currentDevice != UserSettings.MicDevice) {
                Microphone.End(currentDevice);
            }

            currentDevice = UserSettings.MicDevice;
            input = Microphone.Start(currentDevice, true, CaptureLength, currentFreq);
        }

        //samples = new float[input.samples * input.channels];
        samples = new float[currentFreq * input.channels];
        if(input.GetData(samples, 0)) {
            rmsValue = CalculateRMSValue(samples);
            decibel = 20 * Mathf.Log10(rmsValue / 0.1f); // 0.1f는 임의의 참조 값
            //Debug.Log(decibel);
        }
    }

    private float CalculateRMSValue(float[] samples) {
        const int startIndex = 3;
        float[] newSamples = new float[samples.Length - startIndex - (samples.Length / 2)];
        Array.Copy(samples, startIndex, newSamples, 0, newSamples.Length);

        // 1단계: 각 샘플 값을 제곱하여 더함
        float sum = newSamples.Sum(t => Mathf.Pow(t, 2));

        // 2단계: 평균 구하기
        float average = sum / newSamples.Length;

        // 3단계: 제곱근 구하기
        return Mathf.Sqrt(average);
    }

    #region Action
    private void OnMicrophoneDeviceChanged(string device) {
        if(currentDevice != device && Microphone.IsRecording(currentDevice)) {
            Microphone.End(currentDevice);
        }

        if(UserSettings.UseMicBoolean) {
            currentDevice = device;
            input = Microphone.Start(currentDevice, true, CaptureLength, currentFreq);
        }
    }

    private void OnUseMicChanged(bool value) {
        gameObject.SetActive(value);
    }
    #endregion
}
