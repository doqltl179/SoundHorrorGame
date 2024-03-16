using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorImageController : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    public CanvasGroup CanvasGroup { get { return canvasGroup; } }
    public float Alpha {
        get => canvasGroup.alpha;
        set => canvasGroup.alpha = value;
    }


    private void OnEnable() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable() {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
