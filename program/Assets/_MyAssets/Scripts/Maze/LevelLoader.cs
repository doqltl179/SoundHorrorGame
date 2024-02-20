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
    private const string MAT_RIM_ARRAY_LENGTH_NAME = "_RimArrayLength";
    private const string MAT_RIM_POSITION_ARRAY_NAME = "_RimPosArray";
    private const string MAT_RIM_RADIUS_ARRAY_NAME = "_RimRadiusArray";
    //private const string MAT_RIM_THICKNESS_ARRAY_NAME = "_RimThicknessArray";
    private const string MAT_RIM_ALPHA_ARRAY_NAME = "_RimAlphaArray";

    private static readonly int LIST_MAX_LENGTH = 256;
    private Vector4[] rimPosArray = new Vector4[LIST_MAX_LENGTH];
    private float[] rimRadiusArray = new float[LIST_MAX_LENGTH];
    //private float[] rimThicknessArray = new float[LIST_MAX_LENGTH];
    private float[] rimAlphaArray = new float[LIST_MAX_LENGTH];

    public static readonly float STANDARD_RIM_RADIUS_SPREAD_TIME = 10.0f;
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

#if Play_Game_Automatically
    int watchIndex = 0;
#endif
    private void Update() {
        float[] currentRimRadiusArray = SoundManager.Instance.GetSoundObjectRadiusArray(STANDARD_RIM_RADIUS_SPREAD_TIME, STANDARD_RIM_RADIUS_SPREAD_LENGTH);
        float[] currentRimAlphaArray = SoundManager.Instance.GetSoundObjectAlphaArray(STANDARD_RIM_RADIUS_SPREAD_TIME, STANDARD_RIM_RADIUS_SPREAD_LENGTH);

        Array.Copy(currentRimRadiusArray, 0, rimRadiusArray, 0, currentRimRadiusArray.Length);
        Array.Copy(currentRimAlphaArray, 0, rimAlphaArray, 0, currentRimAlphaArray.Length);

        blockFloorMaterial.SetFloatArray(MAT_RIM_RADIUS_ARRAY_NAME, rimRadiusArray);
        blockFloorMaterial.SetFloatArray(MAT_RIM_ALPHA_ARRAY_NAME, rimAlphaArray);
#if Use_Two_Materials_On_MazeBlock
        blockWallMaterial.SetFloatArray(MAT_RIM_RADIUS_ARRAY_NAME, rimRadiusArray);
        blockWallMaterial.SetFloatArray(MAT_RIM_ALPHA_ARRAY_NAME, rimAlphaArray);
#endif

        foreach(MonsterController mc in monsters) {
            mc.Material.SetFloatArray(MAT_RIM_RADIUS_ARRAY_NAME, rimRadiusArray);
            mc.Material.SetFloatArray(MAT_RIM_ALPHA_ARRAY_NAME, rimAlphaArray);
        }

#if Play_Game_Automatically
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
            Vector3 camPos = monsters[watchIndex].HeadPos + monsters[watchIndex].HeadForward * MazeBlock.BlockSize;
            Vector3 camForward = (monsters[watchIndex].HeadPos - camPos).normalized;
            UtilObjects.Instance.CamPos = Vector3.Lerp(UtilObjects.Instance.CamPos, camPos, Time.deltaTime);
            UtilObjects.Instance.CamForward = Vector3.Lerp(UtilObjects.Instance.CamForward, camForward, Time.deltaTime);
        }
#endif
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
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.25f);

            blockFloorMaterial = mat;
        }
#if Use_Two_Materials_On_MazeBlock
        if(blockWallMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.25f);
            mat.SetColor("_BaseColor", Color.red);

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
    public List<Vector3> GetPath(Vector3 startPos, Vector3 endPos, float rayRadius) {
        Vector3 p1 = startPos;
        Vector3 p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //임의로 player의 높이를 적용
        Vector3 direction = (endPos - startPos).normalized;
        // 벽과 플레이어만 감지하도록 설정
        int mask = (1 << LayerMask.NameToLayer(PlayerController.LayerName)) | 
            (1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));
        if(Physics.CapsuleCast(p1, p2, rayRadius, direction, out tempPathHit, float.MaxValue, mask) && 
            tempPathHit.collider.CompareTag(PlayerController.TagName)) {
            Debug.Log("Can move on straight line to player.");

            return new List<Vector3>() {
                startPos,
                tempPathHit.collider.transform.position
            };
        }

        /* --------------------------------------------------- */

        Vector2Int startCoord = GetMazeCoordinate(startPos);
        Vector2Int endCoord = GetMazeCoordinate(endPos);
        Debug.Log(string.Format("startPos: {0}, endPos: {1}, startCoord: {2}, endCoord: {3}", startPos, endPos, startCoord, endCoord));

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
    public List<Vector3> GetRandomPointPathCompareDistance(Vector3 currentPos, float rayRadius, bool overDistance, float distance) {
        Vector2Int endpoint = overDistance ? GetRandomCoordOverDistance(currentPos, distance) : GetRandomCoordNearbyDistance(currentPos, distance);

        return GetPath(currentPos, GetBlockPos(endpoint), rayRadius);
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
    private void OnWorldSoundAdded(SoundObject so) {
        Vector4[] currentRimPosList = SoundManager.Instance.GetSoundObjectPosArray();
        Array.Copy(currentRimPosList, 0, rimPosArray, 0, currentRimPosList.Length);

        blockFloorMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);
        blockFloorMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
#if Use_Two_Materials_On_MazeBlock
        blockWallMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);
        blockWallMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
#endif

        foreach(MonsterController mc in monsters) {
            mc.Material.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);
            mc.Material.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
        }
    }

    private void OnWorldSoundRemoved() {
        Vector4[] currentRimPosList = SoundManager.Instance.GetSoundObjectPosArray();
        Array.Copy(currentRimPosList, 0, rimPosArray, 0, currentRimPosList.Length);

        blockFloorMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);
        blockFloorMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
#if Use_Two_Materials_On_MazeBlock
        blockWallMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);
        blockWallMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
#endif

        foreach(MonsterController mc in monsters) {
            mc.Material.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);
            mc.Material.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
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
