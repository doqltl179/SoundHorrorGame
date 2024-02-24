using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {



    private IEnumerator Start() {
        const int levelWidth = 50;
        const int levelHeight = 50;
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(levelWidth, levelHeight);

        Vector2Int playerStartCoord = new Vector2Int(
            Random.Range(0, LevelLoader.Instance.LevelWidth),
            Random.Range(0, LevelLoader.Instance.LevelHeight));
        PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(playerStartCoord);
        UtilObjects.Instance.CamPos = PlayerController.Instance.HeadPos;
        UtilObjects.Instance.CamForward = PlayerController.Instance.HeadForward;

#if Play_Game_Automatically
        //LevelLoader.Instance.AddMonsterOnLevelRandomly(
        //    LevelLoader.MonsterType.Kitty,
        //    1,
        //    LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
        //    true);
        //LevelLoader.Instance.AddMonsterOnLevelRandomly(
        //    LevelLoader.MonsterType.Starry,
        //    1,
        //    LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
        //    true);
        //LevelLoader.Instance.AddMonsterOnLevelRandomly(
        //    LevelLoader.MonsterType.Cloudy,
        //    1,
        //    LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
        //    true);
        LevelLoader.Instance.AddMonsterOnLevelRandomly(
            LevelLoader.MonsterType.Froggy, 
            40, 
            LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH * 2, 
            false);
        //LevelLoader.Instance.AddMonsterOnLevelRandomly(
        //    LevelLoader.MonsterType.Honey, 
        //    1,
        //    LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
        //    true);
        //LevelLoader.Instance.AddMonsterOnLevelRandomly(
        //    LevelLoader.MonsterType.Bunny, 
        //    1,
        //    LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
        //    true);

        LevelLoader.Instance.AddItemOnLevelRandomly(
            LevelLoader.ItemType.Crystal, 
            20, 
            LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
            false);
#else
#endif

        while(SceneLoader.Instance.IsLoading) {
            yield return null;
        }
        // 레벨을 생성하고 몬스터가 경로를 찾기 전에 한 프레임을 쉬어주는 것으로
        // 생성된 레벨에 콜라이더가 제대로 적용되는 시간을 줌
        yield return null; 

#if Play_Game_Automatically
        LevelLoader.Instance.PlayMonsters();
#else
#endif
    }

#if Play_Game_Automatically
    int watchIndex = 0;
    private void Update() {
        if(Input.GetKeyDown(KeyCode.Alpha1)) watchIndex = 1;
        else if(Input.GetKeyDown(KeyCode.Alpha2)) watchIndex = 2;
        else if(Input.GetKeyDown(KeyCode.Alpha3)) watchIndex = 3;
        else if(Input.GetKeyDown(KeyCode.Alpha4)) watchIndex = 4;
        else if(Input.GetKeyDown(KeyCode.Alpha5)) watchIndex = 5;
        else if(Input.GetKeyDown(KeyCode.Alpha6)) watchIndex = 6;
        else if(Input.GetKeyDown(KeyCode.Alpha7)) watchIndex = 7;
        else if(Input.GetKeyDown(KeyCode.Alpha8)) watchIndex = 8;
        else if(Input.GetKeyDown(KeyCode.Alpha9)) watchIndex = 9;
        else if(Input.GetKeyDown(KeyCode.Alpha0)) watchIndex = 0;
        else if(Input.GetKeyDown(KeyCode.Backspace)) watchIndex = -1;

        if(watchIndex >= 0) {
            Vector3 camPos = 
                LevelLoader.Instance.Monsters[watchIndex].HeadPos +
                LevelLoader.Instance.Monsters[watchIndex].HeadForward * MazeBlock.BlockSize;
            Vector3 camForward = (LevelLoader.Instance.Monsters[watchIndex].HeadPos - camPos).normalized;
            UtilObjects.Instance.CamPos = Vector3.Lerp(UtilObjects.Instance.CamPos, camPos, Time.deltaTime);
            UtilObjects.Instance.CamForward = Vector3.Lerp(UtilObjects.Instance.CamForward, camForward, Time.deltaTime);
        }
        else {
            UtilObjects.Instance.CamPos = PlayerController.Instance.HeadPos;
            UtilObjects.Instance.CamForward = PlayerController.Instance.HeadForward;
        }
    }
#endif
}
