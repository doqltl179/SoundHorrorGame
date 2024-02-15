using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UtilObjects : ResourceGenericSingleton<UtilObjects> {
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

    public Vector3 CamPos => Cam.transform.position;

    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }
}
