using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

using Random = UnityEngine.Random;

public class GameController : MonoBehaviour {
    [SerializeField] private StandingSpaceConrtoller standingSpaceController;
    [SerializeField] private UserInterface userInterface;

    private GameLevelSettings[] gameLevels = new GameLevelSettings[] {
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 5, 
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 5,
                },
            },

            CollectItemCount = 3,
        },
    };
    public GameLevelSettings CurrentLevelSettings { get; private set; }

    private Vector2Int standingSpaceCoord;

    private IEnumerator scenarioCoroutine = null;
    private IEnumerator scenarioTextAnimationCoroutine = null;



    private void Awake() {
        PlayerController.Instance.OnEnteredNPCArea += OnEnteredNPCArea;
    }

    private void OnDestroy() {
        PlayerController.Instance.OnEnteredNPCArea -= OnEnteredNPCArea;

        if(scenarioTextAnimationCoroutine != null) {
            StopCoroutine(scenarioTextAnimationCoroutine);
            scenarioTextAnimationCoroutine = null;
        }
        if(scenarioCoroutine != null) {
            StopCoroutine(scenarioCoroutine);
            scenarioCoroutine = null;
        }
    }

    private IEnumerator Start() {
        if(SceneLoader.Instance.Param == null) {
            if(UserSettings.GameLevel >= gameLevels.Length) {
                Debug.LogError($"GameLevel is out of range. GameLevel: {UserSettings.GameLevel}");

                yield break;
            }

            CurrentLevelSettings = gameLevels[UserSettings.GameLevel];
        }
        else {
            object[] param = SceneLoader.Instance.Param;
            GameLevelSettings levelSettings = (GameLevelSettings)param[0];

            CurrentLevelSettings = levelSettings;
        }

        // Level 초기화
        InitGameLevel(CurrentLevelSettings.LevelWidth, CurrentLevelSettings.LevelHeight);

        // Sound 초기화
        SoundManager.Instance.ResetAllSoundObjects();

        // 다른 컴포넌트의 `Start`함수가 끝나기를 기다리기 위함
        yield return null;

        // 몬스터 생성
        int zoom = 2;
        Vector2Int calculatedLevelSize = LevelLoader.Instance.GetLevelSize(zoom);

        GameLevelSettings.MonsterStruct tempStruct;
        Vector2Int zoomInCoord = Vector2Int.zero;
        int addedCount = 0;
        for(int i = 0; i < CurrentLevelSettings.Monsters.Length; i++) {
            tempStruct = CurrentLevelSettings.Monsters[i];
            for(int j = 0; j < tempStruct.generateCount; j++) {
                zoomInCoord.y = calculatedLevelSize.y - (addedCount / calculatedLevelSize.x + 1);
                zoomInCoord.x = addedCount % calculatedLevelSize.x;

                LevelLoader.Instance.AddMonsterOnLevel(tempStruct.type, zoomInCoord, zoom);

                addedCount++;
            }
        }

        // StandingSpace 위치 설정
        standingSpaceCoord = new Vector2Int(Random.Range(1, CurrentLevelSettings.LevelWidth - 2), -1);
        standingSpaceController.transform.position = 
            LevelLoader.Instance.GetBlockPos(standingSpaceCoord) + 
            Vector3.forward * MazeBlock.BlockSize * 0.5f;

        // 플레이어 transform 초기화
        PlayerController.Instance.Pos = standingSpaceController.PlayerPos;
        PlayerController.Instance.Rotation = standingSpaceController.PlayerRotation;

        PlayerController.Instance.IsPlaying = true;

        #region 나중에 삭제하세요
        SceneLoader.Instance.ChangeCurrentLoadedSceneImmediately(SceneLoader.SceneType.Game);
        #endregion
    }

    private void InitGameLevel(int levelWidth, int levelHeight) {
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(levelWidth, levelHeight);
    }

    #region Utility
    public void StartScenario() {
        if(scenarioCoroutine == null) {
            switch(UserSettings.GameLevel) {
                case 0: scenarioCoroutine = Scenario00(); break;
                case 1: scenarioCoroutine = Scenario01(); break;
                case 2: scenarioCoroutine = Scenario02(); break;
                default: scenarioCoroutine = ScenarioDefault(); break;
            }

            StartCoroutine(scenarioCoroutine);
        }
    }
    #endregion

    #region Action
    private void OnEnteredNPCArea() {
        StartScenario();
    }
    #endregion

    #region Scenario
    private IEnumerator Scenario00() {
        // Stop Player
        PlayerController.Instance.IsPlaying = false;

        // Camera Move
        Vector3 camStartPos = UtilObjects.Instance.CamPos;
        Quaternion camStartRotation = UtilObjects.Instance.CamRotation;

        const float cameraMoveTime = 3.0f;
        float timeChecker = 0.0f;
        float timeRatio = 0.0f;
        float lerpRatio = 0.0f;
        while(timeChecker < cameraMoveTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / cameraMoveTime;
            lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
            UtilObjects.Instance.CamPos = Vector3.Lerp(camStartPos, standingSpaceController.NPCCameraViewPos, lerpRatio);
            UtilObjects.Instance.CamRotation = Quaternion.Lerp(camStartRotation, standingSpaceController.NPCCameraViewRotation, lerpRatio);

            yield return null;
        }

        // Active MessageBox
        userInterface.SetActiveMessage = true;
        userInterface.MessageAlpha = 1.0f;

        // Scenario Start
        const string ScenarioStandardKeyString = "LevelStart00";
        WaitForSeconds wait = new WaitForSeconds(2.0f);

        string key = "";
        void OnLocaleChanged(string languageCode) {
            Locale currentLocale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
            StringTable currentTable = LocalizationSettings.StringDatabase.GetTable("Scenario", currentLocale);
            StringTableEntry currentTableEntry = currentTable.GetEntry(key);
            userInterface.MessageText = currentTableEntry.Value;
        }

        int scenarioIndex = 0;
        const int turningPointIndex = 6;

        // 0: 대기
        // -1: 이전으로
        // 1: 다음으로
        int moveScenario = 0; 
        while(scenarioIndex < turningPointIndex) {
            // Reset Buttons
            userInterface.SetMessageButtons();
            userInterface.SetScenarioButtons();

            // 추임새
            standingSpaceController.SetAnimationTrigger_Talking();

            // String Animation
            key = ScenarioStandardKeyString + scenarioIndex.ToString().PadLeft(2, '0');
            scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(key);
            yield return StartCoroutine(scenarioTextAnimationCoroutine);

            // 추임새 제한
            // 완벽하지는 않지만 이거라도 하는게 좋아 보인다.
            standingSpaceController.SetAnimationResetTrigger_Talking();

            moveScenario = 0;
            switch(scenarioIndex) {
                case 0: userInterface.SetScenarioButtons(null, () => { moveScenario = 1; }); break;
                //case turningPointIndex - 1: userInterface.SetScenarioButtons(() => { moveScenario = -1; }, null); break;
                default: userInterface.SetScenarioButtons(() => { moveScenario = -1; }, () => { moveScenario = 1; }); break;
            }
            UserSettings.OnLocaleChanged += OnLocaleChanged;
            while(moveScenario == 0) {
                yield return null;
            }
            UserSettings.OnLocaleChanged -= OnLocaleChanged;

            scenarioIndex += moveScenario;
        }

        #region Start Turning Point
        // Reset Buttons
        userInterface.SetMessageButtons();
        userInterface.SetScenarioButtons();

        // 추임새
        standingSpaceController.SetAnimationTrigger_Talking();

        // String Animation
        key = ScenarioStandardKeyString + scenarioIndex.ToString().PadLeft(2, '0');
        scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(key);
        yield return StartCoroutine(scenarioTextAnimationCoroutine);

        // 0: 대기
        // -1: Special Scenario 진입
        // 1: 통상 궤도 진입
        moveScenario = 0;
        userInterface.SetMessageButtons(
            "Yes",
            () => { moveScenario = 1; },
            "No",
            () => { moveScenario = -1; });
        UserSettings.OnLocaleChanged += OnLocaleChanged;
        while(moveScenario == 0) {
            yield return null;
        }
        UserSettings.OnLocaleChanged -= OnLocaleChanged;
        #endregion

        // Special Scenario 진입
        if(moveScenario == -1) {

        }

        #region Door Open Animation
        // Hide MessageBox
        userInterface.MessageAlpha = 0.0f;

        // Open Door
        MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        MazeBlock animationBlock2 = standingSpaceController.GetOpenCoordBlock();
        const float openAnimationTime = 3.0f;

        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, true, openAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, true, openAnimationTime));

        // Camera Rotation ==> Show Door Opening
        float cameraLookStartHeight = MazeBlock.WallHeight - 1.0f;
        float cameraLookEndHeight = 1.0f;
        Vector3 animationWallPos = animationBlock2.GetSidePos(MazeCreator.ActiveWall.F);
        Vector3 cameraLookPos;
        Vector3 cameraLookForward;
        Vector3 camStartForward = UtilObjects.Instance.CamForward;

        timeChecker = 0.0f;
        timeRatio = 0.0f;
        lerpRatio = 0.0f;
        while(timeChecker < cameraMoveTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / cameraMoveTime;
            lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
            cameraLookPos = new Vector3(animationWallPos.x, Mathf.Lerp(cameraLookStartHeight, cameraLookEndHeight, lerpRatio), animationWallPos.z);
            cameraLookForward = (cameraLookPos - UtilObjects.Instance.CamPos).normalized;
            UtilObjects.Instance.CamForward = Vector3.Lerp(camStartForward, cameraLookForward, lerpRatio);

            yield return null;
        }

        // Delay
        yield return new WaitForSeconds(1.0f);

        // Camera View 복구
        camStartRotation = UtilObjects.Instance.CamRotation;

        timeChecker = 0.0f;
        timeRatio = 0.0f;
        lerpRatio = 0.0f;
        while(timeChecker < cameraMoveTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / cameraMoveTime;
            lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
            UtilObjects.Instance.CamRotation = Quaternion.Lerp(camStartRotation, standingSpaceController.NPCCameraViewRotation, lerpRatio);

            yield return null;
        }
        #endregion

        // 통상 궤도 진입
        // Show MessageBox
        userInterface.MessageAlpha = 1.0f;

        // 시나리오 이어서 시작
        scenarioIndex++;
        const int scenarioEndIndex = 13;

        moveScenario = 0;
        while(scenarioIndex <= scenarioEndIndex) {
            // Reset Buttons
            userInterface.SetMessageButtons();
            userInterface.SetScenarioButtons();

            // 추임새
            standingSpaceController.SetAnimationTrigger_Talking();

            // String Animation
            key = ScenarioStandardKeyString + scenarioIndex.ToString().PadLeft(2, '0');
            scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(key);
            yield return StartCoroutine(scenarioTextAnimationCoroutine);

            // 추임새 제한
            standingSpaceController.SetAnimationResetTrigger_Talking();

            moveScenario = 0;
            switch(scenarioIndex) {
                case turningPointIndex + 1: userInterface.SetScenarioButtons(null, () => { moveScenario = 1; }); break;
                //case scenarioEndIndex: userInterface.SetScenarioButtons(() => { moveScenario = -1; }, null); break;
                default: userInterface.SetScenarioButtons(() => { moveScenario = -1; }, () => { moveScenario = 1; }); break;
            }
            UserSettings.OnLocaleChanged += OnLocaleChanged;
            while(moveScenario == 0) {
                yield return null;
            }
            UserSettings.OnLocaleChanged -= OnLocaleChanged;

            scenarioIndex += moveScenario;
        }

        // Hide MessageBox
        userInterface.MessageAlpha = 0.0f;

        // Camera Move
        PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(standingSpaceCoord);
        PlayerController.Instance.Forward = Vector3.forward;
        PlayerController.Instance.ResetCameraAnchor();

        camStartPos = UtilObjects.Instance.CamPos;
        camStartRotation = UtilObjects.Instance.CamRotation;

        timeChecker = 0.0f;
        timeRatio = 0.0f;
        lerpRatio = 0.0f;
        while(timeChecker < cameraMoveTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / cameraMoveTime;
            lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
            UtilObjects.Instance.CamPos = Vector3.Lerp(camStartPos, PlayerController.Instance.CamPos, lerpRatio);
            UtilObjects.Instance.CamRotation = Quaternion.Lerp(camStartRotation, PlayerController.Instance.CamRotation, lerpRatio);

            yield return null;
        }

        // 플레이어 움직임 복구
        PlayerController.Instance.IsPlaying = true;

        // 플레이어의 진입을 감지하기 위한 루프문
        float compareDist = MazeBlock.BlockSize * 0.5f;
        float dist = 0.0f;
        bool isPlayerInMaze = false;
        while(dist < compareDist || !isPlayerInMaze) {
            dist = Vector3.Distance(PlayerController.Instance.Pos, animationWallPos);
            isPlayerInMaze = LevelLoader.Instance.IsCoordInLevelSize(PlayerController.Instance.CurrentCoord, 0);

            yield return null;
        }

        // 진입 시 애니메이션 시작


        // BGM 시작
        SoundManager.Instance.PlayBGM(SoundManager.SoundType.Game, 5.0f, 0.3f);

        scenarioCoroutine = null;
    }

    private IEnumerator Scenario01() {
        yield return null;

        scenarioCoroutine = null;
    }

    private IEnumerator Scenario02() {
        yield return null;

        scenarioCoroutine = null;
    }

    private IEnumerator ScenarioDefault() {
        yield return null;

        scenarioCoroutine = null;
    }

    private IEnumerator ScenarioTextAnimationCoroutine(string key) {
        Locale currentLocale = LocalizationSettings.AvailableLocales.GetLocale(UserSettings.LanguageCode);
        StringTable currentTable = LocalizationSettings.StringDatabase.GetTable("Scenario", currentLocale);
        StringTableEntry currentTableEntry = currentTable.GetEntry(key);
        string text = currentTableEntry.Value;
        string animationText = "";
        int textCopyLength = 0;

        void OnLocaleChanged(string localeCode) {
            currentLocale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            currentTable = LocalizationSettings.StringDatabase.GetTable("Scenario", currentLocale);
            currentTableEntry = currentTable.GetEntry(key);
            text = currentTableEntry.Value;
            animationText = "";
            textCopyLength = 0;

            userInterface.MessageText = "";
        }
        UserSettings.OnLocaleChanged += OnLocaleChanged;

        userInterface.SetMessageBoxButton(() => {
            textCopyLength = text.Length;
        });

        WaitForSeconds animationWait = new WaitForSeconds(0.05f);
        while(textCopyLength <= text.Length) {
            animationText = text[0..textCopyLength];
            userInterface.MessageText = animationText;

            textCopyLength++;
            yield return animationWait;
        }

        userInterface.SetMessageBoxButton();
        UserSettings.OnLocaleChanged -= OnLocaleChanged;
        scenarioTextAnimationCoroutine = null;
    }
    #endregion
}

public class GameLevelSettings {
    public int LevelWidth;
    public int LevelHeight;

    public struct MonsterStruct {
        public LevelLoader.MonsterType type;
        public int generateCount;
    }
    public MonsterStruct[] Monsters;

    public int CollectItemCount;
}
