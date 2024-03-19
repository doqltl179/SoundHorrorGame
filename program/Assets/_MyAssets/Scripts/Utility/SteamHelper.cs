using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamHelper : GenericSingleton<SteamHelper> {
    private const string Achievement_ClearChapter = "Clear_Chapter_";
    private const string Achievement_ClearHappyEnding = "Clear_HappyEnding";
    private const string Achievement_ClearBadEnding = "Clear_BadEnding";

    private const string Achievement_GoodChoice = "Good_Choice";



    #region Utility
    public void SetAchievement_GoodChoice() => SetAchievement(Achievement_GoodChoice);

    public void SetAchievement_ClearHappyEnding() => SetAchievement(Achievement_ClearHappyEnding);

    public void SetAchievement_ClearBadEnding() => SetAchievement(Achievement_ClearBadEnding);

    /// <summary>
    /// `LevelIndex + 1 == chapter`가 된다.
    /// </summary>
    public void SetAchievement_ClearChapter(int levelIndex) => SetAchievement(Achievement_ClearChapter + (levelIndex + 1).ToString());

    public void ResetAchievement() {
        SteamUserStats.ResetAllStats(true);
    }

    #region For Test
    public void TestSetAchievement_WinOneGame() => SetAchievement("ACH_WIN_ONE_GAME");

    public void TestSetAchievement_WinOneHundredGames() => SetAchievement("ACH_WIN_100_GAMES");

    public void TestSetAchievement_TravelFarAccum() => SetAchievement("ACH_TRAVEL_FAR_ACCUM");

    public void TestSetAchievement_TravelFarSingle() => SetAchievement("ACH_TRAVEL_FAR_SINGLE");
    #endregion
    #endregion

    private void SetAchievement(string key) {
        if(!SteamManager.Initialized) return;

        SteamUserStats.SetAchievement(key);
        SteamUserStats.StoreStats();
    }
}
