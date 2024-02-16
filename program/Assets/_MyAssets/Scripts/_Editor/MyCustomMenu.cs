#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MyCustomMenu : MonoBehaviour
{
    private const string MenuName = "My Custom Menu";

    private const string SymbolsMenuName = MenuName + "/Symbols";

    private const string SymbolsMenuItemName_UseMazeBlockMaterial = SymbolsMenuName + "/Use MazeBlock Material";

    private static string def_UNITY_POST_PROCESSING_STACK_V2 = "UNITY_POST_PROCESSING_STACK_V2";
    private static string def_Use_MazeBlock_Material = "Use_MazeBlock_Material";

    private static List<string> symbolList = new List<string>() {
        def_UNITY_POST_PROCESSING_STACK_V2
    };

    private static bool useMazeBlockMaterial = true;



    private void Awake() {
        if(useMazeBlockMaterial) {
            symbolList.Add(def_Use_MazeBlock_Material);
            Menu.SetChecked(SymbolsMenuItemName_UseMazeBlockMaterial, useMazeBlockMaterial);
        }
    }

    [MenuItem(SymbolsMenuItemName_UseMazeBlockMaterial)]
    private static void UseMazeBlockMaterial() {
        useMazeBlockMaterial = !useMazeBlockMaterial;
        if(useMazeBlockMaterial) symbolList.Add(def_Use_MazeBlock_Material);
        else symbolList.Remove(def_Use_MazeBlock_Material);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbolList.ToArray());

        Menu.SetChecked(SymbolsMenuItemName_UseMazeBlockMaterial, useMazeBlockMaterial);
    }
}
#endif