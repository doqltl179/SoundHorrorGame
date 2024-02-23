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
    private const string FuncsMenuName = MenuName + "/Funcs";

    private const string SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock = SymbolsMenuName + "/Use Two Materials On MazeBlock";
    private const string SymbolsMenuItemName_PlayGameAutomatically = SymbolsMenuName + "/Play Game Automatically";
    private const string FuncsMenuItemName_ResetAllSymbols = FuncsMenuName + "/Reset All Symbols";
    private const string FuncsMenuItemName_ApplyAllSymbols = FuncsMenuName + "/Apply All Symbols";

    private static string def_UNITY_POST_PROCESSING_STACK_V2 = "UNITY_POST_PROCESSING_STACK_V2";
    private static string def_Use_Two_Materials_On_MazeBlock = "Use_Two_Materials_On_MazeBlock";
    private static string def_Play_Game_Automatically = "Play_Game_Automatically";

    private static bool useTwoMaterialsOnMazeBlock = false;
    private static bool playGameAutomatically = false;




    [InitializeOnLoadMethod]
    private static void OnEditorLoaded() {
        string[] defines;
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out defines);
        if(defines != null) {
            foreach(string d in defines) {
                if(d == def_Use_Two_Materials_On_MazeBlock) {
                    useTwoMaterialsOnMazeBlock = true;
                }
                else if(d == def_Play_Game_Automatically) {
                    playGameAutomatically = true;
                }
            }
        }

        Menu.SetChecked(SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock, useTwoMaterialsOnMazeBlock);
        Menu.SetChecked(SymbolsMenuItemName_PlayGameAutomatically, playGameAutomatically);
    }

    [MenuItem(FuncsMenuItemName_ApplyAllSymbols)]
    private static void ApplyAllSymbols() {
        string[] defines;
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out defines);

        SetDefine(
            def_Use_Two_Materials_On_MazeBlock,
            SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock,
            ref useTwoMaterialsOnMazeBlock,
            true);
        SetDefine(
            def_Play_Game_Automatically,
            SymbolsMenuItemName_PlayGameAutomatically,
            ref playGameAutomatically,
            true);
    }

    [MenuItem(FuncsMenuItemName_ResetAllSymbols)]
    private static void ResetAllSymbols() {
        string[] defines;
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out defines);

        SetDefine(
            def_Use_Two_Materials_On_MazeBlock,
            SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock,
            ref useTwoMaterialsOnMazeBlock,
            false);
        SetDefine(
            def_Play_Game_Automatically,
            SymbolsMenuItemName_PlayGameAutomatically,
            ref playGameAutomatically,
            false);
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
            }
        }
        else {
            if(isExist) {
                string[] newDefines = defines.Where(t => t != def).ToArray();
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newDefines);
            }
        }

        Menu.SetChecked(menuPath, property);
    }
}
#endif