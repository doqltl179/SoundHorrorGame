#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class MyCustomMenu : MonoBehaviour
{
    private const string MenuName = "My Custom Menu";

    private const string SymbolsMenuName = MenuName + "/Symbols";

    private const string SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock = SymbolsMenuName + "/Use Two Materials On MazeBlock";
    private const string SymbolsMenuItemName_PlayGameAutomatically = SymbolsMenuName + "/Play Game Automatically";

    private static string def_UNITY_POST_PROCESSING_STACK_V2 = "UNITY_POST_PROCESSING_STACK_V2";
    private static string def_Use_Two_Materials_On_MazeBlock = "Use_Two_Materials_On_MazeBlock";
    private static string def_Play_Game_Automatically = "Play_Game_Automatically";

    private static bool useTwoMaterialsOnMazeBlock = false;
    private static bool playGameAutomatically = false;



    private void Awake() {
        string[] defines;
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out defines);
        if(defines != null) {
            foreach(string d in defines) {
                if(d == def_Use_Two_Materials_On_MazeBlock) {
                    SetDefine(
                        def_Use_Two_Materials_On_MazeBlock,
                        SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock,
                        ref useTwoMaterialsOnMazeBlock,
                        true);
                }
                else if(d == def_Play_Game_Automatically) {
                    SetDefine(
                        def_Play_Game_Automatically,
                        SymbolsMenuItemName_PlayGameAutomatically,
                        ref playGameAutomatically,
                        true);
                }
            }
        }
    }

    [MenuItem(SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock)]
    private static void UseMazeBlockMaterial() {
        SetDefine(
            def_Use_Two_Materials_On_MazeBlock, 
            SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock,
            ref useTwoMaterialsOnMazeBlock);
    }

    [MenuItem(SymbolsMenuItemName_PlayGameAutomatically)]
    private static void PlayGameAutomatically() {
        SetDefine(
            def_Play_Game_Automatically,
            SymbolsMenuItemName_PlayGameAutomatically,
            ref playGameAutomatically);
    }

    private static void SetDefine(string def, string menuPath, ref bool property) {
        SetDefine(def, menuPath, ref property, !property);
    }

    private static void SetDefine(string def, string menuPath, ref bool property, bool propertyValue) {
        property = propertyValue;

        string[] defines;
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out defines);

        bool isExist = Array.FindIndex(defines, t => t == def) >= 0;
        if(property) {
            if(!isExist) {
                string[] newDefines = new string[defines.Length + 1];
                Array.Copy(defines, 0, newDefines, 0, defines.Length);
                newDefines[newDefines.Length - 1] = def;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newDefines);

                Menu.SetChecked(menuPath, property);
            }
        }
        else {
            if(isExist) {
                string[] newDefines = defines.Where(t => t != def).ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newDefines);

                Menu.SetChecked(menuPath, property);
            }
        }
    }
}
#endif