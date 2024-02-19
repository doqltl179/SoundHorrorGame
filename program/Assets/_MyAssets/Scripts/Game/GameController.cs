using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {




    private IEnumerator Start() {
        const int levelWidth = 100;
        const int levelHeight = 100;
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(levelWidth, levelHeight);

        PlayerController.Instance.Pos = LevelLoader.Instance.GetCenterPos();

        while(SceneLoader.Instance.IsLoading) {
            yield return null;
        }
    }
}
