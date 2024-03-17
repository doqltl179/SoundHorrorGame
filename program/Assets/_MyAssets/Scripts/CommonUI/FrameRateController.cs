using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FrameRateController : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    public CanvasGroup CanvasGroup { get { return canvasGroup; } }
    public float Alpha {
        get => canvasGroup.alpha;
        set => canvasGroup.alpha = value;
    }

    [SerializeField] private TextMeshProUGUI frameText;

    private float currentDeltaTime = 0.0f;
    private float fps, msec;



    private void Update() {
        // DeltaTime을 이용해 FPS 계산
        currentDeltaTime += (Time.unscaledDeltaTime - currentDeltaTime) * 0.1f;
        fps = 1.0f / currentDeltaTime;
        msec = currentDeltaTime * 1000.0f;

        // UI 텍스트 업데이트
        frameText.text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
    }
}
