using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

public class LevelLoader : GenericSingleton<LevelLoader> {
    #region Monter
    public enum MonsterType {
        Bunny,
        Honey,
        Froggy, 
        Cloudy, 
        Starry, 
        Kitty, 

    }

    private const string ROOT_PATH_OF_MONSTERS = "Monsters";

    private Dictionary<MonsterType, GameObject> monsterResources = new Dictionary<MonsterType, GameObject>();

    private List<MonsterController> monsters = new List<MonsterController>();
    public List<MonsterController> Monsters { get { return monsters; } }
    public int MonsterCount { get { return monsters.Count; } }
    #endregion

    #region Item
    public enum ItemType {
        Crystal,

    }

    private const string ROOT_PATH_OF_ITEMS = "Items";

    private List<ItemController> items = new List<ItemController>();
    public List<ItemController> Items { get { return items; } }
    public int ItemCount { get { return items.Count; } }
    #endregion

    #region Maze
    private MazeBlock[,] mazeBlocks = null;
    public int LevelWidth { get; private set; }
    public int LevelHeight { get; private set; }

    private readonly Vector3 basicBlockPos = new Vector3(1.0f, 0.0f, 1.0f) * MazeBlock.BlockSize;
    private readonly Vector3 blockPosOffset = MazeBlock.StandardBlockAnchor * MazeBlock.BlockSize;

    private Material blockFloorMaterial = null;
#if Use_Two_Materials_On_MazeBlock
    private Material blockWallMaterial = null;
#endif
    private static readonly int LIST_MAX_LENGTH = 256;

    #region Material Rim Properties
    // Rim property는 SoundObject를 활용하여 계산하고 있기 때문에 SoundManager에 의존하고 있음
    private const string MAT_RIM_COLOR_NAME = "_RimColor";
    private const string MAT_RIM_ARRAY_LENGTH_NAME = "_RimArrayLength";
    private const string MAT_RIM_POSITION_ARRAY_NAME = "_RimPosArray";
    private const string MAT_RIM_RADIUS_ARRAY_NAME = "_RimRadiusArray";
    private const string MAT_RIM_ALPHA_ARRAY_NAME = "_RimAlphaArray";
    private MaterialPropertiesGroup[] rimMaterialPropertiesGroups = new MaterialPropertiesGroup[] {
        new MaterialPropertiesGroup(
            SoundManager.SoundFrom.None,
            MAT_RIM_COLOR_NAME + "_None",
            MAT_RIM_ARRAY_LENGTH_NAME + "_None",
            MAT_RIM_POSITION_ARRAY_NAME + "_None",
            MAT_RIM_RADIUS_ARRAY_NAME + "_None",
            MAT_RIM_ALPHA_ARRAY_NAME + "_None",
            Color.white),
        new MaterialPropertiesGroup(
            SoundManager.SoundFrom.Player,
            MAT_RIM_COLOR_NAME + "_Player",
            MAT_RIM_ARRAY_LENGTH_NAME + "_Player",
            MAT_RIM_POSITION_ARRAY_NAME + "_Player",
            MAT_RIM_RADIUS_ARRAY_NAME + "_Player",
            MAT_RIM_ALPHA_ARRAY_NAME + "_Player",
            Color.white),
        new MaterialPropertiesGroup(
            SoundManager.SoundFrom.Monster,
            MAT_RIM_COLOR_NAME + "_Monster",
            MAT_RIM_ARRAY_LENGTH_NAME + "_Monster",
            MAT_RIM_POSITION_ARRAY_NAME + "_Monster",
            MAT_RIM_RADIUS_ARRAY_NAME + "_Monster",
            MAT_RIM_ALPHA_ARRAY_NAME + "_Monster",
            Color.red),
        new MaterialPropertiesGroup(
            SoundManager.SoundFrom.Item,
            MAT_RIM_COLOR_NAME + "_Item",
            MAT_RIM_ARRAY_LENGTH_NAME + "_Item",
            MAT_RIM_POSITION_ARRAY_NAME + "_Item",
            MAT_RIM_RADIUS_ARRAY_NAME + "_Item",
            MAT_RIM_ALPHA_ARRAY_NAME + "_Item",
            new Color(0.35f, 1.0f, 0.25f))
    };

    #endregion

    #region Material Player Properties
    // Player past pos property는 SoundObject를 활용하지 않기 때문에 LevelLoader에서 직접 계산함
    private const string MAT_PLAYER_PAST_POSITION_COLOR_NAME = "_PlayerPastPosColor";
    private const string MAT_PLAYER_PAST_POSITION_ARRAY_LENGTH_NAME = "_PlayerPastPosArrayLength";
    private const string MAT_PLAYER_PAST_POSITION_ARRAY_NAME = "_PlayerPastPosArray";
    private const string MAT_PLAYER_PAST_POSITION_ALPHA_ARRAY_NAME = "_PlayerPastPosAlphaArray";
    private MaterialPropertiesGroup playerMaterialPropertiesGroup = new MaterialPropertiesGroup(
        SoundManager.SoundFrom.None, //None을 적용했지만 SoundObject를 사용하지는 않음
        MAT_PLAYER_PAST_POSITION_COLOR_NAME, 
        MAT_PLAYER_PAST_POSITION_ARRAY_LENGTH_NAME, 
        MAT_PLAYER_PAST_POSITION_ARRAY_NAME, 
        string.Empty, 
        MAT_PLAYER_PAST_POSITION_ALPHA_ARRAY_NAME, 
        Color.white
    );

    #region Material Draw Properties
    //private const string MAT_USE_BASE_COLOR_NAME = "_UseBaseColor";
    //private const string MAT_DRAW_RIM_NAME = "_DrawRim";
    //private const string MAT_DRAW_PLAYER_PAST_POSITION_NAME = "_DrawPlayerPastPos";
    private const string MAT_USE_BASE_COLOR_KEY = "USE_BASE_COLOR";
    private const string MAT_DRAW_RIM_KEY = "DRAW_RIM";
    private const string MAT_DRAW_PLAYER_PAST_POSITION_KEY = "DRAW_PLAYER_PAST_POS";
    #endregion

    private const string MAT_COLOR_STRENGTH_MAX_NAME = "_ColorStrengthMax";
    private const string MAT_RIM_THICKNESS_NAME = "_RimThickness";
    private const string MAT_RIM_THICKNESS_OFFSET_NAME = "_RimThicknessOffset";
    private const string MAT_PLAYER_PAST_POSITION_RADIUS_NAME = "_PlayerPastPosRadius";
    #endregion

    public static readonly float STANDARD_RIM_RADIUS_SPREAD_TIME = 5.0f;
    // SpreadTime 동안 MazeBlock을 10칸 이동하기 위해 10을 곱함
    public static readonly float STANDARD_RIM_RADIUS_SPREAD_LENGTH = MazeBlock.BlockSize * 10;
    #endregion

    private AudioReverbZone reverbZone = null;

    public Action<ItemController> OnItemCollected;



    protected override void Awake() {
        base.Awake();

        SoundManager.Instance.OnWorldSoundAdded += WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += WorldSoundRemoved;

        OnItemCollected += ItemCollected;

        UserSettings.OnDisplayBrightnessChanged += OnDisplayBrightnessChanged;
    }

    private void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= WorldSoundRemoved;

        OnItemCollected -= ItemCollected;

        UserSettings.OnDisplayBrightnessChanged -= OnDisplayBrightnessChanged;
    }

    private void LateUpdate() {
        foreach(MaterialPropertiesGroup group in rimMaterialPropertiesGroups) {
            if(group.CurrentArrayLength > 0) {
                group.SetUpdateRadiusArray(blockFloorMaterial);
                group.SetUpdateAlphaArray(blockFloorMaterial);
#if Use_Two_Materials_On_MazeBlock
                group.SetUpdateRadiusArray(blockWallMaterial);
                group.SetUpdateAlphaArray(blockWallMaterial);
#endif

                foreach(MonsterController mc in monsters) {
                    group.SetUpdateRadiusArray(mc.Material);
                    group.SetUpdateAlphaArray(mc.Material);
                }
            }
        }

        if(playerMaterialPropertiesGroup.CurrentArrayLength > 0) {
            // Material의 Player Property는 MazeBlock 오브젝트에만 추가함
            playerMaterialPropertiesGroup.SetUpdateAlphaArray(
                blockFloorMaterial,
                Time.deltaTime,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH); //임의의 길이 설정
#if Use_Two_Materials_On_MazeBlock
            playerMaterialPropertiesGroup.SetUpdateAlphaArray(
                blockWallMaterial,
                Time.deltaTime,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH); //임의의 길이 설정
#endif
        }
    }

    #region Utility
    public void ResetLevel() {
        if(mazeBlocks != null) {
            int arrayLengthX = mazeBlocks.GetLength(0);
            int arrayLengthY = mazeBlocks.GetLength(1);
            for(int x = 0; x < arrayLengthX; x++) {
                for(int y = 0; y < arrayLengthY; y++) {
                    Destroy(mazeBlocks[x, y].gameObject);
                }
            }
            LevelWidth = 0;
            LevelHeight = 0;
            mazeBlocks = null;
        }

        if(monsters.Count > 0) {
            foreach(MonsterController mc in monsters) {
                Destroy(mc.gameObject);
            }

            monsters.Clear();
        }
    }

    public void LoadLevel(int width, int height, bool createEmpty = false) {
        if(createEmpty) MazeCreator.CreateEmptyMaze(width, height);
        else MazeCreator.CreateMaze(width, height);

        if(blockFloorMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));

            mat.SetFloat(MAT_COLOR_STRENGTH_MAX_NAME, UserSettings.DisplayBrightness);
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.2f);
            mat.SetFloat(MAT_RIM_THICKNESS_OFFSET_NAME, 1.0f);
            foreach(MaterialPropertiesGroup group in rimMaterialPropertiesGroups) {
                mat.SetVector(group.MAT_COLOR_NAME, group.Color);
            }

            //mat.SetFloat(MAT_USE_BASE_COLOR_NAME, 1.0f);
            //mat.SetFloat(MAT_DRAW_RIM_NAME, 1.0f);
            //mat.SetFloat(MAT_DRAW_PLAYER_PAST_POSITION_NAME, 1.0f);
            mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
            mat.EnableKeyword(MAT_DRAW_RIM_KEY);
            mat.EnableKeyword(MAT_DRAW_PLAYER_PAST_POSITION_KEY);

            mat.SetFloat(MAT_PLAYER_PAST_POSITION_RADIUS_NAME, 0.15f);

            blockFloorMaterial = mat;
        }
#if Use_Two_Materials_On_MazeBlock
        if(blockWallMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));

            mat.SetFloat(MAT_COLOR_STRENGTH_MAX_NAME, UserSettings.DisplayBrightness);
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.2f);
            mat.SetFloat(MAT_RIM_THICKNESS_OFFSET_NAME, 1.0f);
            mat.SetColor("_BaseColor", Color.red);
            foreach(MaterialPropertiesGroup group in rimMaterialPropertiesGroups) {
                mat.SetVector(group.MAT_COLOR_NAME, group.Color);
            }

            //mat.SetFloat(MAT_USE_BASE_COLOR_NAME, 1.0f);
            //mat.SetFloat(MAT_DRAW_RIM_NAME, 1.0f);
            //mat.SetFloat(MAT_DRAW_PLAYER_PAST_POSITION_NAME, 0.0f);
            mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
            mat.EnableKeyword(MAT_DRAW_RIM_KEY);
            mat.DisableKeyword(MAT_DRAW_PLAYER_PAST_POSITION_KEY);

            blockWallMaterial = mat;
        }
#endif

        string componentName = typeof(MazeBlock).Name;
        GameObject resourceObj = ResourceLoader.GetResource<GameObject>(componentName);

        mazeBlocks = new MazeBlock[width, height];
        for(int x = 0; x < width; x++) {
            for(int z = 0; z < height; z++) {
                GameObject go = Instantiate(resourceObj, transform);
                go.name = componentName;

                go.transform.position = GetBlockPos(x, z);
                go.transform.localScale = MazeBlock.StandardBlockScale;

                MazeBlock mb = go.GetComponent<MazeBlock>();
                mb.WallInfo = MazeCreator.Maze[x, z].WallInfo;
#if Use_Two_Materials_On_MazeBlock
                mb.SetMaterial(blockFloorMaterial, blockWallMaterial);
#else
                mb.SetMaterial(blockFloorMaterial);
#endif

                mazeBlocks[x, z] = mb;
            }
        }

        LevelWidth = width;
        LevelHeight = height;

        SetReverbZone();
    }

    public Vector3 GetBlockPos(Vector2Int coord) {
        return GetBlockPos(coord.x, coord.y);
    }

    public Vector3 GetBlockPos(float x, float y) {
        return new Vector3(basicBlockPos.x * x, basicBlockPos.y, basicBlockPos.z * y) + blockPosOffset;
    }

    public Vector3 GetCenterPos() {
        return GetBlockPos((LevelWidth - 1) * 0.5f, (LevelHeight - 1) * 0.5f);
    }

    public float GetMazeLengthWidth() {
        return LevelWidth * MazeBlock.BlockSize;
    }

    public float GetMazeLengthHeight() {
        return LevelHeight * MazeBlock.BlockSize;
    }

    RaycastHit tempPathHit;
    public List<Vector3> GetPath(Vector3 startPos, Vector3 endPos, float rayRadius) {
        Vector2Int startCoord = GetMazeCoordinate(startPos);
        Vector2Int endCoord = GetMazeCoordinate(endPos);
        Debug.Log(string.Format("startPos: {0}, endPos: {1}, startCoord: {2}, endCoord: {3}", startPos, endPos, startCoord, endCoord));
        if(IsSameVec2Int(startCoord, endCoord)) {
            return new List<Vector3>() {
                startPos,
                endPos
            };
        }

        List<PathHelper> pastCoordList = new List<PathHelper>();
        List<PathHelper> checkCoordList = new List<PathHelper>();
        checkCoordList.Add(
            new PathHelper(
                -1,
                MazeCreator.Maze[startCoord.x, startCoord.y], 
                0, 
                GetStraightDistance(startCoord, endCoord)
            )
        );

        #region 단순 경로 탐색
        int mostCloseHelperIndex;
        PathHelper mostCloseHelper;
        MazeInfo tempInfo;
        Vector2Int tempMoveCoord;
        bool IsExistEndCoordOnCoordX(Vector2Int start, Vector2Int end) {
            return (start.y == endCoord.y && endCoord.y == end.y) && (start.x <= endCoord.x && endCoord.x <= end.x);
        }
        bool IsExistEndCoordOnCoordY(Vector2Int start, Vector2Int end) {
            return (start.x == endCoord.x && endCoord.x == end.x) && (start.y <= endCoord.y && endCoord.y <= end.y);
        }
        bool CanAddInCheckList(Vector2Int coord) {
            return pastCoordList.FindIndex(t => IsSameVec2Int(t.Coord, coord)) < 0 &&
                checkCoordList.FindIndex(t => IsSameVec2Int(t.Coord, coord)) < 0;
        }
        void AddInfoToCheckList(Vector2Int checkCoord, Vector2Int movedCoord) {
            PathHelper helper = new PathHelper(
                        pastCoordList.Count - 1,
                        MazeCreator.Maze[movedCoord.x, movedCoord.y],
                        mostCloseHelper.DistanceBetweenStartPointAndCurrentPoint + GetStraightDistance(checkCoord, movedCoord),
                        GetStraightDistance(movedCoord, endCoord));
            checkCoordList.Add(helper);
        }
        void AddInfoToPastList(Vector2Int checkCoord, Vector2Int movedCoord) {
            PathHelper helper = new PathHelper(
                        pastCoordList.Count - 1,
                        MazeCreator.Maze[movedCoord.x, movedCoord.y],
                        mostCloseHelper.DistanceBetweenStartPointAndCurrentPoint + GetStraightDistance(checkCoord, movedCoord),
                        GetStraightDistance(movedCoord, endCoord));
            pastCoordList.Add(helper);
        }
        while(true) {
            // CheckList에서 endCoord에 가장 가까운 경로를 가진 Helper를 찾는다
            mostCloseHelperIndex = GetMostCloseHelperIndex(checkCoordList);
            mostCloseHelper = checkCoordList[mostCloseHelperIndex];

            // 위에서 찾은 Helper를 PastList에 넣고 CheckList에서 제거한다
            pastCoordList.Add(mostCloseHelper);
            checkCoordList.RemoveAt(mostCloseHelperIndex);

            // 위에서 찾은 Helper를 기준으로 갈 수 있는 곳을 CheckList에 추가한다
            tempInfo = MazeCreator.Maze[mostCloseHelper.Coord.x, mostCloseHelper.Coord.y];
            if(!tempInfo.WallInfo.HasFlag(MazeCreator.ActiveWall.R)) {
                tempMoveCoord = tempInfo.NextCrossLoadCoord_R;
                if(IsExistEndCoordOnCoordX(tempInfo.CurrentCoord, tempMoveCoord)) {
                    AddInfoToPastList(tempInfo.CurrentCoord, endCoord); 
                    break;
                }
                else if(CanAddInCheckList(tempMoveCoord)) {
                    AddInfoToCheckList(tempInfo.CurrentCoord, tempMoveCoord);
                }
            }
            if(!tempInfo.WallInfo.HasFlag(MazeCreator.ActiveWall.F)) {
                tempMoveCoord = tempInfo.NextCrossLoadCoord_F;
                if(IsExistEndCoordOnCoordY(tempInfo.CurrentCoord, tempMoveCoord)) {
                    AddInfoToPastList(tempInfo.CurrentCoord, endCoord); 
                    break;
                }
                else if(CanAddInCheckList(tempMoveCoord)) {
                    AddInfoToCheckList(tempInfo.CurrentCoord, tempMoveCoord);
                }
            }
            if(!tempInfo.WallInfo.HasFlag(MazeCreator.ActiveWall.L)) {
                tempMoveCoord = tempInfo.NextCrossLoadCoord_L;
                if(IsExistEndCoordOnCoordX(tempMoveCoord, tempInfo.CurrentCoord)) {
                    AddInfoToPastList(tempInfo.CurrentCoord, endCoord); 
                    break;
                }
                else if(CanAddInCheckList(tempMoveCoord)) {
                    AddInfoToCheckList(tempInfo.CurrentCoord, tempMoveCoord);
                }
            }
            if(!tempInfo.WallInfo.HasFlag(MazeCreator.ActiveWall.B)) {
                tempMoveCoord = tempInfo.NextCrossLoadCoord_B;
                if(IsExistEndCoordOnCoordY(tempMoveCoord, tempInfo.CurrentCoord)) {
                    AddInfoToPastList(tempInfo.CurrentCoord, endCoord); 
                    break;
                }
                else if(CanAddInCheckList(tempMoveCoord)) {
                    AddInfoToCheckList(tempInfo.CurrentCoord, tempMoveCoord);
                }
            }

            // 만약 CheckList에 Helper가 없다면 null 값을 return
            if(checkCoordList.Count <= 0) {
                Debug.LogError("Path not found.");

                return null;
            }
        }
        #endregion

        #region 경로 정리
        List<PathHelper> pathHelperList = new List<PathHelper>();
        int searchIndex = pastCoordList.Count - 1;
        while(true) {
            pathHelperList.Add(pastCoordList[searchIndex]);
            searchIndex = pathHelperList[pathHelperList.Count - 1].ParentIndex;
            if(searchIndex < 0) {
                break;
            }
        }
        if(pathHelperList.Count < 2) {
            Debug.LogError("Path count not enough.");

            return null;
        }
        pathHelperList.Reverse();
        #endregion

        #region Coord to Vector3
        MazeCreator.ActiveWall GetMovedDirection(Vector2Int startCoord, Vector2Int endCoord) {
            if(startCoord.x == endCoord.x) {
                return startCoord.y < endCoord.y ? MazeCreator.ActiveWall.F : MazeCreator.ActiveWall.B;
            }
            else {
                return startCoord.x < endCoord.x ? MazeCreator.ActiveWall.R : MazeCreator.ActiveWall.L;
            }
        }

        // coord List를 Vector3 List로 변환
        // rayRadius를 사용하여 CornerPoint를 적용
        List<Vector3> pathList = new List<Vector3>();
        pathList.Add(startPos); //처음 위치 설정

        MazeBlock tempBlock;
        Vector2Int pastCoord;
        Vector2Int currentCoord;
        Vector2Int nextCoord;
        MazeCreator.ActiveWall d1;
        MazeCreator.ActiveWall d2;
        for(int i = 1; i < pathHelperList.Count - 1; i++) {
            pastCoord = pathHelperList[i - 1].Coord;
            currentCoord = pathHelperList[i].Coord;
            nextCoord = pathHelperList[i + 1].Coord;
            d1 = GetMovedDirection(pastCoord, currentCoord);
            d2 = GetMovedDirection(currentCoord, nextCoord);
            // 직선 경로 제거
            if(d1 == d2) {
                pathHelperList.RemoveAt(i);
                i--;
            }
            else {
                tempBlock = mazeBlocks[currentCoord.x, currentCoord.y];

                pathList.Add(tempBlock.GetCornerPoint(d1, d2, rayRadius));
            }
        }

        //pathList.Add(endPos); //마지막 위치 설정
        pathList.Add(new Vector3(endPos.x, 0.0f, endPos.z));
        #endregion

        // 심층 단순화
        //Vector3 p1;
        //Vector3 p2;
        //Vector3 direction;
        //if(pathList.Count > 2) {
        //    Vector3 tempPast;
        //    Vector3 tempNext;
        //    float distance;
        //    for(int i = 1; i < pathList.Count - 1; i++) {
        //        tempPast = pathList[i - 1];
        //        tempNext = pathList[i + 1];

        //        p1 = tempPast;
        //        p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //임의로 player의 높이를 적용
        //        direction = (tempNext - tempPast).normalized;
        //        distance = Vector3.Distance(tempPast, tempNext);
        //        if(!Physics.CapsuleCast(p1, p2, rayRadius, direction, out tempPathHit, distance, mask)) {
        //            pathList.RemoveAt(i);
        //            i--;
        //        }
        //    }
        //}

        return pathList;
    }

    public float GetPathDistance(List<Vector3> path) {
        if(path == null || path.Count < 2) {
            Debug.LogWarning("Path not enough.");

            return 0.0f;
        }

        float distance = 0.0f;
        for(int i = 0; i < path.Count - 1; i++) {
            distance += Vector3.Distance(path[i], path[i + 1]);
        }

        return distance;
    }

    public Vector2Int GetMazeCoordinate(Vector3 pos) =>
        new Vector2Int(Mathf.FloorToInt(pos.x / MazeBlock.BlockSize), Mathf.FloorToInt(pos.z / MazeBlock.BlockSize));

    /// <summary>
    /// <br/> overDistance == true : currentPos를 기준으로 distance 보다 먼 좌표의 경로 반환
    /// <br/> overDistance == false : currentPos를 기준으로 distance 보다 가까운 좌표의 경로 반환
    /// </summary>
    public List<Vector3> GetRandomPointPathCompareDistance(Vector3 currentPos, float rayRadius, bool overDistance, float distance) {
        Vector2Int endpoint = overDistance ? GetRandomCoordOverDistance(currentPos, distance) : GetRandomCoordNearbyDistance(currentPos, distance);

        return GetPath(currentPos, GetBlockPos(endpoint), rayRadius);
    }

    /// <summary>
    /// <br/>현재 생성된 몬스터의 위치와 겹치지 않게 생성.
    /// <br/>플레이어와의 거리가 일정 이상 떨어진 위치에 생성
    /// </summary>
    public void AddMonsterOnLevelRandomly(MonsterType type, int count, float compareDistance, bool overDistance) {
        if(count <= 0) {
            Debug.LogWarning($"Count not enough. Count: {count}");

            return;
        }

        GameObject resource = ResourceLoader.GetResource<GameObject>(Path.Combine(ROOT_PATH_OF_MONSTERS, type.ToString()));
        if(resource == null) {
            Debug.LogError($"Monster Resource not found. type: {type}");

            return;
        }

        Vector2Int[] currentMonstersCoordArray = monsters.Select(t => GetMazeCoordinate(t.Pos)).ToArray();
        List<Vector2Int> usingCoordList = new List<Vector2Int>();
        while(usingCoordList.Count < count) {
            // 플레이어와의 거리가 일정 거리 이상 떨어져 있는 coord 생성
            Vector2Int randomCoord = overDistance ?
                GetRandomCoordOverDistance(UtilObjects.Instance.CamPos, compareDistance) :
                GetRandomCoordNearbyDistance(UtilObjects.Instance.CamPos, compareDistance);
            // 현재 몬스터들과 겹치지 않는 위치 확인
            if(Array.FindIndex(currentMonstersCoordArray, t => IsSameVec2Int(t, randomCoord)) < 0 &&
                usingCoordList.FindIndex(t => IsSameVec2Int(t, randomCoord)) < 0 &&
                monsters.FindIndex(t => IsSameVec2Int(GetMazeCoordinate(t.Pos), randomCoord)) < 0 &&
                items.FindIndex(t => IsSameVec2Int(GetMazeCoordinate(t.Pos), randomCoord)) < 0) {
                usingCoordList.Add(randomCoord);
            }
        }

        foreach(Vector2Int coord in usingCoordList) {
            GameObject go = Instantiate(resource, transform);
            go.transform.position = GetBlockPos(coord);

            MonsterController mc = go.GetComponent<MonsterController>();
            mc.Material.SetFloat(MAT_COLOR_STRENGTH_MAX_NAME, UserSettings.DisplayBrightness);
            mc.Material.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.2f);
            mc.Material.SetFloat(MAT_RIM_THICKNESS_OFFSET_NAME, 4.0f);
            //mc.Material.SetFloat(MAT_USE_BASE_COLOR_NAME, 0.0f);
            //mc.Material.SetFloat(MAT_DRAW_RIM_NAME, 1.0f);
            //mc.Material.SetFloat(MAT_DRAW_PLAYER_PAST_POSITION_NAME, 0.0f);
            mc.Material.DisableKeyword(MAT_USE_BASE_COLOR_KEY);
            mc.Material.DisableKeyword(MAT_DRAW_RIM_KEY);
            mc.Material.DisableKeyword(MAT_DRAW_PLAYER_PAST_POSITION_KEY);

            monsters.Add(mc);
        }
    }

    public void AddItemOnLevelRandomly(ItemType type, int count, float compareDistance, bool overDistance) {
        if(count <= 0) {
            Debug.LogWarning($"Count not enough. Count: {count}");

            return;
        }

        GameObject resource = ResourceLoader.GetResource<GameObject>(Path.Combine(ROOT_PATH_OF_ITEMS, type.ToString()));
        if(resource == null) {
            Debug.LogError($"Item Resource not found. type: {type}");

            return;
        }

        Vector2Int[] currentItemsCoordArray = items.Select(t => GetMazeCoordinate(t.Pos)).ToArray();
        List<Vector2Int> usingCoordList = new List<Vector2Int>();
        while(usingCoordList.Count < count) {
            // 플레이어와의 거리가 일정 거리 이상 떨어져 있는 coord 생성
            Vector2Int randomCoord = overDistance ? 
                GetRandomCoordOverDistance(UtilObjects.Instance.CamPos, compareDistance) : 
                GetRandomCoordNearbyDistance(UtilObjects.Instance.CamPos, compareDistance);
            // 현재 아이템과 겹치지 않는 위치 확인
            if(Array.FindIndex(currentItemsCoordArray, t => IsSameVec2Int(t, randomCoord)) < 0 &&
                usingCoordList.FindIndex(t => IsSameVec2Int(t, randomCoord)) < 0 && 
                monsters.FindIndex(t => IsSameVec2Int(GetMazeCoordinate(t.Pos), randomCoord)) < 0 &&
                items.FindIndex(t => IsSameVec2Int(GetMazeCoordinate(t.Pos), randomCoord)) < 0) {
                usingCoordList.Add(randomCoord);
            }
        }

        foreach(Vector2Int coord in usingCoordList) {
            GameObject go = Instantiate(resource, transform);
            go.transform.position = GetBlockPos(coord);

            ItemController ic = go.GetComponent<ItemController>();

            items.Add(ic);
        }
    }

    public void PlayMonsters() { 
        // 몬스터가 게임 중간에 추가될 때도 있기 때문에 전체 범위가 아닌 제한된 범위에서만 for문 실행
        for(int i = monsters.Count - 1; i >= 0; i--) {
            if(monsters[i].IsPlaying) break;

            monsters[i].Play();
        }
    }

    public void StopMonsters() {
        foreach(MonsterController mc in monsters) {
            mc.Stop();
        }
    }

    public void AddPlayerPosInMaterialProperty(Vector3 pos) {
        playerMaterialPropertiesGroup.AddPos(new Vector4(pos.x, pos.y, pos.z));
        playerMaterialPropertiesGroup.SetPosArray(blockFloorMaterial);
#if Use_Two_Materials_On_MazeBlock
        playerMaterialPropertiesGroup.SetPosArray(blockWallMaterial);
#endif
    }
    #endregion

    #region Action
    private void WorldSoundAdded(SoundObject so, SoundManager.SoundFrom from) {
        foreach(MaterialPropertiesGroup group in rimMaterialPropertiesGroups) {
            group.UpdateArrayLength();

            group.SetPosArray(blockFloorMaterial);
#if Use_Two_Materials_On_MazeBlock
            group.SetPosArray(blockWallMaterial);
#endif

            foreach(MonsterController mc in monsters) {
                group.SetPosArray(mc.Material);
            }
        }
    }

    private void WorldSoundRemoved(SoundManager.SoundFrom from) {
        foreach(MaterialPropertiesGroup group in rimMaterialPropertiesGroups) {
            group.UpdateArrayLength();

            group.SetPosArray(blockFloorMaterial);
#if Use_Two_Materials_On_MazeBlock
            group.SetPosArray(blockWallMaterial);
#endif

            foreach(MonsterController mc in monsters) {
                group.SetPosArray(mc.Material);
            }
        }
    }

    private void ItemCollected(ItemController item) {

    }

    private void OnDisplayBrightnessChanged(float value) {
        if(blockFloorMaterial != null) blockFloorMaterial.SetFloat(MAT_COLOR_STRENGTH_MAX_NAME, value);
#if Use_Two_Materials_On_MazeBlock
        if(blockWallMaterial != null) blockWallMaterial.SetFloat(MAT_COLOR_STRENGTH_MAX_NAME, value);
#endif

        if(monsters != null) {
            foreach(MonsterController mc in monsters) {
                mc.Material.SetFloat(MAT_COLOR_STRENGTH_MAX_NAME, value);
            }
        }
    }
    #endregion

    /// <summary>
    /// compareDistance 보다 멀리 있는 coord를 반환
    /// </summary>
    private Vector2Int GetRandomCoordOverDistance(Vector3 currentPos, float compareDistance) {
        Vector2Int randomCoord;
        Vector3 tempPos;
        float dist;
        while(true) {
            randomCoord = new Vector2Int(Random.Range(0, LevelWidth), Random.Range(0, LevelHeight));
            tempPos = GetBlockPos(randomCoord);
            dist = Vector3.Distance(currentPos, tempPos);
            if(dist > compareDistance) {
                break;
            }
        }

        return randomCoord;
    }

    /// <summary>
    /// compareDistance 보다 가까이 있는 coord를 반환
    /// </summary>
    private Vector2Int GetRandomCoordNearbyDistance(Vector3 currentPos, float compareDistance) {
        Vector2Int randomCoord;
        Vector3 tempPos;
        float dist;
        while(true) {
            randomCoord = new Vector2Int(Random.Range(0, LevelWidth), Random.Range(0, LevelHeight));
            tempPos = GetBlockPos(randomCoord);
            dist = Vector3.Distance(currentPos, tempPos);
            if(dist < compareDistance) {
                break;
            }
        }

        return randomCoord;
    }

    private void SetReverbZone() {
        if(reverbZone == null) {
            GameObject go = new GameObject(nameof(AudioReverbZone));
            go.transform.SetParent(transform);

            AudioReverbZone r = go.AddComponent<AudioReverbZone>();
            float mazeLengthW = GetMazeLengthWidth();
            float mazeLengthH = GetMazeLengthHeight();
            r.minDistance = (mazeLengthW > mazeLengthH ? mazeLengthW : mazeLengthH) * 0.5f * 1.414f;
            r.maxDistance = r.minDistance * 2.0f;
            r.reverbPreset = AudioReverbPreset.Cave;

            reverbZone = r;
        }

        reverbZone.transform.position = GetCenterPos();
    }

    private bool IsSameVec2Int(Vector2Int v1, Vector2Int v2) => v1.x == v2.x && v1.y == v2.y;

    #region 길찾기 Util Func
    private int GetStraightDistance(Vector2Int c1, Vector2Int c2) => Mathf.Abs(c2.x - c1.x) + Mathf.Abs(c2.y - c1.y);
    private int GetMostCloseHelperIndex(List<PathHelper> helperList) {
        int index = 0;
        for(int i = 1; i < helperList.Count; i++) {
            if(helperList[i].TotalDistance < helperList[index].TotalDistance) {
                index = i;
            }
        }

        return index;
    }
    #endregion

    private class MaterialPropertiesGroup {
        public SoundManager.SoundFrom From;

        public string MAT_COLOR_NAME { get; private set; }
        public string MAT_ARRAY_LENGTH_NAME { get; private set; }
        public string MAT_POSITION_ARRAY_NAME { get; private set; }
        public string MAT_RADIUS_ARRAY_NAME { get; private set; }
        public string MAT_ALPHA_ARRAY_NAME { get; private set; }

        public Vector4[] PosArray { get; private set; } = new Vector4[LIST_MAX_LENGTH];
        // Player Alpha를 계산하기 위해 tempArray로 사용됨
        public float[] RadiusArray { get; private set; } = new float[LIST_MAX_LENGTH];
        public float[] AlphaArray { get; private set; } = new float[LIST_MAX_LENGTH];

        public Color Color;

        public int CurrentArrayLength { get; private set; } = 0;

        public MaterialPropertiesGroup(
            SoundManager.SoundFrom from,
            string colorName,
            string arrayLengthName,
            string positionArrayName,
            string radiusArrayName,
            string alphaArrayName,
            Color color) {
            From = from;
            MAT_COLOR_NAME = colorName;
            MAT_ARRAY_LENGTH_NAME = arrayLengthName;
            MAT_POSITION_ARRAY_NAME = positionArrayName;
            MAT_RADIUS_ARRAY_NAME = radiusArrayName;
            MAT_ALPHA_ARRAY_NAME = alphaArrayName;
            Color = color;
        }

        #region Utility
        #region Player Property Update Func
        /// <summary>
        /// Array의 마지막 index에 추가됨
        /// </summary>
        public void AddPos(Vector4 pos) {
            if(CurrentArrayLength >= PosArray.Length) {
                int removeCount = PosArray.Length - CurrentArrayLength + 1;
                RemovePos(removeCount);
            }

            PosArray[CurrentArrayLength] = pos;
            CurrentArrayLength++;
        }

        /// <summary>
        /// Array의 첫 index(0)이 제거됨
        /// </summary>
        public void RemovePos(int removeCount = 1) {
            if(removeCount < 1) {
                Debug.LogWarning($"removeCount not enough. removeCount: {removeCount}");

                return;
            }

            Vector4[] newPosArray = new Vector4[PosArray.Length];
            Array.Copy(PosArray, removeCount, newPosArray, 0, PosArray.Length - removeCount);
            PosArray = newPosArray;

            // tempArray로 사용하기 위해 같이 update
            float[] newRadiusArray = new float[RadiusArray.Length];
            Array.Copy(RadiusArray, removeCount, newRadiusArray, 0, RadiusArray.Length - removeCount);
            RadiusArray = newRadiusArray;

            float[] newAlphaArray = new float[AlphaArray.Length];
            Array.Copy(AlphaArray, removeCount, newAlphaArray, 0, AlphaArray.Length - removeCount);
            AlphaArray = newAlphaArray;

            CurrentArrayLength -= removeCount;
        }

        public void SetUpdateAlphaArray(Material mat, float addValue, float max) {
            // Array는 새로운 값이 추가될 때에 맨 뒤 쪽에 추가되기 때문에 상시 내림차순 정렬이 된 것과 같음
            // tempRadius > max 조건이 확인되는 인덱스(i)를 확인하고 i 보다 작거나 같은 index를 모두 제거
            int i = CurrentArrayLength - 1;
            for(; i >= 0; i--) {
                float newRadius = RadiusArray[i] + addValue;
                if(newRadius > max) {
                    break;
                }

                AlphaArray[i] = 1.0f - Mathf.InverseLerp(0.9f, 1.0f, Mathf.Abs(newRadius / max - 0.5f) * 2.0f);

                RadiusArray[i] = newRadius;
            }

            // for문이 break 없이 끝났다면 i는 -1이 되어 있음
            if(i >= 0) {
                RemovePos(i + 1);

                SetPosArray(mat);
            }

            mat.SetFloatArray(MAT_ALPHA_ARRAY_NAME, AlphaArray);
        }
        #endregion

        #region Rim Property Update Func
        public bool UpdateArrayLength() {
            Vector4[] rimPosArray = SoundManager.Instance.GetSoundObjectPosArray(From);
            if(rimPosArray.Length != CurrentArrayLength) {
                Array.Copy(rimPosArray, 0, PosArray, 0, rimPosArray.Length);
                CurrentArrayLength = rimPosArray.Length;

                return true;
            }

            return false;
        }

        // 매 프레임마다 업데이트 해야하는 properties
        public void SetUpdateRadiusArray(Material mat) {
            float[] rimRadiusArray = SoundManager.Instance.GetSoundObjectRadiusArray(
                From,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
            Array.Copy(rimRadiusArray, 0, RadiusArray, 0, rimRadiusArray.Length);
            mat.SetFloatArray(MAT_RADIUS_ARRAY_NAME, RadiusArray);
        }

        // 매 프레임마다 업데이트 해야하는 properties
        public void SetUpdateAlphaArray(Material mat) {
            float[] rimAlphaArray = SoundManager.Instance.GetSoundObjectAlphaArray(
                From,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
            Array.Copy(rimAlphaArray, 0, AlphaArray, 0, rimAlphaArray.Length);
            mat.SetFloatArray(MAT_ALPHA_ARRAY_NAME, AlphaArray);
        }
        #endregion

        // Rim과 Player에 모두 사용됨
        public void SetPosArray(Material mat) {
            mat.SetInteger(MAT_ARRAY_LENGTH_NAME, CurrentArrayLength);
            mat.SetVectorArray(MAT_POSITION_ARRAY_NAME, PosArray);
        }
        #endregion
    }

    private class PathHelper {
        public int ParentIndex { get; private set; }
        /// <summary>
        /// Direction of Parent to CurrentPoint
        /// </summary>
        public MazeInfo Info { get; private set; }
        public Vector2Int Coord { get { return Info.CurrentCoord; } }
        public MazeCreator.ActiveWall ActiveWall { get { return Info.WallInfo; } }

        public int DistanceBetweenStartPointAndCurrentPoint { get; private set; }
        public int DistanceBetweenCurrentPointAndEndPoint { get; private set; }
        public int TotalDistance { get; private set; }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="accumulatedDistance">누적된 거리</param>
        public PathHelper(
            int parentIndex,
            MazeInfo info, 
            int accumulatedDistance, 
            int distanceBetweenCurrentPointAndEndPoint) {
            ParentIndex = parentIndex;
            Info = info;

            DistanceBetweenStartPointAndCurrentPoint = accumulatedDistance;
            DistanceBetweenCurrentPointAndEndPoint = distanceBetweenCurrentPointAndEndPoint;
            TotalDistance = DistanceBetweenStartPointAndCurrentPoint + DistanceBetweenCurrentPointAndEndPoint;
        }
    }
}
