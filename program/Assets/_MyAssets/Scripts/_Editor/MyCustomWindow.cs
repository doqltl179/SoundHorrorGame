#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MyCustomWindow : EditorWindow {
    private const string WindowsMenuName = MyCustomMenu.MenuName + "/Windows";

    private const string WindowName_MyCustomWindow = WindowsMenuName + "/My Custom Window";

    private int gameLevel;



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

        GUILayout.BeginHorizontal();
        gameLevel = EditorGUILayout.IntField("Game Level", gameLevel);
        GUILayout.Space(10);

        if(GUILayout.Button("Apply")) {
            UserSettings.GameLevel = gameLevel;

            Debug.Log($"GameLevel Changed. GameLevel: {UserSettings.GameLevel}");
        }

        GUILayout.EndHorizontal();
    }
}
#endif