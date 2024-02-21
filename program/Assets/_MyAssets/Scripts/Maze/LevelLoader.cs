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

    }

    private readonly string BASIC_PATH_OF_MONSTERS = "Monsters";

    private Dictionary<MonsterType, GameObject> monsterResources = new Dictionary<MonsterType, GameObject>();

    private List<MonsterController> monsters = new List<MonsterController>();
    public List<MonsterController> Monsters { get { return monsters; } }

    public int MonsterCount { get { return monsters.Count; } }
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

    private const string MAT_RIM_THICKNESS_NAME = "_RimThickness";
    private const string MAT_RIM_COLOR_NAME = "_RimColor";
    private const string MAT_RIM_ARRAY_LENGTH_NAME = "_RimArrayLength";
    private const string MAT_RIM_POSITION_ARRAY_NAME = "_RimPosArray";
    private const string MAT_RIM_RADIUS_ARRAY_NAME = "_RimRadiusArray";
    private const string MAT_RIM_ALPHA_ARRAY_NAME = "_RimAlphaArray";

    private static readonly int LIST_MAX_LENGTH = 256;

    private MaterialPropertiesGroup[] materialPropertiesGroups = new MaterialPropertiesGroup[] {
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
            Color.green)
    };

    public static readonly float STANDARD_RIM_RADIUS_SPREAD_TIME = 5.0f;
    // SpreadTime 동안 MazeBlock을 10칸 이동하기 위해 10을 곱함
    public static readonly float STANDARD_RIM_RADIUS_SPREAD_LENGTH = MazeBlock.BlockSize * 10;
    #endregion

    private AudioReverbZone reverbZone = null;



    protected override void Awake() {
        base.Awake();

        SoundManager.Instance.OnWorldSoundAdded += OnWorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += OnWorldSoundRemoved;
    }

    private void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= OnWorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= OnWorldSoundRemoved;
    }

    private void Start() {
        MonsterController testMonster = FindObjectOfType<MonsterController>();
        if(testMonster != null) {
            monsters.Add(testMonster);
        }
    }

    private void Update() {
        foreach(MaterialPropertiesGroup group in materialPropertiesGroups) { 
            group.SetUpdateProperties(blockFloorMaterial);
#if Use_Two_Materials_On_MazeBlock
            group.SetUpdateProperties(blockWallMaterial);
#endif

            foreach(MonsterController mc in monsters) {
                group.SetUpdateProperties(mc.Material);
            }
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

        string componentName = typeof(MazeBlock).Name;
        GameObject resourceObj = ResourceLoader.GetResource<GameObject>(componentName);

        if(blockFloorMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.35f);
            foreach(MaterialPropertiesGroup group in materialPropertiesGroups) {
                mat.SetVector(group.MAT_RIM_COLOR_NAME, group.RimColor);
            }

            blockFloorMaterial = mat;
        }
#if Use_Two_Materials_On_MazeBlock
        if(blockWallMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.35f);
            mat.SetColor("_BaseColor", Color.red);
            foreach(MaterialPropertiesGroup group in materialPropertiesGroups) {
                mat.SetVector(group.MAT_RIM_COLOR_NAME, group.RimColor);
            }

            blockWallMaterial = mat;
        }
#endif


        mazeBlocks = new MazeBlock[width, height];
        for(int x = 0; x < width; x++) {
            for(int z = 0; z < height; z++) {
                GameObject go = Instantiate(resourceObj, transform);
                go.name = componentName;

                go.transform.position = GetBlockPos(x, z);
                go.transform.localScale = MazeBlock.StandardBlockScale;

                MazeBlock mb = go.GetComponent<MazeBlock>();
                mb.WallInfo = MazeCreator.Maze[x, z];
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
    public List<Vector3> GetPath(Vector3 startPos, Vector3 endPos, float rayRadius, int mask) {
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
        checkCoordList.Add(new PathHelper(-1, startCoord, MazeCreator.ActiveWall.None, 0, GetStraightDistance(startCoord, endCoord)));

        int mostCloseHelperIndex;
        PathHelper mostCloseHelper;
        MazeCreator.ActiveWall tempWallInfo;
        Vector2Int tempMoveCoord;
        while(true) {
            // CheckList에서 endCoord에 가장 가까운 경로를 가진 Helper를 찾는다
            mostCloseHelperIndex = GetMostCloseHelperIndex(checkCoordList);
            mostCloseHelper = checkCoordList[mostCloseHelperIndex];

            // 위에서 찾은 Helper를 PastList에 넣고 CheckList에서 제거한다
            pastCoordList.Add(mostCloseHelper);
            checkCoordList.RemoveAt(mostCloseHelperIndex);

            // 만약 PastList에 추가된 Helper의 Point가 endCoord와 같다면 루프 종료
            if(IsSameVec2Int(mostCloseHelper.Coord, endCoord)) {
                Debug.Log("Successed find path!");

                break;
            }

            // 위에서 찾은 Helper를 기준으로 갈 수 있는 곳을 CheckList에 추가한다
            tempWallInfo = mazeBlocks[mostCloseHelper.Coord.x, mostCloseHelper.Coord.y].WallInfo;
            if(!tempWallInfo.HasFlag(MazeCreator.ActiveWall.R)) {
                tempMoveCoord = GetMoveToCoordRight(mostCloseHelper.Coord);
                if(pastCoordList.FindIndex(t => IsSameVec2Int(t.Coord, tempMoveCoord)) < 0 && checkCoordList.FindIndex(t => IsSameVec2Int(t.Coord, tempMoveCoord)) < 0) {
                    PathHelper helper = new PathHelper(
                        pastCoordList.Count - 1,
                        tempMoveCoord,
                        MazeCreator.ActiveWall.R,
                        mostCloseHelper.DistanceBetweenStartPointAndCurrentPoint + 1, 
                        GetStraightDistance(tempMoveCoord, endCoord));
                    checkCoordList.Add(helper);
                }
            }
            if(!tempWallInfo.HasFlag(MazeCreator.ActiveWall.F)) {
                tempMoveCoord = GetMoveToCoordForward(mostCloseHelper.Coord);
                if(pastCoordList.FindIndex(t => IsSameVec2Int(t.Coord, tempMoveCoord)) < 0 && checkCoordList.FindIndex(t => IsSameVec2Int(t.Coord, tempMoveCoord)) < 0) {
                    PathHelper helper = new PathHelper(
                        pastCoordList.Count - 1,
                        tempMoveCoord,
                        MazeCreator.ActiveWall.F,
                        mostCloseHelper.DistanceBetweenStartPointAndCurrentPoint + 1,
                        GetStraightDistance(tempMoveCoord, endCoord));
                    checkCoordList.Add(helper);
                }
            }
            if(!tempWallInfo.HasFlag(MazeCreator.ActiveWall.L)) {
                tempMoveCoord = GetMoveToCoordLeft(mostCloseHelper.Coord);
                if(pastCoordList.FindIndex(t => IsSameVec2Int(t.Coord, tempMoveCoord)) < 0 && checkCoordList.FindIndex(t => IsSameVec2Int(t.Coord, tempMoveCoord)) < 0) {
                    PathHelper helper = new PathHelper(
                        pastCoordList.Count - 1,
                        tempMoveCoord,
                        MazeCreator.ActiveWall.L,
                        mostCloseHelper.DistanceBetweenStartPointAndCurrentPoint + 1,
                        GetStraightDistance(tempMoveCoord, endCoord));
                    checkCoordList.Add(helper);
                }
            }
            if(!tempWallInfo.HasFlag(MazeCreator.ActiveWall.B)) {
                tempMoveCoord = GetMoveToCoordBack(mostCloseHelper.Coord);
                if(pastCoordList.FindIndex(t => IsSameVec2Int(t.Coord, tempMoveCoord)) < 0 && checkCoordList.FindIndex(t => IsSameVec2Int(t.Coord, tempMoveCoord)) < 0) {
                    PathHelper helper = new PathHelper(
                        pastCoordList.Count - 1,
                        tempMoveCoord,
                        MazeCreator.ActiveWall.B, 
                        mostCloseHelper.DistanceBetweenStartPointAndCurrentPoint + 1,
                        GetStraightDistance(tempMoveCoord, endCoord));
                    checkCoordList.Add(helper);
                }
            }

            // 만약 CheckList에 Helper가 없다면 null 값을 return
            if(checkCoordList.Count <= 0) {
                Debug.LogError("Path not found.");

                return null;
            }
        }

        // 탐색을 통해 나온 coord 정리
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

        // 경로 단순화
        List<PathHelper> simplePathHelperList = new List<PathHelper>();
        simplePathHelperList.Add(pathHelperList[0]); //처음 좌표 추가
        PathHelper moveDirectionChecker = pathHelperList[1];
        for(int i = 2; i < pathHelperList.Count; i++) {
            if(moveDirectionChecker.MovedDirection != pathHelperList[i].MovedDirection) {
                simplePathHelperList.Add(pathHelperList[i - 1]);
                moveDirectionChecker = pathHelperList[i];
            }
        }
        simplePathHelperList.Add(pathHelperList[pathHelperList.Count - 1]); //마지막 좌표 추가

        // coord List를 Vector3 List로 변환
        // rayRadius를 사용하여 CornerPoint를 적용
        List<Vector3> pathList = new List<Vector3>();
        pathList.Add(startPos); //처음 위치 설정
        PathHelper currentHelper;
        PathHelper nextHelper;
        MazeBlock tempBlock;
        for(int i = 1; i < simplePathHelperList.Count - 1; i++) {
            currentHelper = simplePathHelperList[i];
            nextHelper = simplePathHelperList[i + 1];
            tempBlock = mazeBlocks[currentHelper.Coord.x, currentHelper.Coord.y];
            pathList.Add(tempBlock.GetCornerPoint(currentHelper.MovedDirection, nextHelper.MovedDirection, rayRadius));
        }
        pathList.Add(endPos); //마지막 위치 설정

        // 심층 단순화
        Vector3 p1;
        Vector3 p2;
        Vector3 direction;
        if(pathList.Count > 2) {
            Vector3 tempPast;
            Vector3 tempNext;
            float distance;
            for(int i = 1; i < pathList.Count - 1; i++) {
                tempPast = pathList[i - 1];
                tempNext = pathList[i + 1];

                p1 = tempPast;
                p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //임의로 player의 높이를 적용
                direction = (tempNext - tempPast).normalized;
                distance = Vector3.Distance(tempPast, tempNext);
                if(!Physics.CapsuleCast(p1, p2, rayRadius, direction, out tempPathHit, distance, mask)) {
                    pathList.RemoveAt(i);
                    i--;
                }
            }
        }

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
    public List<Vector3> GetRandomPointPathCompareDistance(Vector3 currentPos, float rayRadius, int mask, bool overDistance, float distance) {
        Vector2Int endpoint = overDistance ? GetRandomCoordOverDistance(currentPos, distance) : GetRandomCoordNearbyDistance(currentPos, distance);

        return GetPath(currentPos, GetBlockPos(endpoint), rayRadius, mask);
    }

    /// <summary>
    /// <br/>현재 생성된 몬스터의 위치와 겹치지 않게 생성.
    /// <br/>플레이어와의 거리가 일정 이상 떨어진 위치에 생성
    /// </summary>
    public void AddMonsterOnLevelRandomly(MonsterType type, int count) {
        if(count <= 0) {
            Debug.LogWarning($"Count not enough. Count: {count}");

            return;
        }

        GameObject resource = ResourceLoader.GetResource<GameObject>(Path.Combine(BASIC_PATH_OF_MONSTERS, type.ToString()));
        if(resource == null) {
            Debug.LogError($"Monster Resource not found. type: {type}");

            return;
        }

        Vector2Int[] currentMonstersCoordArray = monsters.Select(t => GetMazeCoordinate(t.Pos)).ToArray();
        List<Vector2Int> usingCoordList = new List<Vector2Int>();
        while(usingCoordList.Count < count) {
            // 플레이어와의 거리가 일정 거리 이상 떨어져 있는 coord 생성
            Vector2Int randomCoord = GetRandomCoordOverDistance(UtilObjects.Instance.CamPos, STANDARD_RIM_RADIUS_SPREAD_LENGTH * 2);
            // 현재 몬스터들과 겹치지 않는 위치 확인
            if(Array.FindIndex(currentMonstersCoordArray, t => IsSameVec2Int(t, randomCoord)) < 0 &&
                usingCoordList.FindIndex(t => IsSameVec2Int(t, randomCoord)) < 0) {
                usingCoordList.Add(randomCoord);
            }
        }

        foreach(Vector2Int coord in usingCoordList) {
            GameObject go = Instantiate(resource, transform);
            go.transform.position = GetBlockPos(coord);

            MonsterController mc = go.GetComponent<MonsterController>();

            monsters.Add(mc);
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
    #endregion

    #region Action
    private void OnWorldSoundAdded(SoundObject so, SoundManager.SoundFrom from) {
        foreach(MaterialPropertiesGroup group in materialPropertiesGroups) {
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

    private void OnWorldSoundRemoved(SoundManager.SoundFrom from) {
        foreach(MaterialPropertiesGroup group in materialPropertiesGroups) {
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

    private GameObject GetMonsterObject(MonsterType type) {
        GameObject obj = null;
        if(!monsterResources.TryGetValue(type, out obj)) {
            string path = GetMonsterObjectPath(type);
            obj = ResourceLoader.GetResource<GameObject>(path);

            monsterResources.Add(type, obj);
        }

        return obj;
    }

    private string GetMonsterObjectPath(MonsterType type) {
        switch(type) {
            case MonsterType.Bunny:
                return Path.Combine(BASIC_PATH_OF_MONSTERS, type.ToString());

            default:
                return string.Empty;
        }
    }

    #region 길찾기 Util Func
    private Vector2Int GetMoveToCoordRight(Vector2Int coord) => new Vector2Int(coord.x + 1, coord.y);
    private Vector2Int GetMoveToCoordForward(Vector2Int coord) => new Vector2Int(coord.x, coord.y + 1);
    private Vector2Int GetMoveToCoordLeft(Vector2Int coord) => new Vector2Int(coord.x - 1, coord.y);
    private Vector2Int GetMoveToCoordBack(Vector2Int coord) => new Vector2Int(coord.x, coord.y - 1);
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

        public string MAT_RIM_COLOR_NAME;
        public string MAT_RIM_ARRAY_LENGTH_NAME;
        public string MAT_RIM_POSITION_ARRAY_NAME;
        public string MAT_RIM_RADIUS_ARRAY_NAME;
        public string MAT_RIM_ALPHA_ARRAY_NAME;

        public Vector4[] RimPosArray = new Vector4[LIST_MAX_LENGTH];
        public float[] RimRadiusArray = new float[LIST_MAX_LENGTH];
        public float[] RimAlphaArray = new float[LIST_MAX_LENGTH];

        public Color RimColor;

        private int currentRimArrayLength = LIST_MAX_LENGTH;

        public MaterialPropertiesGroup(
            SoundManager.SoundFrom from,
            string rimColorName,
            string rimArrayLengthName,
            string rimPositionArrayName,
            string rimRadiusArrayName,
            string rimAlphaArrayName,
            Color rimColor) {
            From = from;
            MAT_RIM_COLOR_NAME = rimColorName;
            MAT_RIM_ARRAY_LENGTH_NAME = rimArrayLengthName;
            MAT_RIM_POSITION_ARRAY_NAME = rimPositionArrayName;
            MAT_RIM_RADIUS_ARRAY_NAME = rimRadiusArrayName;
            MAT_RIM_ALPHA_ARRAY_NAME = rimAlphaArrayName;
            RimColor = rimColor;
        }

        #region Utility
        public bool UpdateArrayLength() {
            Vector4[] rimPosArray = SoundManager.Instance.GetSoundObjectPosArray(From);
            if(rimPosArray.Length != currentRimArrayLength) {
                Array.Copy(rimPosArray, 0, RimPosArray, 0, rimPosArray.Length);
                currentRimArrayLength = rimPosArray.Length;

                return true;
            }

            return false;
        }

        public void SetPosArray(Material mat) {
            mat.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimArrayLength);
            mat.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, RimPosArray);
        }

        // 매 프레임마다 업데이트 해야하는 properties
        public void SetUpdateProperties(Material mat) {
            float[] rimRadiusArray = SoundManager.Instance.GetSoundObjectRadiusArray(
                From,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
            Array.Copy(rimRadiusArray, 0, RimRadiusArray, 0, rimRadiusArray.Length);
            mat.SetFloatArray(MAT_RIM_RADIUS_ARRAY_NAME, RimRadiusArray);

            float[] rimAlphaArray = SoundManager.Instance.GetSoundObjectAlphaArray(
                From,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
            Array.Copy(rimAlphaArray, 0, RimAlphaArray, 0, rimAlphaArray.Length);
            mat.SetFloatArray(MAT_RIM_ALPHA_ARRAY_NAME, RimAlphaArray);
        }
        #endregion
    }

    private class PathHelper {
        public int ParentIndex { get; private set; }
        public Vector2Int Coord { get; private set; }
        /// <summary>
        /// Direction of Parent to CurrentPoint
        /// </summary>
        public MazeCreator.ActiveWall MovedDirection { get; private set; }

        public int DistanceBetweenStartPointAndCurrentPoint { get; private set; }
        public int DistanceBetweenCurrentPointAndEndPoint { get; private set; }
        public int TotalDistance { get; private set; }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="accumulatedDistance">누적된 거리</param>
        public PathHelper(
            int parentIndex, 
            Vector2Int coord, 
            MazeCreator.ActiveWall movedDirection, 
            int accumulatedDistance, 
            int distanceBetweenCurrentPointAndEndPoint) {
            ParentIndex = parentIndex;
            Coord = coord;
            MovedDirection = movedDirection;

            DistanceBetweenStartPointAndCurrentPoint = accumulatedDistance;
            DistanceBetweenCurrentPointAndEndPoint = distanceBetweenCurrentPointAndEndPoint;
            TotalDistance = DistanceBetweenStartPointAndCurrentPoint + DistanceBetweenCurrentPointAndEndPoint;
        }
    }
}
