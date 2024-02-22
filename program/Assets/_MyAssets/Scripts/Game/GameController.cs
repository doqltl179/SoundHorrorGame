using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {
    [SerializeField] private PlayerController playerController;



    private IEnumerator Start() {
        const int levelWidth = 50;
        const int levelHeight = 50;
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(levelWidth, levelHeight);

        Vector2Int playerStartCoord = new Vector2Int(
            Random.Range(0, LevelLoader.Instance.LevelWidth),
            Random.Range(0, LevelLoader.Instance.LevelHeight));
        playerController.Pos = LevelLoader.Instance.GetBlockPos(playerStartCoord);
        UtilObjects.Instance.CamPos = playerController.HeadPos;
        UtilObjects.Instance.CamForward = playerController.HeadForward;

#if Play_Game_Automatically
        LevelLoader.Instance.AddMonsterOnLevelRandomly(
            LevelLoader.MonsterType.Froggy, 
            20, 
            LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH, 
            true);
        LevelLoader.Instance.AddMonsterOnLevelRandomly(
            LevelLoader.MonsterType.Honey, 
            20,
            LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
            true);
        LevelLoader.Instance.AddMonsterOnLevelRandomly(
            LevelLoader.MonsterType.Bunny, 
            20,
            LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
            true);

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
        // ������ �����ϰ� ���Ͱ� ��θ� ã�� ���� �� �������� �����ִ� ������
        // ������ ������ �ݶ��̴��� ����� ����Ǵ� �ð��� ��
        yield return null; 

#if Play_Game_Automatically
        LevelLoader.Instance.PlayMonsters();
        playerController.playAutomatically = true;
#else
#endif
    }

#if Play_Game_Automatically
    int watchIndex = 0;
    private void FixedUpdate() {
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

        if(watchIndex > 0) {
            Vector3 camPos = 
                LevelLoader.Instance.Monsters[watchIndex].HeadPos +
                LevelLoader.Instance.Monsters[watchIndex].HeadForward * MazeBlock.BlockSize;
            Vector3 camForward = (LevelLoader.Instance.Monsters[watchIndex].HeadPos - camPos).normalized;
            UtilObjects.Instance.CamPos = Vector3.Lerp(UtilObjects.Instance.CamPos, camPos, Time.deltaTime);
            UtilObjects.Instance.CamForward = Vector3.Lerp(UtilObjects.Instance.CamForward, camForward, Time.deltaTime);
        }
        else {
            UtilObjects.Instance.CamPos = playerController.HeadPos;
            UtilObjects.Instance.CamForward = playerController.HeadForward;
        }
    }
#endif
}
