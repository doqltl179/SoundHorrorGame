using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

public class GameController : MonoBehaviour {
    [SerializeField] private StandingSpaceConrtoller standingSpaceController;
    [SerializeField] private UserInterface userInterface;
    [SerializeField] private PathGuide pathGuide;

    private readonly GameLevelSettings[] gameLevels = new GameLevelSettings[] {
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            IsEmpty = false,

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
                    generateCount = 3,
                },
            },

            HandlingCubeCount = 8, 
            TeleportCount = 0, 
        },
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            IsEmpty = false,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Froggy,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Kitty,
                    generateCount = 2,
                },
            },

            Items = new GameLevelSettings.ItemStruct[] {
                new GameLevelSettings.ItemStruct() {
                    type = LevelLoader.ItemType.Crystal,
                    generateCount = 3,
                },
            },

            HandlingCubeCount = 8,
            TeleportCount = 2,
        },
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            IsEmpty = false,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Froggy,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Kitty,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Cloudy,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Starry,
                    generateCount = 2,
                },
            },

            Items = new GameLevelSettings.ItemStruct[] {
                new GameLevelSettings.ItemStruct() {
                    type = LevelLoader.ItemType.Crystal,
                    generateCount = 5,
                },
            },

            HandlingCubeCount = 8,
            TeleportCount = 4,
        },
        new GameLevelSettings() {
            LevelWidth = 16,
            LevelHeight = 16,

            IsEmpty = false,

            Monsters = new GameLevelSettings.MonsterStruct[] {
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Bunny,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Honey,
                    generateCount = 3,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Froggy,
                    generateCount = 2,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Kitty,
                    generateCount = 3,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Cloudy,
                    generateCount = 3,
                },
                new GameLevelSettings.MonsterStruct() {
                    type = LevelLoader.MonsterType.Starry,
                    generateCount = 3,
                },
            },

            Items = new GameLevelSettings.ItemStruct[] {
                new GameLevelSettings.ItemStruct() {
                    type = LevelLoader.ItemType.Crystal,
                    generateCount = 3,
                },
            },

            HandlingCubeCount = 0,
            TeleportCount = 4,
        },
    };
    public GameLevelSettings CurrentLevelSettings { get; private set; }
    private ToyHammer toyHammer = null;

    private Vector2Int standingSpaceCoord;

    private IEnumerator scenarioCoroutine = null;
    private IEnumerator scenarioCameraAnimationCoroutine = null;
    private IEnumerator scenarioTextAnimationCoroutine = null;

    private IEnumerator exitEnterCheckCoroutine = null;

    private IEnumerator onPlayerCatchedCoroutine = null;
    private IEnumerator onGameEndCoroutine = null;

    private IEnumerator initGameCoroutine = null;



    private void Awake() {
        PlayerController.Instance.OnEnteredNPCArea += OnEnteredNPCArea;
        PlayerController.Instance.OnPlayerCatched += OnPlayerCatched;

        LevelLoader.Instance.OnItemCollected += OnItemCollected;
    }

    private void OnDestroy() {
        PlayerController.Instance.OnEnteredNPCArea -= OnEnteredNPCArea;
        PlayerController.Instance.OnPlayerCatched -= OnPlayerCatched;

        LevelLoader.Instance.OnItemCollected -= OnItemCollected;

        //if(onGameEndCoroutine != null) {
        //    StopCoroutine(onGameEndCoroutine);
        //    onGameEndCoroutine = null;
        //}
        //if(onPlayerCatchedCoroutine != null) {
        //    StopCoroutine(onPlayerCatchedCoroutine);
        //    onPlayerCatchedCoroutine = null;
        //}
        //if(exitEnterCheckCoroutine != null) {
        //    StopCoroutine(exitEnterCheckCoroutine);
        //    exitEnterCheckCoroutine = null;
        //}
        //if(scenarioTextAnimationCoroutine != null) {
        //    StopCoroutine(scenarioTextAnimationCoroutine);
        //    scenarioTextAnimationCoroutine = null;
        //}
        //if(scenarioCameraAnimationCoroutine != null) {
        //    StopCoroutine(scenarioCameraAnimationCoroutine);
        //    scenarioCameraAnimationCoroutine = null;
        //}
        //if(scenarioCoroutine != null) {
        //    StopCoroutine(scenarioCoroutine);
        //    scenarioCoroutine = null;
        //}

        StopAllCoroutines();
    }

    private void Start() {
        if(initGameCoroutine == null) {
            initGameCoroutine = InitGameCoroutine();
            StartCoroutine(initGameCoroutine);
        }

        #region 나중에 삭제하세요
        SceneLoader.Instance.ChangeCurrentLoadedSceneImmediately(SceneLoader.SceneType.Game);
        #endregion
    }

    private IEnumerator InitGameCoroutine() {
        if(SceneLoader.Instance.Param == null) {
            if(UserSettings.GameLevel >= gameLevels.Length) {
                Debug.LogError($"GameLevel is out of range. GameLevel: {UserSettings.GameLevel}");

                //yield break;
                UserSettings.GameLevel = 0;
            }

            CurrentLevelSettings = gameLevels[UserSettings.GameLevel];
        }
        else {
            object[] param = SceneLoader.Instance.Param;
            GameLevelSettings levelSettings = (GameLevelSettings)param[0];

            CurrentLevelSettings = levelSettings;
        }

        // Mic Off
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = true;
            userInterface.MicGageActive = false;
        }

        // Component Off
        pathGuide.gameObject.SetActive(false);

        // Level 초기화
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(CurrentLevelSettings.LevelWidth, CurrentLevelSettings.LevelHeight, CurrentLevelSettings.IsEmpty);

        // Sound 초기화
        SoundManager.Instance.ResetAllSoundObjects();

        // 다른 컴포넌트의 `Start`함수가 끝나기를 기다리기 위함
        yield return null;

        const int zoom = 2;
        Vector2Int calculatedLevelSize = LevelLoader.Instance.GetLevelSize(zoom);
        Vector2Int zoomInCoord = Vector2Int.zero;
        int coordCount = calculatedLevelSize.x * calculatedLevelSize.y;
        int randomMin, randomMax;
        int zoomInCoordIndex;

        // 미로의 가장 밑 줄(플레이어가 시작하는 곳)에는 아무것도 놓지 못하게 함.
        Vector2Int[] ignoreCoords = new Vector2Int[LevelLoader.Instance.LevelWidth];
        for(int i = 0; i < ignoreCoords.Length; i++) {
            ignoreCoords[i] = new Vector2Int(i, 0);
        }
        int ignoreCoordsCopyStartIndex = ignoreCoords.Length;

        // 아이템 생성
        int itemCount = CurrentLevelSettings.Items != null ? CurrentLevelSettings.Items.Sum(t => t.generateCount) : 0;
        if(itemCount > 0) {
            randomMax = coordCount / itemCount;
            randomMin = randomMax / 2;

            GameLevelSettings.ItemStruct itemStruct;
            zoomInCoordIndex = 0;
            for(int i = 0; i < CurrentLevelSettings.Items.Length; i++) {
                itemStruct = CurrentLevelSettings.Items[i];
                for(int j = 0; j < itemStruct.generateCount; j++) {
                    zoomInCoordIndex += Random.Range(randomMin, randomMax);

                    zoomInCoord.y = calculatedLevelSize.y - (zoomInCoordIndex / calculatedLevelSize.x + 1);
                    zoomInCoord.x = zoomInCoordIndex % calculatedLevelSize.x;
                    LevelLoader.Instance.AddItemOnLevel(itemStruct.type, zoomInCoord, zoom, ignoreCoords);

                    zoomInCoordIndex++;
                }
            }
        }
        Array.Resize(ref ignoreCoords, ignoreCoords.Length + LevelLoader.Instance.ItemCount);
        Array.Copy(LevelLoader.Instance.GetAllItemCoords(), 0, ignoreCoords, ignoreCoordsCopyStartIndex, LevelLoader.Instance.ItemCount);
        ignoreCoordsCopyStartIndex = ignoreCoords.Length;

        // Teleport 생성
        itemCount = CurrentLevelSettings.TeleportCount;
        if(itemCount > 0) {
            randomMax = coordCount / itemCount;
            randomMin = randomMax / 2;

            zoomInCoordIndex = 0;
            for(int i = 0; i < itemCount; i++) {
                zoomInCoordIndex += Random.Range(randomMin, randomMax);

                zoomInCoord.y = calculatedLevelSize.y - (zoomInCoordIndex / calculatedLevelSize.x + 1);
                zoomInCoord.x = zoomInCoordIndex % calculatedLevelSize.x;
                LevelLoader.Instance.AddTeleportOnLevel(LevelLoader.ItemType.Teleport, zoomInCoord, zoom, ignoreCoords);

                zoomInCoordIndex++;
            }
        }
        Array.Resize(ref ignoreCoords, ignoreCoords.Length + LevelLoader.Instance.TeleportCount);
        Array.Copy(LevelLoader.Instance.GetAllTeleportCoords(), 0, ignoreCoords, ignoreCoordsCopyStartIndex, LevelLoader.Instance.TeleportCount);
        ignoreCoordsCopyStartIndex = ignoreCoords.Length;

        // PickUP 아이템 생성
        itemCount = CurrentLevelSettings.HandlingCubeCount;
        if(itemCount > 0) {
            randomMax = coordCount / itemCount;
            randomMin = randomMax / 2;

            zoomInCoordIndex = 0;
            for(int i = 0; i < itemCount; i++) {
                zoomInCoordIndex += Random.Range(randomMin, randomMax);

                zoomInCoord.y = calculatedLevelSize.y - (zoomInCoordIndex / calculatedLevelSize.x + 1);
                zoomInCoord.x = zoomInCoordIndex % calculatedLevelSize.x;
                LevelLoader.Instance.AddPickupItemOnLevel(LevelLoader.ItemType.HandlingCube, zoomInCoord, zoom, ignoreCoords);

                zoomInCoordIndex++;
            }
        }
        Array.Resize(ref ignoreCoords, ignoreCoords.Length + LevelLoader.Instance.HandlingCubeCount);
        Array.Copy(LevelLoader.Instance.GetAllPickupItemCoords(), 0, ignoreCoords, ignoreCoordsCopyStartIndex, LevelLoader.Instance.HandlingCubeCount);
        ignoreCoordsCopyStartIndex = ignoreCoords.Length;

        // 몬스터 생성
        int monsterCount = CurrentLevelSettings.Monsters != null ? CurrentLevelSettings.Monsters.Sum(t => t.generateCount) : 0;
        if(monsterCount > 0) {
            randomMax = coordCount / monsterCount;
            randomMin = randomMax / 2;

            GameLevelSettings.MonsterStruct monsterStruct;
            zoomInCoordIndex = 0;
            for(int i = 0; i < CurrentLevelSettings.Monsters.Length; i++) {
                monsterStruct = CurrentLevelSettings.Monsters[i];
                for(int j = 0; j < monsterStruct.generateCount; j++) {
                    zoomInCoordIndex += Random.Range(randomMin, randomMax);

                    zoomInCoord.y = calculatedLevelSize.y - (zoomInCoordIndex / calculatedLevelSize.x + 1);
                    zoomInCoord.x = zoomInCoordIndex % calculatedLevelSize.x;
                    LevelLoader.Instance.AddMonsterOnLevel(monsterStruct.type, zoomInCoord, zoom, ignoreCoords);

                    zoomInCoordIndex++;
                }
            }
        }

        // StandingSpace 위치 설정
        standingSpaceCoord = new Vector2Int(Random.Range(1, CurrentLevelSettings.LevelWidth - 2), -1);
        standingSpaceController.transform.position =
            LevelLoader.Instance.GetBlockPos(standingSpaceCoord) +
            Vector3.forward * MazeBlock.BlockSize * 0.5f;

        // 플레이어 transform 초기화
        PlayerController.Instance.Pos = standingSpaceController.BlockCenter.transform.position;
        PlayerController.Instance.Forward = Vector3.forward;
        PlayerController.Instance.ResetCameraAnchor();
        PlayerController.Instance.ResetPlayerStatus();

        // Mic On
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = false;
            userInterface.MicGageActive = true;
        }

        // GameLevel 별로 세팅
        switch(UserSettings.GameLevel) {
            case 0: {
                    Vector3 npcPos = standingSpaceController.BlockLT.transform.position;
                    Vector3 npcForward = (PlayerController.Instance.Pos - npcPos).normalized;
                    standingSpaceController.InitializeNPCAnchor(npcPos, npcForward);
                    standingSpaceController.NPCActive = true;
                    standingSpaceController.NPCModelActive = true;

                    standingSpaceController.SetColor(Color.white * 0.85f, 5.0f);

                    if(toyHammer != null) {
                        Destroy(toyHammer.gameObject);
                        toyHammer = null;
                    }

                    yield return null;
                    PlayerController.Instance.IsPlaying = true;
                }
                break;
            case 1: {
                    Vector3 npcPos = standingSpaceController.BlockT.GetSidePos(MazeCreator.ActiveWall.F);
                    standingSpaceController.InitializeNPCAnchor(npcPos);
                    standingSpaceController.NPCActive = true;
                    standingSpaceController.NPCModelActive = false;

                    MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
                    MazeBlock animationBlock2 = standingSpaceController.BlockT;
                    StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, true, 0.0f));
                    StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, true, 0.0f));

                    standingSpaceController.SetColor(Color.white * 0.85f, 5.0f);

                    if(toyHammer != null) {
                        Destroy(toyHammer.gameObject);
                        toyHammer = null;
                    }

                    yield return null;
                    PlayerController.Instance.IsPlaying = true;
                }
                break;
            case 2: {
                    Vector3 npcPos = standingSpaceController.BlockT.transform.position;
                    Vector3 npcForward = (PlayerController.Instance.Pos - npcPos).normalized;
                    standingSpaceController.InitializeNPCAnchor(npcPos, npcForward);
                    standingSpaceController.NPCActive = true;
                    standingSpaceController.NPCModelActive = true;

                    MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
                    MazeBlock animationBlock2 = standingSpaceController.BlockT;
                    StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, false, 0.0f));
                    StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, false, 0.0f));

                    standingSpaceController.SetColor(Color.white * 0.85f, 5.0f);

                    if(toyHammer != null) {
                        Destroy(toyHammer.gameObject);
                        toyHammer = null;
                    }

                    yield return null;
                    PlayerController.Instance.IsPlaying = true;
                }
                break;
            case 3: {
                    Vector3 npcPos = standingSpaceController.BlockCenter.GetSidePos(MazeCreator.ActiveWall.F);
                    Vector3 npcForward = (PlayerController.Instance.Pos - npcPos).normalized;
                    standingSpaceController.InitializeNPCAnchor(npcPos, npcForward);
                    standingSpaceController.NPCActive = true;
                    standingSpaceController.NPCModelActive = true;

                    MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
                    MazeBlock animationBlock2 = standingSpaceController.BlockT;
                    StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, true, 0.0f));
                    StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, true, 0.0f));

                    standingSpaceController.SetColor(new Color(1.0f, 0.15f, 0.15f), 5.0f);

                    yield return null;
                    PlayerController.Instance.IsPlaying = true;

                    yield return new WaitForSeconds(0.2f);
                    PlayerController.Instance.OnEnteredNPCArea?.Invoke();
                }
                break;
        }

#if Skip_Scenario
        yield return null;

        PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(new Vector2Int(standingSpaceCoord.x, standingSpaceCoord.y + 1));
        PlayerController.Instance.Forward = Vector3.forward;

        LevelLoader.Instance.PlayMonsters();
        LevelLoader.Instance.PlayItems();
        LevelLoader.Instance.PlayPickupItems();

        OnScenarioEnd();
#endif

        initGameCoroutine = null;
    }

    private IEnumerator RestartGameCoroutine() {
        // Stop Objects
        LevelLoader.Instance.StopMonsters();
        LevelLoader.Instance.StopItems();
        LevelLoader.Instance.StopPickupItems();

        // Mic Off
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = true;
        }

        // Component Off
        pathGuide.gameObject.SetActive(false);

        // Sound 초기화
        SoundManager.Instance.ResetAllSoundObjects();

        yield return null;

        const int zoom = 2;
        Vector2Int calculatedLevelSize = LevelLoader.Instance.GetLevelSize(zoom);
        Vector2Int zoomInCoord = Vector2Int.zero;
        int coordCount = calculatedLevelSize.x * calculatedLevelSize.y;
        int randomMin, randomMax;
        int zoomInCoordIndex;

        // 미로의 가장 밑 줄(플레이어가 시작하는 곳)에는 아무것도 놓지 못하게 함.
        Vector2Int[] ignoreCoords = new Vector2Int[LevelLoader.Instance.LevelWidth];
        for(int i = 0; i < ignoreCoords.Length; i++) {
            ignoreCoords[i] = new Vector2Int(i, 0);
        }
        int ignoreCoordsCopyStartIndex = ignoreCoords.Length;

        // 아이템 재배치
        int itemCount = LevelLoader.Instance.ItemCount;
        //if(itemCount > 0) {
        //    randomMax = coordCount / itemCount;
        //    randomMin = randomMax / 2;

        //    zoomInCoordIndex = 0;
        //    for(int i = 0; i < itemCount; i++) {
        //        zoomInCoordIndex += Random.Range(randomMin, randomMax);

        //        zoomInCoord.y = calculatedLevelSize.y - (zoomInCoordIndex / calculatedLevelSize.x + 1);
        //        zoomInCoord.x = zoomInCoordIndex % calculatedLevelSize.x;
        //        LevelLoader.Instance.ResetItemOnLevel(i, zoomInCoord, zoom, ignoreCoords);

        //        zoomInCoordIndex++;
        //    }
        //}
        //Array.Resize(ref ignoreCoords, ignoreCoords.Length + LevelLoader.Instance.ItemCount);
        //Array.Copy(LevelLoader.Instance.GetAllItemCoords(), 0, ignoreCoords, ignoreCoordsCopyStartIndex, LevelLoader.Instance.ItemCount);
        //ignoreCoordsCopyStartIndex = ignoreCoords.Length;

        // Teleport 재배치
        itemCount = LevelLoader.Instance.TeleportCount;
        if(itemCount > 0) {
            randomMax = coordCount / itemCount;
            randomMin = randomMax / 2;

            zoomInCoordIndex = 0;
            for(int i = 0; i < itemCount; i++) {
                zoomInCoordIndex += Random.Range(randomMin, randomMax);

                zoomInCoord.y = calculatedLevelSize.y - (zoomInCoordIndex / calculatedLevelSize.x + 1);
                zoomInCoord.x = zoomInCoordIndex % calculatedLevelSize.x;
                LevelLoader.Instance.ResetTeleportOnLevel(i, zoomInCoord, zoom, ignoreCoords);

                zoomInCoordIndex++;
            }
        }
        Array.Resize(ref ignoreCoords, ignoreCoords.Length + LevelLoader.Instance.TeleportCount);
        Array.Copy(LevelLoader.Instance.GetAllTeleportCoords(), 0, ignoreCoords, ignoreCoordsCopyStartIndex, LevelLoader.Instance.TeleportCount);
        ignoreCoordsCopyStartIndex = ignoreCoords.Length;

        // PickUP 아이템 재배치
        itemCount = LevelLoader.Instance.HandlingCubeCount;
        if(itemCount > 0) {
            randomMax = coordCount / itemCount;
            randomMin = randomMax / 2;

            zoomInCoordIndex = 0;
            for(int i = 0; i < itemCount; i++) {
                zoomInCoordIndex += Random.Range(randomMin, randomMax);

                zoomInCoord.y = calculatedLevelSize.y - (zoomInCoordIndex / calculatedLevelSize.x + 1);
                zoomInCoord.x = zoomInCoordIndex % calculatedLevelSize.x;
                LevelLoader.Instance.ResetPickupItemOnLevel(i, zoomInCoord, zoom, ignoreCoords);

                zoomInCoordIndex++;
            }
        }
        Array.Resize(ref ignoreCoords, ignoreCoords.Length + LevelLoader.Instance.HandlingCubeCount);
        Array.Copy(LevelLoader.Instance.GetAllPickupItemCoords(), 0, ignoreCoords, ignoreCoordsCopyStartIndex, LevelLoader.Instance.HandlingCubeCount);
        ignoreCoordsCopyStartIndex = ignoreCoords.Length;

        // 몬스터 재배치
        int monsterCount = LevelLoader.Instance.MonsterCount;
        if(monsterCount > 0) {
            randomMax = coordCount / monsterCount;
            randomMin = randomMax / 2;

            zoomInCoordIndex = 0;
            for(int i = 0; i < monsterCount; i++) {
                zoomInCoordIndex += Random.Range(randomMin, randomMax);

                zoomInCoord.y = calculatedLevelSize.y - (zoomInCoordIndex / calculatedLevelSize.x + 1);
                zoomInCoord.x = zoomInCoordIndex % calculatedLevelSize.x;
                LevelLoader.Instance.ResetMonsterOnLevel(i, zoomInCoord, zoom, ignoreCoords);

                zoomInCoordIndex++;
            }
        }

        // StandingSpace 위치 설정
        //standingSpaceCoord = new Vector2Int(Random.Range(1, CurrentLevelSettings.LevelWidth - 2), -1);
        //standingSpaceController.transform.position =
        //    LevelLoader.Instance.GetBlockPos(standingSpaceCoord) +
        //    Vector3.forward * MazeBlock.BlockSize * 0.5f;

        // 플레이어 초기화
        PlayerController.Instance.Pos = standingSpaceController.BlockT.transform.position + Vector3.forward * MazeBlock.BlockSize;
        PlayerController.Instance.Forward = Vector3.forward;
        PlayerController.Instance.ResetCameraAnchor();
        PlayerController.Instance.ResetPlayerStatus();
        if(toyHammer != null) {
            PlayerController.Instance.SetPickupItem(toyHammer);
            toyHammer.gameObject.SetActive(true);
        }

        yield return StartCoroutine(UtilObjects.Instance.SetActiveLoadingAction(true, 0.5f));
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(UtilObjects.Instance.SetActiveLoadingAction(false, 0.5f));

        PlayerController.Instance.IsPlaying = true;

        // Mic On
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = false;
            userInterface.MicGageActive = true;
        }

        yield return StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(false, 2.0f));

        OnScenarioEnd();

        initGameCoroutine = null;
    }

    #region Utility
    public void StartScenario() {
        if(scenarioCoroutine == null) {
            switch(UserSettings.GameLevel) {
                case 0: scenarioCoroutine = Scenario00(); break;
                case 1: scenarioCoroutine = Scenario01(); break;
                case 2: scenarioCoroutine = Scenario02(); break;
                case 3: scenarioCoroutine = Scenario03(); break;
            }

            if(scenarioCoroutine != null) StartCoroutine(scenarioCoroutine);
        }
    }
    #endregion

    #region Action
    private void OnPlayerCatched(MonsterController monster) {
        if(onPlayerCatchedCoroutine == null) {
            onPlayerCatchedCoroutine = OnPlayerCatchedCoroutine(monster);
            StartCoroutine(onPlayerCatchedCoroutine);
        }
    }

    private IEnumerator OnPlayerCatchedCoroutine(MonsterController monster) {
        LevelLoader.Instance.StopMonsters();
        LevelLoader.Instance.StopItems();
        LevelLoader.Instance.StopPickupItems();

        PlayerController.Instance.DropPickupItem();
        PlayerController.Instance.IsPlaying = false;

        monster.PlayerCatchAnimation();
        yield return new WaitForSeconds(0.9f);

        yield return UtilObjects.Instance.SetActiveRayBlockAction(true, 0.5f);

        OnGameEnd(false);

        onPlayerCatchedCoroutine = null;
    }

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
        standingSpaceController.NPCActive = false;
        standingSpaceController.gameObject.SetActive(true);

        // StandingSpace 위치 설정
        standingSpaceCoord = new Vector2Int(Random.Range(1, CurrentLevelSettings.LevelWidth - 2), -1);
        standingSpaceController.transform.position =
            LevelLoader.Instance.GetBlockPos(standingSpaceCoord) +
            Vector3.forward * MazeBlock.BlockSize * 0.5f;

        // Open Door
        MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        MazeBlock animationBlock2 = standingSpaceController.BlockT;

        const float m_standardWallAnimationTime = 3.0f;
        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, true, m_standardWallAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, true, m_standardWallAnimationTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        // Set Head Message
        userInterface.SetHeadMessage("ExitOpened", 10.0f);

        // Start Guide
        pathGuide.gameObject.SetActive(true);
        pathGuide.transform.position = PlayerController.Instance.Pos;
        pathGuide.GuideStart(LevelLoader.Instance.GetBlockPos(new Vector2Int(standingSpaceCoord.x, standingSpaceCoord.y + 1)));

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

        OnExitEntered();

        exitEnterCheckCoroutine = null;
    }

    #region Scenario
    private IEnumerator Scenario00() {
        // Mic Off
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = true;
            userInterface.MicGageActive = false;
        }

        // Stop Player
        PlayerController.Instance.IsPlaying = false;

        // Camera Move
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = standingSpaceController.NPCCameraViewPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = standingSpaceController.NPCCameraViewRotation;
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
        userInterface.SetMessageButtons("Yes", () => { moveScenario = 1; }, "No", () => { moveScenario = -1; });
        UserSettings.OnLocaleChanged += OnLocaleChanged;
        while(moveScenario == 0) {
            yield return null;
        }
        UserSettings.OnLocaleChanged -= OnLocaleChanged;
        #endregion

        // Special Scenario 진입
        if(moveScenario == -1) {
            const string SpecialScenarioKeyString = ScenarioStandardKeyString + "_Special";
            int specialScenarioIndex = 0;
            string specialScenarioKey;
            IEnumerator TextAnimationWithScenarioButtons(string key = null) {
                // Reset Buttons
                userInterface.SetMessageButtons();
                userInterface.SetScenarioButtons();

                // String Animation
                specialScenarioKey = string.IsNullOrEmpty(key) ? SpecialScenarioKeyString + specialScenarioIndex.ToString().PadLeft(2, '0') : key;
                scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(specialScenarioKey);
                yield return StartCoroutine(scenarioTextAnimationCoroutine);

                moveScenario = 0;
                switch(specialScenarioIndex) {
                    case 0:
                    case 3:
                        userInterface.SetScenarioButtons(null, () => { moveScenario = 1; }); 
                        break;
                    //case turningPointIndex - 1: userInterface.SetScenarioButtons(() => { moveScenario = -1; }, null); break;
                    default: userInterface.SetScenarioButtons(() => { moveScenario = -1; }, () => { moveScenario = 1; }); break;
                }
                UserSettings.OnLocaleChanged += OnLocaleChanged;
                while(moveScenario == 0) {
                    yield return null;
                }
                UserSettings.OnLocaleChanged -= OnLocaleChanged;

                specialScenarioIndex += moveScenario;
            }
            IEnumerator TextAnimationWithMessageButtons() {
                // Reset Buttons
                userInterface.SetMessageButtons();
                userInterface.SetScenarioButtons();

                // String Animation
                specialScenarioKey = SpecialScenarioKeyString + specialScenarioIndex.ToString().PadLeft(2, '0');
                scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(specialScenarioKey);
                yield return StartCoroutine(scenarioTextAnimationCoroutine);

                // 0: 대기
                // -1: Special Scenario 진입
                // 1: 통상 궤도 진입
                moveScenario = 0;
                userInterface.SetMessageButtons("Yes", () => { moveScenario = 1; }, "No", () => { moveScenario = -1; });
                UserSettings.OnLocaleChanged += OnLocaleChanged;
                while(moveScenario == 0) {
                    yield return null;
                }
                UserSettings.OnLocaleChanged -= OnLocaleChanged;
            }

            yield return StartCoroutine(TextAnimationWithScenarioButtons()); // 00
            yield return StartCoroutine(TextAnimationWithMessageButtons()); // 01

            if(moveScenario != 1) {
                specialScenarioIndex++;
                yield return StartCoroutine(TextAnimationWithMessageButtons()); // 02

                if(moveScenario != 1) {
                    specialScenarioIndex++;
                    while(specialScenarioIndex < 5) {
                        yield return StartCoroutine(TextAnimationWithScenarioButtons());
                    }

                    // 계속해서 거절당해 메인으로 돌아감
                    SceneLoader.Instance.LoadScene(SceneLoader.SceneType.Main);
                    yield break;
                }
            }

            specialScenarioIndex = 0;
            yield return StartCoroutine(TextAnimationWithScenarioButtons(SpecialScenarioKeyString + "Exit")); // Exit
        }

        #region Door Open Animation
        // Hide MessageBox
        userInterface.MessageAlpha = 0.0f;

        // Open Door
        MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        MazeBlock animationBlock2 = standingSpaceController.BlockT;

        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, true, m_standardWallAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, true, m_standardWallAnimationTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        // Camera Rotation ==> Show Door Opening
        float cameraLookStartHeight = MazeBlock.WallHeight - 1.0f;
        float cameraLookEndHeight = 1.0f;
        Vector3 animationWallPos = animationBlock2.GetSidePos(MazeCreator.ActiveWall.F);

        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camLookAtStartPos = animationWallPos + Vector3.up * cameraLookStartHeight;
        m_camLookAtEndPos = animationWallPos + Vector3.up * cameraLookEndHeight;
        scenarioCameraAnimationCoroutine = CameraLookAtAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Delay
        yield return new WaitForSeconds(1.0f);

        // Camera View 복구
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = UtilObjects.Instance.CamPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = standingSpaceController.NPCCameraViewRotation;
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

        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = PlayerController.Instance.CamPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = PlayerController.Instance.CamRotation;
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

        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = normalCheckPos + Vector3.forward * MazeBlock.BlockSize;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = Quaternion.LookRotation((normalCheckPos - m_camEndPos).normalized);
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Wall Animation
        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, false, m_standardWallAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, false, m_standardWallAnimationTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        // Camera Animation
        cameraLookStartHeight = 1.0f;
        cameraLookEndHeight = MazeBlock.WallHeight - 1.0f;

        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camLookAtStartPos = animationWallPos + Vector3.up * cameraLookStartHeight;
        m_camLookAtEndPos = animationWallPos + Vector3.up * cameraLookEndHeight;
        scenarioCameraAnimationCoroutine = CameraLookAtAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Delay
        yield return new WaitForSeconds(1.0f);

        // Camera 복구
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = PlayerController.Instance.CamPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = PlayerController.Instance.CamRotation;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // 플레이어 움직임 복구
        PlayerController.Instance.IsPlaying = true;

        // Mic On
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = false;
            userInterface.MicGageActive = true;
        }

        OnScenarioEnd();

        scenarioCoroutine = null;
    }

    private IEnumerator Scenario01() {
        // Mic Off
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = true;
            userInterface.MicGageActive = false;
        }

        // Stop Player
        PlayerController.Instance.IsPlaying = false;

        // Set NPC Pos
        Vector3 npcPos = standingSpaceController.BlockCenter.transform.position;
        standingSpaceController.InitializeNPCAnchor(npcPos, Vector3.forward);
        standingSpaceController.NPCModelActive = true;

        // Camera Move
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = standingSpaceController.NPCCameraViewPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = standingSpaceController.NPCCameraViewRotation;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Active MessageBox
        userInterface.MessageActive = true;
        userInterface.MessageAlpha = 1.0f;

        // Scenario Start
        const string ScenarioStandardKeyString = "LevelStart01";
        string key = "";
        void OnLocaleChanged(string languageCode) {
            Locale currentLocale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
            StringTable currentTable = LocalizationSettings.StringDatabase.GetTable("Scenario", currentLocale);
            StringTableEntry currentTableEntry = currentTable.GetEntry(key);
            userInterface.MessageText = currentTableEntry.Value;
        }

        int scenarioIndex = 0;
        const int scenarioEndIndex = 10;
        const int specialIndex = 8;
        bool isShowingSpecialIndex = false;

        // 0: 대기
        // -1: 이전으로
        // 1: 다음으로
        int moveScenario = 0;
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
            // 완벽하지는 않지만 이거라도 하는게 좋아 보인다.
            standingSpaceController.SetAnimationResetTrigger_Talking();

            if(!isShowingSpecialIndex && scenarioIndex == specialIndex) {


                moveScenario = 1;
                isShowingSpecialIndex = true;
            }
            else {
                moveScenario = 0;
                switch(scenarioIndex) {
                    case 0: userInterface.SetScenarioButtons(null, () => { moveScenario = 1; }); break;
                    default: userInterface.SetScenarioButtons(() => { moveScenario = -1; }, () => { moveScenario = 1; }); break;
                }
                UserSettings.OnLocaleChanged += OnLocaleChanged;
                while(moveScenario == 0) {
                    yield return null;
                }
                UserSettings.OnLocaleChanged -= OnLocaleChanged;
            }

            scenarioIndex += moveScenario;
            if(isShowingSpecialIndex && scenarioIndex == specialIndex) {
                scenarioIndex += moveScenario;
            }
        }

        // Hide MessageBox
        userInterface.MessageAlpha = 0.0f;

        // Teleport Player
        PlayerController.Instance.Pos = standingSpaceController.BlockT.transform.position;
        PlayerController.Instance.Forward = Vector3.forward;
        PlayerController.Instance.ResetCameraAnchor();

        // Camera Move
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = PlayerController.Instance.CamPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = PlayerController.Instance.CamRotation;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Turn Off NPC
        standingSpaceController.NPCActive = false;

        // Player On
        PlayerController.Instance.IsPlaying = true;

        // 플레이어의 진입을 감지하기 위한 루프문
        Vector3 animationWallPos = standingSpaceController.BlockT.GetSidePos(MazeCreator.ActiveWall.F);
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

        // Wall Animation
        MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        MazeBlock animationBlock2 = standingSpaceController.BlockT;
        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, false, m_standardWallAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, false, m_standardWallAnimationTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        yield return new WaitForSeconds(m_standardWallAnimationTime);

        // Mic On
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = false;
            userInterface.MicGageActive = true;
        }

        OnScenarioEnd();

        scenarioCoroutine = null;
    }

    private IEnumerator Scenario02() {
        // Mic Off
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = true;
            userInterface.MicGageActive = false;
        }

        // Stop Player
        PlayerController.Instance.IsPlaying = false;

        // Camera Move
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = standingSpaceController.NPCCameraViewPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = standingSpaceController.NPCCameraViewRotation;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine(1.5f);
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Active MessageBox
        userInterface.MessageActive = true;
        userInterface.MessageAlpha = 1.0f;

        // Scenario Start
        const string ScenarioStandardKeyString = "LevelStart02";
        string key = "";
        void OnLocaleChanged(string languageCode) {
            Locale currentLocale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
            StringTable currentTable = LocalizationSettings.StringDatabase.GetTable("Scenario", currentLocale);
            StringTableEntry currentTableEntry = currentTable.GetEntry(key);
            userInterface.MessageText = currentTableEntry.Value;
        }

        int scenarioIndex = 0;
        const int turningPointIndex = 3;

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
            scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(key, 0.02f);
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

        // NPC Animation Start
        const float throwStartTime = 90f / 60f;
        const float animationEndTime = 160f / 60f;

        const float firstMoveStartTime = 0.0f;
        const float firstRotateStartTime = 45f / 60f;
        const float secondMoveStartTime = 65f / 60f;

        const float firstMoveTimeLength = firstRotateStartTime - firstMoveStartTime;
        const float firstRotateTimeLength = secondMoveStartTime - firstRotateStartTime;
        const float secondMoveTimeLength = animationEndTime - secondMoveStartTime;

        // Trigger On
        standingSpaceController.SetAnimationTrigger_Throw();
        // NPC Move
        standingSpaceController.StartNPCMoveAnimation(
            firstMoveTimeLength, 
            standingSpaceController.NPCPos + standingSpaceController.NPCForward);
        // NPC Rotate
        standingSpaceController.StartNPCRotateAnimation(
            firstRotateTimeLength,
            Quaternion.LookRotation(Vector3.forward),
            firstMoveTimeLength);
        // NPC Move
        standingSpaceController.StartNPCMoveAnimation(
            secondMoveTimeLength,
            standingSpaceController.NPCPos + standingSpaceController.NPCForward,
            firstMoveTimeLength + firstRotateTimeLength);

        // Wall Animation
        MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        MazeBlock animationBlock2 = standingSpaceController.BlockT;
        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, true, throwStartTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, true, throwStartTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        // Camera Move
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = standingSpaceController.NPCCameraViewRotation;
        float timeChecker = 0.0f;
        float lerpRatio;
        while(timeChecker < throwStartTime) {
            timeChecker += Time.deltaTime;
            lerpRatio = Time.deltaTime * 4;

            m_camStartPos = UtilObjects.Instance.CamPos;
            m_camEndPos = standingSpaceController.NPCCameraViewPos + 
                standingSpaceController.NPCCameraViewForward +
                standingSpaceController.NPCCameraViewUp * 0.5f;
            UtilObjects.Instance.CamPos = Vector3.Lerp(m_camStartPos, m_camEndPos, lerpRatio);

            m_camStartRotation = UtilObjects.Instance.CamRotation;
            m_camEndRotation = Quaternion.LookRotation(standingSpaceController.NPCCameraViewForward);
            UtilObjects.Instance.CamRotation = Quaternion.Lerp(m_camStartRotation, m_camEndRotation, lerpRatio);

            yield return null;
        }

        // Throw Camera Animation
        Vector3 animationWallPos = standingSpaceController.BlockT.GetSidePos(MazeCreator.ActiveWall.F);
        m_camEndPos = animationWallPos + Vector3.forward * MazeBlock.BlockSize * 0.5f + Vector3.up * 0.1f;
        timeChecker = 0.0f;
        while(timeChecker < animationEndTime - throwStartTime) {
            timeChecker += Time.deltaTime;
            lerpRatio = Time.deltaTime * 4;

            m_camStartPos = UtilObjects.Instance.CamPos;
            UtilObjects.Instance.CamPos = Vector3.Lerp(m_camStartPos, m_camEndPos, lerpRatio);

            m_camStartRotation = UtilObjects.Instance.CamRotation;
            m_camLookAtEndPos = standingSpaceController.NPCPos + Vector3.up * 2;
            m_camEndRotation = Quaternion.LookRotation((m_camLookAtEndPos - UtilObjects.Instance.CamPos).normalized);
            UtilObjects.Instance.CamRotation = Quaternion.Lerp(m_camStartRotation, m_camEndRotation, lerpRatio);

            yield return null;
        }

        // Set Player
        PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        PlayerController.Instance.Forward = (standingSpaceController.NPCPos - m_camEndPos).normalized;
        PlayerController.Instance.ResetCameraAnchor();

        // Delay
        yield return new WaitForSeconds(0.2f);

        // Wall Animation
        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, false, m_standardWallAnimationTime));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, false, m_standardWallAnimationTime));
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.WallAnimation);

        // Camera Animation
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = PlayerController.Instance.CamPos;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = PlayerController.Instance.CamRotation;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine();
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Camera Animation
        //float cameraLookStartHeight = 1.0f;
        //float cameraLookEndHeight = MazeBlock.WallHeight - 1.0f;

        //m_camStartRotation = UtilObjects.Instance.CamRotation;
        //m_camLookAtStartPos = animationWallPos + Vector3.up * cameraLookStartHeight;
        //m_camLookAtEndPos = animationWallPos + Vector3.up * cameraLookEndHeight;
        //scenarioCameraAnimationCoroutine = CameraLookAtAnimationCoroutine();
        //yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // 플레이어 움직임 복구
        PlayerController.Instance.IsPlaying = true;

        // Mic On
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = false;
            userInterface.MicGageActive = true;
        }

        OnScenarioEnd();

        scenarioCoroutine = null;
    }

    private IEnumerator Scenario03() {
        // Mic Off
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = true;
            userInterface.MicGageActive = false;
        }

        // Stop Player
        PlayerController.Instance.IsPlaying = false;

        // Camera Move
        m_camStartPos = UtilObjects.Instance.CamPos;
        m_camEndPos = standingSpaceController.NPCCameraViewPos + standingSpaceController.NPCCameraViewForward;
        m_camStartRotation = UtilObjects.Instance.CamRotation;
        m_camEndRotation = standingSpaceController.NPCCameraViewRotation;
        scenarioCameraAnimationCoroutine = CameraMoveRotateAnimationCoroutine(1.5f);
        yield return StartCoroutine(scenarioCameraAnimationCoroutine);

        // Active MessageBox
        userInterface.MessageActive = true;
        userInterface.MessageAlpha = 1.0f;

        // Scenario Start
        const string ScenarioStandardKeyString = "LevelStart03";
        string key = "";
        void OnLocaleChanged(string languageCode) {
            Locale currentLocale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
            StringTable currentTable = LocalizationSettings.StringDatabase.GetTable("Scenario", currentLocale);
            StringTableEntry currentTableEntry = currentTable.GetEntry(key);
            userInterface.MessageText = currentTableEntry.Value;
        }

        int scenarioIndex = 0;
        const int turningPointIndex = 7;

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
            scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(key, 0.02f);
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
        userInterface.SetMessageButtons("Resist", () => { moveScenario = 1; }, "Accept", () => { moveScenario = -1; });
        UserSettings.OnLocaleChanged += OnLocaleChanged;
        while(moveScenario == 0) {
            yield return null;
        }
        UserSettings.OnLocaleChanged -= OnLocaleChanged;
        #endregion

        // Turning Point Setting
        bool isBadEnding = moveScenario == -1;
        string NewScenarioStandardKeyString = ScenarioStandardKeyString + (isBadEnding ? "_Accept" : "_Resist");
        int scenarioEndIndex = isBadEnding ? 3 : 4;

        // 0: 대기
        // -1: 이전으로
        // 1: 다음으로
        moveScenario = 0;
        scenarioIndex = 0;
        while(scenarioIndex < scenarioEndIndex) {
            // Reset Buttons
            userInterface.SetMessageButtons();
            userInterface.SetScenarioButtons();

            // 추임새
            standingSpaceController.SetAnimationTrigger_Talking();

            // String Animation
            key = NewScenarioStandardKeyString + scenarioIndex.ToString().PadLeft(2, '0');
            scenarioTextAnimationCoroutine = ScenarioTextAnimationCoroutine(key, 0.02f);
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

        // Fade Out
        yield return StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(true, 2.0f));

        // Hide MessageBox
        userInterface.MessageAlpha = 0.0f;// Close MessageBox

        // Player Setting
        PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        PlayerController.Instance.Forward = Vector3.forward;
        PlayerController.Instance.ResetCameraAnchor();
        PlayerController.Instance.CheatMode = !isBadEnding;

        // Camera Setting
        UtilObjects.Instance.CamPos = PlayerController.Instance.CamPos;
        UtilObjects.Instance.CamForward = PlayerController.Instance.CamForward;

        // Close Door
        MazeBlock animationBlock1 = LevelLoader.Instance.GetMazeBlock(standingSpaceCoord.x, standingSpaceCoord.y + 1);
        MazeBlock animationBlock2 = standingSpaceController.BlockT;
        StartCoroutine(animationBlock1.WallAnimation(MazeCreator.ActiveWall.B, false, 0.0f));
        StartCoroutine(animationBlock2.WallAnimation(MazeCreator.ActiveWall.F, false, 0.0f));

        // Delay
        yield return new WaitForSeconds(1.0f);

        // 플레이어 움직임 복구
        PlayerController.Instance.IsPlaying = true;

        // Mic On
        if(UserSettings.UseMicBoolean) {
            MicrophoneRecorder.Instance.IsMute = false;
            userInterface.MicGageActive = true;
        }

        OnScenarioEnd();

        // Happy Ending
        if(!isBadEnding) {
            if(toyHammer == null) {
                GameObject resource = ResourceLoader.GetResource<GameObject>(LevelLoader.ROOT_PATH_OF_ITEMS + "/ToyHammer");
                GameObject go = Instantiate(resource, PlayerController.Instance.PickupHandAnchor);

                ToyHammer th = go.GetComponent<ToyHammer>();
                th.Stop();

                toyHammer = th;
            }

            PlayerController.Instance.SetPickupItem(toyHammer);
            toyHammer.gameObject.SetActive(true);
        }
        // Bad Ending
        else {
            if(toyHammer != null) {
                toyHammer.gameObject.SetActive(false);
            }
        }

        // Fade In
        yield return UtilObjects.Instance.SetActiveRayBlockAction(false, 1.0f);

        scenarioCoroutine = null;
    }

    #region Help Func
    const float m_standardWallAnimationTime = 3.0f;
    const float m_standardCameraMoveTime = 3.0f;

    Vector3 m_camStartPos, m_camEndPos;
    Vector3 m_camLookAtStartPos, m_camLookAtEndPos;
    Quaternion m_camStartRotation, m_camEndRotation;

    IEnumerator CameraMoveRotateAnimationCoroutine(float animationTime = m_standardCameraMoveTime) {
        float timeChecker = 0.0f;
        float timeRatio = 0.0f;
        float lerpRatio = 0.0f;
        while(timeChecker < animationTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / animationTime;
            lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
            UtilObjects.Instance.CamPos = Vector3.Lerp(m_camStartPos, m_camEndPos, lerpRatio);
            UtilObjects.Instance.CamRotation = Quaternion.Lerp(m_camStartRotation, m_camEndRotation, lerpRatio);

            yield return null;
        }

        scenarioCameraAnimationCoroutine = null;
    }

    IEnumerator CameraLookAtAnimationCoroutine(float animationTime = m_standardCameraMoveTime) {
        Vector3 lookAtPos;

        float timeChecker = 0.0f;
        float timeRatio = 0.0f;
        float lerpRatio = 0.0f;
        while(timeChecker < animationTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / animationTime;
            lerpRatio = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);
            lookAtPos = Vector3.Lerp(m_camLookAtStartPos, m_camLookAtEndPos, lerpRatio);
            m_camEndRotation = Quaternion.LookRotation((lookAtPos - UtilObjects.Instance.CamPos).normalized);
            UtilObjects.Instance.CamRotation = Quaternion.Lerp(m_camStartRotation, m_camEndRotation, lerpRatio);

            yield return null;
        }

        scenarioCameraAnimationCoroutine = null;
    }

    private IEnumerator ScenarioTextAnimationCoroutine(string key, float waitTime = 0.05f) {
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

        WaitForSeconds animationWait = new WaitForSeconds(waitTime);
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

    private IEnumerator ScenarioTextAnimationWithStringCoroutine(string text, float waitTime = 0.05f) {
        string animationText = "";
        int textCopyLength = 0;

        userInterface.SetMessageBoxButton(() => {
            textCopyLength = text.Length;
        });

        WaitForSeconds animationWait = new WaitForSeconds(waitTime);
        while(textCopyLength <= text.Length) {
            animationText = text[0..textCopyLength];
            userInterface.MessageText = animationText;

            textCopyLength++;
            yield return animationWait;
        }

        userInterface.SetMessageBoxButton();

        scenarioTextAnimationCoroutine = null;
    }
    #endregion
    #endregion

    private void OnGameEnd(bool isClear) {
        if(onGameEndCoroutine == null) {
            onGameEndCoroutine = OnGameEndCoroutine(isClear);
            StartCoroutine(onGameEndCoroutine);
        }
    }

    private IEnumerator OnGameEndCoroutine(bool isClear) {
        // Exit로 탈출한 경우
        if(isClear) {
            // Level 증가
            UserSettings.GameLevel++;

            // 오브젝트 Stop
            LevelLoader.Instance.StopMonsters();
            LevelLoader.Instance.StopItems();
            LevelLoader.Instance.StopPickupItems();
            PlayerController.Instance.IsPlaying = false;

            // BGM Off
            SoundManager.Instance.StopBGM(SoundManager.SoundType.Game, 1.0f);

            // Display Fade Out
            yield return StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(true, 1.0f, Color.white));

            // Display Color to Black
            StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(true, 0.0f, Color.black));

            // Fake Load Start
            StartCoroutine(UtilObjects.Instance.SetActiveLoadingAction(true, 0.0f));

            // Init Level
            if(initGameCoroutine == null) {
                initGameCoroutine = InitGameCoroutine();
                StartCoroutine(initGameCoroutine);
            }

            // Fake Load
            yield return new WaitForSeconds(1.0f);

            // Display Fade In
            StartCoroutine(UtilObjects.Instance.SetActiveLoadingAction(false, 1.0f));
            yield return StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(false, 1.0f));
        }
        // 몬스터에게 잡힌 경우
        else {
            if(initGameCoroutine == null) {
                initGameCoroutine = RestartGameCoroutine();
                StartCoroutine(initGameCoroutine);
            }
        }

        onGameEndCoroutine = null;
    }

    private void OnExitEntered() {


        OnGameEnd(true);
    }

    private void OnScenarioEnd() {
        SoundManager.Instance.PlayBGM(SoundManager.SoundType.Game, 5.0f, 0.3f);

        standingSpaceController.gameObject.SetActive(false);

        LevelLoader.Instance.PlayMonsters();
        LevelLoader.Instance.PlayItems();
        LevelLoader.Instance.PlayPickupItems();

        int maxItemCount = CurrentLevelSettings.Items != null ? CurrentLevelSettings.Items.Sum(t => t.generateCount) : 0;
        int collectItem = maxItemCount - LevelLoader.Instance.ItemCount;
        userInterface.SetItemCount(maxItemCount, collectItem);

        userInterface.HeadMessageActive = true;
        userInterface.MessageActive = false;
        userInterface.MicGageActive = UserSettings.UseMicBoolean;
        userInterface.CollectItemActive = maxItemCount > 0;
        userInterface.RunGageActive = true;
    }
}

public class GameLevelSettings {
    public int LevelWidth;
    public int LevelHeight;

    public bool IsEmpty;

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

    public int HandlingCubeCount;
    public int TeleportCount;
}
