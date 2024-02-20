using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {




    private IEnumerator Start() {
        const int levelWidth = 50;
        const int levelHeight = 50;
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(levelWidth, levelHeight);

        PlayerController.Instance.Pos = LevelLoader.Instance.GetCenterPos();

#if Play_Game_Automatically
        LevelLoader.Instance.AddMonsterOnLevelRandomly(LevelLoader.MonsterType.Honey, 10);
        LevelLoader.Instance.AddMonsterOnLevelRandomly(LevelLoader.MonsterType.Bunny, 10);
#else
#endif

        while(SceneLoader.Instance.IsLoading) {
            yield return null;
        }

#if Play_Game_Automatically
        LevelLoader.Instance.PlayMonsters();
#else
#endif
    }
}
