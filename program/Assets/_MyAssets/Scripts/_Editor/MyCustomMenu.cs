#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

using Random = UnityEngine.Random;

public class MyCustomMenu : MonoBehaviour
{
    public const string MenuName = "My Custom Menu";

    private const string SymbolsMenuName = MenuName + "/Symbols";
    private const string FuncsMenuName = MenuName + "/Funcs";
    private const string CheatMenuName = MenuName + "/Cheat";

    private const string SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock = SymbolsMenuName + "/Use Two Materials On MazeBlock";
    private const string SymbolsMenuItemName_SkipScenario = SymbolsMenuName + "/Skip Scenario";
    private const string SymbolsMenuItemName_ShowFPS = SymbolsMenuName + "/Show FPS";
    private const string FuncsMenuItemName_ResetAllSymbols = FuncsMenuName + "/Reset All Symbols";
    private const string FuncsMenuItemName_ApplyAllSymbols = FuncsMenuName + "/Apply All Symbols";
    private const string CheatMenuItemName_MoveToItem = CheatMenuName + "/Move To Item";
    private const string CheatMenuItemName_Clear = CheatMenuName + "/Clear";
    private const string CheatMenuItemName_CatchedMonster = CheatMenuName + "/Catched Monster";

    private static string def_UNITY_POST_PROCESSING_STACK_V2 = "UNITY_POST_PROCESSING_STACK_V2";
    private static string def_Use_Two_Materials_On_MazeBlock = "Use_Two_Materials_On_MazeBlock";
    private static string def_Skip_Scenario = "Skip_Scenario";
    private static string def_Show_FPS = "Show_FPS";

    private static bool useTwoMaterialsOnMazeBlock = false;
    private static bool skipScenario = false;
    private static bool showFPS = false;



    [InitializeOnLoadMethod]
    private static void OnEditorLoaded() {
        string[] defines;
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out defines);
        if(defines != null) {
            foreach(string d in defines) {
                if(d == def_Use_Two_Materials_On_MazeBlock) {
                    useTwoMaterialsOnMazeBlock = true;
                }
                else if(d == def_Skip_Scenario) {
                    skipScenario = true;
                }
                else if(d == def_Show_FPS) {
                    showFPS = true;
                }
            }
        }

        Menu.SetChecked(SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock, useTwoMaterialsOnMazeBlock);
        Menu.SetChecked(SymbolsMenuItemName_SkipScenario, skipScenario);
        Menu.SetChecked(SymbolsMenuItemName_ShowFPS, showFPS);
    }

    #region Util Func
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
            def_Skip_Scenario,
            SymbolsMenuItemName_SkipScenario,
            ref skipScenario,
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
            def_Skip_Scenario,
            SymbolsMenuItemName_SkipScenario,
            ref skipScenario,
            false);
    }
    #endregion

    #region Define
    [MenuItem(SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock)]
    private static void UseMazeBlockMaterial() {
        SetDefine(
            def_Use_Two_Materials_On_MazeBlock, 
            SymbolsMenuItemName_UseTwoMaterialsOnMazeBlock,
            ref useTwoMaterialsOnMazeBlock);
    }

    [MenuItem(SymbolsMenuItemName_SkipScenario)]
    private static void SkipScenario() {
        SetDefine(
            def_Skip_Scenario,
            SymbolsMenuItemName_SkipScenario,
            ref skipScenario);
    }

    [MenuItem(SymbolsMenuItemName_ShowFPS)]
    private static void ShowFPS() {
        SetDefine(
            def_Show_FPS,
            SymbolsMenuItemName_ShowFPS,
            ref showFPS);
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
    #endregion

    #region Cheat
    [MenuItem(CheatMenuItemName_MoveToItem)]
    private static void MoveToItem() {
        if(SceneLoader.Instance.CurrentLoadedScene == SceneLoader.SceneType.Game) {
            if(LevelLoader.Instance.ItemCount > 0) {
                PlayerController.Instance.Pos = LevelLoader.Instance.Items[0].Pos;
            }
        }
    }

    [MenuItem(CheatMenuItemName_Clear)]
    private static void Clear() {
        List<ItemController> items = new List<ItemController>();
        foreach(ItemController item in LevelLoader.Instance.Items) {
            items.Add(item);
        }
        foreach(ItemController item in items) {
            LevelLoader.Instance.CollectItem(item);
        }

        StandingSpaceConrtoller standingspaceController = FindAnyObjectByType<StandingSpaceConrtoller>();
        Vector2Int teleportCoord = LevelLoader.Instance.GetMazeCoordinate(standingspaceController.transform.position + Vector3.forward);
        PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(teleportCoord);
        PlayerController.Instance.Forward = Vector3.back;
    }

    [MenuItem(CheatMenuItemName_CatchedMonster)]
    private static void CatchedMonster() {
        List<MonsterController> monsters = LevelLoader.Instance.Monsters;
        PlayerController.Instance.Pos = monsters[Random.Range(0, monsters.Count)].Pos;
    }
    #endregion
}
#endif