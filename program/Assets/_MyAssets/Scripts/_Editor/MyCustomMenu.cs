#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MyCustomMenu : MonoBehaviour
{
    private const string MenuName = "My Custom Menu";

    private const string SymbolsMenuName = MenuName + "/Symbols";

    private const string SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock = SymbolsMenuName + "/Use Two Materials On MazeBlock";

    private static string def_UNITY_POST_PROCESSING_STACK_V2 = "UNITY_POST_PROCESSING_STACK_V2";
    private static string def_Use_Two_Materials_On_MazeBlock = "Use_Two_Materials_On_MazeBlock";

    private static List<string> symbolList = new List<string>() {
        def_UNITY_POST_PROCESSING_STACK_V2
    };

    private static bool useTwoMaterialsOnMazeBlock = false;



    private void Awake() {
        string[] defines;
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out defines);
        if(defines != null) {
            foreach(string d in defines) {
                if(d == def_Use_Two_Materials_On_MazeBlock) useTwoMaterialsOnMazeBlock = true;
            }
        }

        if(useTwoMaterialsOnMazeBlock) {
            symbolList.Add(def_Use_Two_Materials_On_MazeBlock);
            Menu.SetChecked(SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock, useTwoMaterialsOnMazeBlock);
        }
    }

    [MenuItem(SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock)]
    private static void UseMazeBlockMaterial() {
        useTwoMaterialsOnMazeBlock = !useTwoMaterialsOnMazeBlock;
        if(useTwoMaterialsOnMazeBlock) symbolList.Add(def_Use_Two_Materials_On_MazeBlock);
        else symbolList.Remove(def_Use_Two_Materials_On_MazeBlock);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbolList.ToArray());

        Menu.SetChecked(SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock, useTwoMaterialsOnMazeBlock);
    }
}
#endif