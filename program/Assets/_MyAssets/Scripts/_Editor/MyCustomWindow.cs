#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MyCustomWindow : EditorWindow {
    private const string WindowsMenuName = MyCustomMenu.MenuName + "/Windows";

    private const string WindowName_MyCustomWindow = WindowsMenuName + "/My Custom Window";

    private int gameLevelClear;
    private int currentGameLevel;

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
        GUILayout.BeginHorizontal();

        if(GUILayout.Button("Screen Capture")) {
            captureSavePath = EditorUtility.SaveFilePanel(
                "Save ScreenShot",
                string.IsNullOrEmpty(captureSavePath) ? Application.dataPath : captureSavePath,
                "ScreenShot" + ".png",
                "png");
            if(!string.IsNullOrEmpty(captureSavePath)) {
                ScreenCapture.CaptureScreenshot(captureSavePath);

                Debug.Log($"ScreenShot saved. path: {captureSavePath}");
            }
            else {
                Debug.Log("ScreenShot path is NULL.");
            }
        }

        GUILayout.EndHorizontal();
        #endregion
    }
}
#endif