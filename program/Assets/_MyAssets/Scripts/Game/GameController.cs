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
    [SerializeField] private PathGuide pathGuide;

    private GameLevelSettings[] gameLevels = new GameLevelSettings[] {
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 2, 
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 2,
                },
            },

            Items = new GameLevelSettings.ItemStruct[] {
                new GameLevelSettings.ItemStruct() {
                    type = LevelLoader.ItemType.Crystal,
                    generateCount = 1,
                },
            },
        },
    };
    public GameLevelSettings CurrentLevelSettings { get; private set; }

    private Vector2Int standingSpaceCoord;

    private IEnumerator scenarioCoroutine = null;
    private IEnumerator scenarioCameraAnimationCoroutine = null;
    private IEnumerator scenarioTextAnimationCoroutine = null;

    private IEnumerator exitEnterCheckCoroutine = null;



    private void Awake() {
        PlayerController.Instance.OnEnteredNPCArea += OnEnteredNPCArea;

        LevelLoader.Instance.OnItemCollected += OnItemCollected;
    }

    private void OnDestroy() {
        PlayerController.Instance.OnEnteredNPCArea -= OnEnteredNPCArea;

        LevelLoader.Instance.OnItemCollected -= OnItemCollected;

        if(scenarioTextAnimationCoroutine != null) {
            StopCoroutine(scenarioTextAnimationCoroutine);
            scenarioTextAnimationCoroutine = null;
        }
        if(scenarioCameraAnimationCoroutine != null) {
            StopCoroutine(scenarioCameraAnimationCoroutine);
            scenarioCameraAnimationCoroutine = null;
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

        // Component Off
        pathGuide.gameObject.SetActive(false);

        // Level 초기화
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(CurrentLevelSettings.LevelWidth, CurrentLevelSettings.LevelHeight);

        // Sound 초기화
        SoundManager.Instance.ResetAllSoundObjects();

        // 다른 컴포넌트의 `Start`함수가 끝나기를 기다리기 위함
        yield return null;

        // 몬스터 생성
        const int zoom = 1;
        Vector2Int calculatedLevelSize = LevelLoader.Instance.GetLevelSize(zoom);
        int coordCount = calculatedLevelSize.x * calculatedLevelSize.y;
        int randomMin, randomMax;

        int monsterCount = CurrentLevelSettings.Monsters.Sum(t => t.generateCount);
        randomMax = coordCount / monsterCount + 1;
        randomMin = randomMax / 2;

        GameLevelSettings.MonsterStruct monsterStruct;
        Vector2Int zoomInCoord = Vector2Int.zero;
        int coordMoveCount = 0;
        for(int i = 0; i < CurrentLevelSettings.Monsters.Length; i++) {
            monsterStruct = CurrentLevelSettings.Monsters[i];
            for(int j = 0; j < monsterStruct.generateCount; j++) {
                zoomInCoord.y = calculatedLevelSize.y - (coordMoveCount / calculatedLevelSize.x + 1);
                zoomInCoord.x = coordMoveCount % calculatedLevelSize.x;

                LevelLoader.Instance.AddMonsterOnLevel(monsterStruct.type, zoomInCoord, zoom);

                coordMoveCount += Random.Range(randomMin, randomMax);
            }
        }

        // 아이템 생성
        int itemCount = CurrentLevelSettings.Items.Sum(t => t.generateCount);
        randomMax = coordCount / itemCount + 1;
        randomMin = randomMax / 2;

        GameLevelSettings.ItemStruct itemStruct;
        coordMoveCount = 0;
        for(int i = 0; i < CurrentLevelSettings.Items.Length; i++) {
            itemStruct = CurrentLevelSettings.Items[i];
            for(int j = 0; j < itemStruct.generateCount; j++) {
                zoomInCoord.y = calculatedLevelSize.y - (coordMoveCount / calculatedLevelSize.x + 1);
                zoomInCoord.x = coordMoveCount % calculatedLevelSize.x;

                LevelLoader.Instance.AddItemOnLevel(itemStruct.type, zoomInCoord, zoom);

                coordMoveCount += Random.Range(randomMin, randomMax);
            }
        }

        // PickUP 아이템 생성
        itemCount = calculatedLevelSize.x * calculatedLevelSize.y / 5;
        randomMax = coordCount / itemCount + 1;
        randomMin = randomMax / 2;

        coordMoveCount = 0;
        for(int i = 0; i < itemCount; i++) {
            zoomInCoord.y = calculatedLevelSize.y - (coordMoveCount / calculatedLevelSize.x + 1);
            zoomInCoord.x = coordMoveCount % calculatedLevelSize.x;

            LevelLoader.Instance.AddPickUpItemOnLevel(LevelLoader.ItemType.HandlingCube, zoomInCoord, zoom);

            coordMoveCount += Random.Range(randomMin, randomMax);
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

#if Skip_Scenario
        PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(new Vector2Int(standingSpaceCoord.x, standingSpaceCoord.y + 1));
        PlayerController.Instance.Forward = Vector3.forward;

        LevelLoader.Instance.PlayMonsters();
        LevelLoader.Instance.PlayItems();

        OnScenarioEnd();
#endif

        #region 나중에 삭제하세요
        SceneLoader.Instance.ChangeCurrentLoadedSceneImmediately(SceneLoader.SceneType.Game);
        #endregion
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
    private void OnItemCollected() {
        int maxItemCount = CurrentLevelSettings.Items.Sum(t => t.generateCount);
        int collectItem = maxItemCount - LevelLoader.Instance.ItemCount;
        userInterface.SetItemCount(maxItemCount, collectItem);

        // Clear
        if(collectItem >= maxItemCount) {
            if(exitEnterCheckCoroutine == null) {
                exitEnterCheckCoroutine = ExitEnterCheckCoroutine();
                StartCoroutine(exitEnterCheckCoroutine);
            }
        }
    }

    private void OnEnteredNPCArea() {
        StartScenario();
    }
    #endregion

    private IEnumerator ExitEnterCheckCoroutine() {
        standingSpaceController.gameObject.SetActive(true);
        standingSpaceController.NPCActive = false;

        // StandingSpace 위치 설정
        standingSpaceCoord = new Vector2Int(Random.Range(1, CurrentLevelSettings.LevelWidth - 2), -1);
        standingSpaceController.transform.position =
            LevelLoader.Instance.GetBlockPos(standingSpaceCoord) +
            Vector3.forward * MazeBlock.BlockSize * 0.5f;

        // Open Door
        MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        MazeBlock animationBlock2 = standingSpaceController.GetOpenCoordBlock();

        const float wallAnimationTime = 3.0f;
        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, true, wallAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, true, wallAnimationTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        // Set Head Message
        userInterface.SetHeadMessage("ExitOpened", 10.0f);

        // Start Guide
        pathGuide.gameObject.SetActive(true);
        pathGuide.transform.position = PlayerController.Instance.Pos;
        pathGuide.Path = LevelLoader.Instance.GetPath(
            PlayerController.Instance.Pos,
            LevelLoader.Instance.GetBlockPos(new Vector2Int(standingSpaceCoord.x, standingSpaceCoord.y + 1)),
            pathGuide.Radius);

        // Wait Player Entered In StandingSpace
        Vector3 animationWallPos = animationBlock2.GetSidePos(MazeCreator.ActiveWall.F);
        bool isOutOfMaze = false;
        float dist = 100.0f;
        while(true) {
            isOutOfMaze = !LevelLoader.Instance.IsCoordInLevelSize(PlayerController.Instance.CurrentCoord, 0);
            dist = Vector3.Distance(animationWallPos, PlayerController.Instance.Pos);

            if(isOutOfMaze && dist > MazeBlock.BlockSize * 0.5f) {
                break;
            }

            yield return null;
        }

        // Remove Head Message
        userInterface.RemoveHeadMessage();

        // Level 증가
        UserSettings.GameLevel++;

        OnExitEntered();

        exitEnterCheckCoroutine = null;
    }

    #region Scenario
    private IEnumerator Scenario00() {
        // Stop Player
        PlayerController.Instance.IsPlaying = false;

        #region Util Func
        const float wallAnimationTime = 3.0f;
        const float cameraMoveTime = 3.0f;

        Vector3 camStartPos, camEndPos, camStartForward, camEndForward;
        Vector3 camLookAtStartPos, camLookAtEndPos;

        IEnumerator CameraMoveRotateAnimationCoroutine() {
            const float cameraMoveTime = 3.0f;
            float timeChecker = 0.0f;
            float timeRatio = 0.0f;
            float lerpRatio = 0.0f;
            while(timeChecker < cameraMoveTime) {
                timeChecker += Time.deltaTime;
                timeRatio = timeChecker / cameraMoveTime;
                lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
                UtilObjects.Instance.CamPos = Vector3.Lerp(camStartPos, camEndPos, lerpRatio);
                UtilObjects.Instance.CamForward = Vector3.Lerp(camStartForward, camEndForward, lerpRatio);

                yield return null;
            }

            scenarioCameraAnimationCoroutine = null;
        }

        IEnumerator CameraLookAtAnimationCoroutine() {
            Vector3 lookAtPos;

            float timeChecker = 0.0f;
            float timeRatio = 0.0f;
            float lerpRatio = 0.0f;
            while(timeChecker < cameraMoveTime) {
                timeChecker += Time.deltaTime;
                timeRatio = timeChecker / cameraMoveTime;
                lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
                lookAtPos = Vector3.Lerp(camLookAtStartPos, camLookAtEndPos, lerpRatio);
                camEndForward = (lookAtPos - UtilObjects.Instance.CamPos).normalized;
                UtilObjects.Instance.CamForward = Vector3.Lerp(camStartForward, camEndForward, lerpRatio);

                yield return null;
            }

            scenarioCameraAnimationCoroutine = null;
        }
        #endregion

        // Camera Move
        camStartPos = UtilObjects.Instance.CamPos;
        camStartForward = UtilObjects.Instance.CamForward;
        camEndPos = standingSpaceController.NPCCameraViewPos;
        camEndForward = standingSpaceController.NPCCameraViewForward;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Active MessageBox
        userInterface.MessageActive = true;
        userInterface.MessageAlpha = 1.0f;

        // Scenario Start
        const string ScenarioStandardKeyString = "LevelStart00";
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

        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, true, wallAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, true, wallAnimationTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        // Camera Rotation ==> Show Door Opening
        float cameraLookStartHeight = MazeBlock.WallHeight - 1.0f;
        float cameraLookEndHeight = 1.0f;
        Vector3 animationWallPos = animationBlock2.GetSidePos(MazeCreator.ActiveWall.F);

        camStartForward = UtilObjects.Instance.CamForward;
        camLookAtStartPos = animationWallPos + Vector3.up * cameraLookStartHeight;
        camLookAtEndPos = animationWallPos + Vector3.up * cameraLookEndHeight;
        scenarioCameraAnimationCoroutine = CameraLookAtAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Delay
        yield return new WaitForSeconds(1.0f);

        // Camera View 복구
        camStartPos = UtilObjects.Instance.CamPos;
        camEndPos = UtilObjects.Instance.CamPos;
        camStartForward = UtilObjects.Instance.CamForward;
        camEndForward = standingSpaceController.NPCCameraViewForward;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);
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
        camEndPos = PlayerController.Instance.CamPos;
        camStartForward = UtilObjects.Instance.CamForward;
        camEndForward = PlayerController.Instance.Forward;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // 플레이어 움직임 복구
        PlayerController.Instance.IsPlaying = true;

        // 플레이어의 진입을 감지하기 위한 루프문
        Vector3 normalCheckPos = LevelLoader.Instance.GetBlockPos(standingSpaceCoord);
        normalCheckPos.y = PlayerController.PlayerHeight;
        Vector3 normalCheckForward = Vector3.forward;

        float compareDist = MazeBlock.BlockSize * 0.5f;
        float dist = 0.0f;
        bool isPlayerInMaze = false;
        float dot = 1.0f;
        while(dist < compareDist || !isPlayerInMaze || dot < 0.05f) {
            dist = Vector3.Distance(PlayerController.Instance.Pos, animationWallPos);
            isPlayerInMaze = LevelLoader.Instance.IsCoordInLevelSize(PlayerController.Instance.CurrentCoord, 0);
            dot = Vector3.Dot(normalCheckForward, PlayerController.Instance.Forward);

            yield return null;
        }

        // 진입 애니메이션 시작
        PlayerController.Instance.IsPlaying = false;

        camStartPos = UtilObjects.Instance.CamPos;
        camEndPos = normalCheckPos + Vector3.forward * MazeBlock.BlockSize;
        camStartForward = UtilObjects.Instance.CamForward;
        camEndForward = (normalCheckPos - camEndPos).normalized;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Wall Animation
        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, false, wallAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, false, wallAnimationTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        // Camera Animation
        cameraLookStartHeight = 1.0f;
        cameraLookEndHeight = MazeBlock.WallHeight - 1.0f;

        camStartForward = UtilObjects.Instance.CamForward;
        camLookAtStartPos = animationWallPos + Vector3.up * cameraLookStartHeight;
        camLookAtEndPos = animationWallPos + Vector3.up * cameraLookEndHeight;
        scenarioCameraAnimationCoroutine = CameraLookAtAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Delay
        yield return new WaitForSeconds(1.0f);

        // Camera 복구
        camStartPos = UtilObjects.Instance.CamPos;
        camEndPos = PlayerController.Instance.CamPos;
        camStartForward = UtilObjects.Instance.CamForward;
        camEndForward = PlayerController.Instance.CamForward;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // 플레이어 움직임 복구
        PlayerController.Instance.IsPlaying = true;

        OnScenarioEnd();

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

    private void OnExitEntered() {

    }

    private void OnScenarioEnd() {
        SoundManager.Instance.PlayBGM(SoundManager.SoundType.Game, 5.0f, 0.3f);

        standingSpaceController.gameObject.SetActive(false);

        LevelLoader.Instance.PlayMonsters();
        LevelLoader.Instance.PlayItems();

        int maxItemCount = CurrentLevelSettings.Items.Sum(t => t.generateCount);
        int collectItem = maxItemCount - LevelLoader.Instance.ItemCount;
        userInterface.SetItemCount(maxItemCount, collectItem);

        userInterface.HeadMessageActive = true;
        userInterface.MessageActive = false;
        userInterface.MicGageActive = UserSettings.UseMicBoolean;
        userInterface.CollectItemActive = true;
        userInterface.RunGageActive = true;
    }
}

public class GameLevelSettings {
    public int LevelWidth;
    public int LevelHeight;

    public struct MonsterStruct {
        public LevelLoader.MonsterType type;
        public int generateCount;
    }
    public MonsterStruct[] Monsters;

    public struct ItemStruct {
        public LevelLoader.ItemType type;
        public int generateCount;
    }
    public ItemStruct[] Items;
}
