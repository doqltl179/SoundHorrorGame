using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class GameController : MonoBehaviour {
    [SerializeField] private StandingSpaceConrtoller standingSpaceController;
    [SerializeField] private UserInterface userInterface;

    private GameLevelSettings[] gameLevels = new GameLevelSettings[] {
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            Monsters = new GameLevelSettings.MonsterStruck[] {
                new GameLevelSettings.MonsterStruck() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 5, 
                },
                new GameLevelSettings.MonsterStruck() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 5,
                },
            },

            CollectItemCount = 3,

            LevelStartScenarioKeys = new string[] {

            },
            LevelEndScenarioKeys = null
        },
    };
    public GameLevelSettings CurrentLevelSettings { get; private set; }

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



        PlayerController.Instance.Pos = standingSpaceController.PlayerPos;
        PlayerController.Instance.Rotation = standingSpaceController.PlayerRotation;

        PlayerController.Instance.IsPlaying = true;



        #region 나중에 삭제하세요
        SceneLoader.Instance.ChangeCurrentLoadedSceneImmediately(SceneLoader.SceneType.Game);
        #endregion



        //const int levelWidth = 8;
        //const int levelHeight = 8;
        //LevelLoader.Instance.ResetLevel();
        //LevelLoader.Instance.LoadLevel(levelWidth, levelHeight);

        //Vector2Int playerStartCoord = new Vector2Int(
        //    Random.Range(0, LevelLoader.Instance.LevelWidth),
        //    Random.Range(0, LevelLoader.Instance.LevelHeight));
        //PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(playerStartCoord);
        //UtilObjects.Instance.CamPos = PlayerController.Instance.HeadPos;
        //UtilObjects.Instance.CamForward = PlayerController.Instance.HeadForward;

        //while(SceneLoader.Instance.IsLoading) {
        //    yield return null;
        //}
        //// 레벨을 생성하고 몬스터가 경로를 찾기 전에 한 프레임을 쉬어주는 것으로
        //// 생성된 레벨에 콜라이더가 제대로 적용되는 시간을 줌
        //yield return null;

        //SceneLoader.Instance.ChangeCurrentLoadedSceneImmediately(SceneLoader.SceneType.Game);

        //if(UserSettings.IsFirstPlayBoolean) {


        //    UserSettings.IsFirstPlayBoolean = false;
        //}
        //LevelLoader.Instance.AddItemOnLevelRandomly(
        //    LevelLoader.ItemType.Crystal,
        //    4,
        //    LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
        //    false);

        //LevelLoader.Instance.AddMonsterOnLevelRandomly(
        //    LevelLoader.MonsterType.Honey,
        //    2,
        //    LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH,
        //    false);

        ////int zoom = 3;
        ////Vector2Int calculatedLevelSize = LevelLoader.Instance.GetLevelSize(zoom);
        ////for(int x = 0; x < calculatedLevelSize.x; x++) {
        ////    for(int y = 0; y < calculatedLevelSize.y; y++) {
        ////        Debug.Log(LevelLoader.Instance.GetRandomCoordOnZoomInCoordArea(new Vector2Int(x, y), zoom));
        ////    }
        ////}

        //SoundManager.Instance.PlayBGM(SoundManager.SoundType.Game, 5.0f, 0.3f);
        yield return null;
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
        Quaternion camStartRotate = UtilObjects.Instance.CamRotation;

        const float cameraMoveTime = 3.0f;
        float timeChecker = 0.0f;
        float timeRatio = 0.0f;
        float lerpRatio = 0.0f;
        while(timeChecker < cameraMoveTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / cameraMoveTime;
            lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
            UtilObjects.Instance.CamPos = Vector3.Lerp(camStartPos, standingSpaceController.NPCCameraViewPos, lerpRatio);
            UtilObjects.Instance.CamRotation = Quaternion.Lerp(camStartRotate, standingSpaceController.NPCCameraViewRotation, lerpRatio);

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

            // String Animation
            key = ScenarioStandardKeyString + scenarioIndex.ToString().PadLeft(2, '0');
            scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(key);
            yield return StartCoroutine(scenarioTextAnimationCoroutine);

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
        userInterface.SetMessageButtons();
        userInterface.SetScenarioButtons();

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

        // 통상 궤도 진입



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

    public struct MonsterStruck {
        public LevelLoader.MonsterType type;
        public int generateCount;
    }
    public MonsterStruck[] Monsters;

    public int CollectItemCount;

    public string[] LevelStartScenarioKeys;
    public string[] LevelEndScenarioKeys;
}
