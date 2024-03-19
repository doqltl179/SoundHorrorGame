#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class MyCustomWindow : EditorWindow {
    private const string WindowsMenuName = MyCustomMenu.MenuName + "/Windows";

    private const string WindowName_MyCustomWindow = WindowsMenuName + "/My Custom Window";

    private int gameLevelClear;
    private int currentGameLevel;

    private Vector2Int captureSize;
    private bool changeCaptureColor;
    private Color targetColor;
    private Color changeColor;
    private string captureSavePath;



    [MenuItem(WindowName_MyCustomWindow)]
    public static void ShowWindow() {
        // 윈도우 인스턴스를 가져오거나 생성합니다.
        GetWindow(typeof(MyCustomWindow), false, "My Custom Window");
    }

    void OnGUI() {
        GUIStyle titleStyle = new GUIStyle() {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(20, 20, 12, 12),
            fixedHeight = 40,
            normal = new GUIStyleState() {
                textColor = Color.white,
            },
        };



        EditorGUILayout.LabelField("User Settings", titleStyle);

        GUILayout.Space(25);
        GUI.DrawTexture(EditorGUILayout.GetControlRect(false, 1), EditorGUIUtility.whiteTexture);
        GUILayout.Space(10);

        #region Set Game Level
        GUILayout.BeginHorizontal();
        gameLevelClear = EditorGUILayout.IntField("Set Game Level", gameLevelClear);
        GUILayout.Space(10);

        if(GUILayout.Button("Clear")) {
            UserSettings.SetGameLevelClear(gameLevelClear, true);

            Debug.Log($"GameLevel Clear Changed To Clear. GameLevel: {UserSettings.GameLevel}");
        }
        if(GUILayout.Button("Initialize")) {
            UserSettings.SetGameLevelClear(gameLevelClear, false);

            Debug.Log($"GameLevel Clear Changed To Not Clear. GameLevel: {UserSettings.GameLevel}");
        }
        GUILayout.EndHorizontal();
        #endregion

        #region Set Current Game Level
        GUILayout.BeginHorizontal();
        currentGameLevel = EditorGUILayout.IntField("Current Game Level", currentGameLevel);
        GUILayout.Space(10);

        if(GUILayout.Button("Apply")) {
            UserSettings.GameLevel = currentGameLevel;

            Debug.Log($"Current GameLevel Changed. GameLevel: {UserSettings.GameLevel}");
        }
        GUILayout.EndHorizontal();
        #endregion



        GUILayout.Space(30);
        EditorGUILayout.LabelField("Screen Capture", titleStyle);

        GUILayout.Space(25);
        GUI.DrawTexture(EditorGUILayout.GetControlRect(false, 1), EditorGUIUtility.whiteTexture);
        GUILayout.Space(10);

        #region Screen Capture
        captureSize = EditorGUILayout.Vector2IntField("Capture Size", captureSize);

        changeCaptureColor = GUILayout.Toggle(changeCaptureColor, "Change Color");
        if(changeCaptureColor) {
            targetColor = EditorGUILayout.ColorField("Target", targetColor);
            changeColor = EditorGUILayout.ColorField("Change To", changeColor);
        }

        GUILayout.BeginHorizontal();

        if(GUILayout.Button("Screen Capture")) {
            string path = EditorUtility.SaveFilePanel(
                "Save ScreenShot",
                string.IsNullOrEmpty(captureSavePath) ? Application.dataPath : captureSavePath,
                "ScreenShot" + ".png",
                "png");
            if(!string.IsNullOrEmpty(path)) {
                captureSavePath = path;
                //ScreenCapture.CaptureScreenshot(captureSavePath);
                Capture(captureSize, captureSavePath);

                Debug.Log($"ScreenShot saved. path: {captureSavePath}");
            }
            else {
                Debug.Log("ScreenShot path is NULL.");
            }
        }

        GUILayout.EndHorizontal();
        #endregion
    }

    private void Capture(Vector2Int captureSize, string path) {
        int width = captureSize.x;
        int height = captureSize.y;
        Debug.Log($"Capture Size: {width}x{height}");

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);

        Camera.main.targetTexture = rt;
        Camera.main.Render();
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        if(changeCaptureColor) {
            Color[] colors = tex.GetPixels();
            for(int i = 0; i < colors.Length; i++) {
                //if(IsSameColor(targetColor, colors[i])) {
                if(IsSameColor(targetColor, colors[i])) {
                    colors[i] = changeColor;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
        }

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }

    private bool IsSameColor(Color c1, Color c2) => c1.r == c2.r && c1.g == c2.g && c1.b == c2.b;
    private bool IsSameColorWithAlpha(Color c1, Color c2) => c1.r == c2.r && c1.g == c2.g && c1.b == c2.b && c1.a == c2.a;
}
#endif