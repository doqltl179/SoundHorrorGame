using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelLoader : GenericSingleton<LevelLoader> {
    private MazeBlock[,] mazeBlocks = null;
    public int LevelWidth { get; private set; }
    public int LevelHeight { get; private set; }

    private readonly Vector3 basicBlockPos = new Vector3(1.0f, 0.0f, 1.0f) * MazeBlock.BlockSize;
    private readonly Vector3 blockPosOffset = MazeBlock.StandardBlockAnchor * MazeBlock.BlockSize;

    private Material blockFloorMaterial = null;
    private Material blockWallMaterial = null;
    private readonly string MAT_RIM_THICKNESS_NAME = "_RimThickness";
    private readonly string MAT_RIM_ARRAY_LENGTH_NAME = "_RimArrayLength";
    private readonly string MAT_RIM_POSITION_ARRAY_NAME = "_RimPosArray";
    private readonly string MAT_RIM_RADIUS_ARRAY_NAME = "_RimRadiusArray";
    //private readonly string MAT_RIM_THICKNESS_ARRAY_NAME = "_RimThicknessArray";
    private readonly string MAT_RIM_ALPHA_ARRAY_NAME = "_RimAlphaArray";

    private static readonly int LIST_MAX_LENGTH = 256;
    private Vector4[] rimPosArray = new Vector4[LIST_MAX_LENGTH];
    private float[] rimRadiusArray = new float[LIST_MAX_LENGTH];
    //private float[] rimThicknessArray = new float[LIST_MAX_LENGTH];
    private float[] rimAlphaArray = new float[LIST_MAX_LENGTH];

    public static readonly float STANDARD_RIM_RADIUS_SPREAD_TIME = 10.0f;
    // SpreadTime ���� MazeBlock�� 10ĭ �̵��ϱ� ���� 10�� ����
    public static readonly float STANDARD_RIM_RADIUS_SPREAD_LENGTH = MazeBlock.BlockSize * 10;

    private AudioReverbZone reverbZone = null;



    private void Awake() {
        DontDestroyOnLoad(gameObject);

        SoundManager.Instance.OnWorldSoundAdded += OnWorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += OnWorldSoundRemoved;
    }

    private void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= OnWorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= OnWorldSoundRemoved;
    }

    private void Update() {
        float[] currentRimRadiusArray = SoundManager.Instance.GetSoundObjectRadiusArray(STANDARD_RIM_RADIUS_SPREAD_TIME, STANDARD_RIM_RADIUS_SPREAD_LENGTH);
        float[] currentRimAlphaArray = SoundManager.Instance.GetSoundObjectAlphaArray(STANDARD_RIM_RADIUS_SPREAD_TIME, STANDARD_RIM_RADIUS_SPREAD_LENGTH);

        Array.Copy(currentRimRadiusArray, 0, rimRadiusArray, 0, currentRimRadiusArray.Length);
        Array.Copy(currentRimAlphaArray, 0, rimAlphaArray, 0, currentRimAlphaArray.Length);

        blockFloorMaterial.SetFloatArray(MAT_RIM_RADIUS_ARRAY_NAME, rimRadiusArray);
        blockFloorMaterial.SetFloatArray(MAT_RIM_ALPHA_ARRAY_NAME, rimAlphaArray);
        blockWallMaterial.SetFloatArray(MAT_RIM_RADIUS_ARRAY_NAME, rimRadiusArray);
        blockWallMaterial.SetFloatArray(MAT_RIM_ALPHA_ARRAY_NAME, rimAlphaArray);
    }

    #region Utility
    public void LoadLevel(int width, int height, bool createEmpty = false) {
        if(createEmpty) MazeCreator.CreateEmptyMaze(width, height);
        else MazeCreator.CreateMaze(width, height);

        string componentName = typeof(MazeBlock).Name;
        GameObject resourceObj = ResourceLoader.GetResource<GameObject>(componentName);

        if(blockFloorMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/MazeBlock"));
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.25f);

            blockFloorMaterial = mat;
        }
        if(blockWallMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/MazeBlock"));
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.25f);
            mat.SetColor("_BaseColor", Color.red);

            blockWallMaterial = mat;
        }


        mazeBlocks = new MazeBlock[width, height];
        for(int x = 0; x < width; x++) {
            for(int z = 0; z < height; z++) {
                GameObject go = Instantiate(resourceObj, transform);
                go.name = componentName;

                go.transform.position = GetBlockPos(x, z);
                go.transform.localScale = MazeBlock.StandardBlockScale;

                MazeBlock mb = go.GetComponent<MazeBlock>();
                mb.WallInfo = MazeCreator.Maze[x, z];
                mb.SetMaterial(blockFloorMaterial, blockWallMaterial);

                mazeBlocks[x, z] = mb;
            }
        }

        LevelWidth = width;
        LevelHeight = height;

        SetReverbZone();
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
        Vector3 p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //���Ƿ� player�� ���̸� ����
        Vector3 direction = (endPos - startPos).normalized;
        // ���� �÷��̾ �����ϵ��� ����
        int mask = LayerMask.NameToLayer(PlayerController.LayerName) | LayerMask.NameToLayer(MazeBlock.WallLayerName);
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
            // CheckList���� endCoord�� ���� ����� ��θ� ���� Helper�� ã�´�
            mostCloseHelperIndex = GetMostCloseHelperIndex(checkCoordList);
            mostCloseHelper = checkCoordList[mostCloseHelperIndex];

            // ������ ã�� Helper�� PastList�� �ְ� CheckList���� �����Ѵ�
            pastCoordList.Add(mostCloseHelper);
            checkCoordList.RemoveAt(mostCloseHelperIndex);

            // ���� PastList�� �߰��� Helper�� Point�� endCoord�� ���ٸ� ���� ����
            if(IsSameVec2Int(mostCloseHelper.Coord, endCoord)) {
                Debug.Log("Successed find path!");

                break;
            }

            // ������ ã�� Helper�� �������� �� �� �ִ� ���� CheckList�� �߰��Ѵ�
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

            // ���� CheckList�� Helper�� ���ٸ� null ���� return
            if(checkCoordList.Count <= 0) {
                Debug.LogError("Path not found.");

                return null;
            }
        }

        // Ž���� ���� ���� coord ����
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

        // ��� �ܼ�ȭ
        List<PathHelper> simplePathHelperList = new List<PathHelper>();
        simplePathHelperList.Add(pathHelperList[0]); //ó�� ��ǥ �߰�
        PathHelper moveDirectionChecker = pathHelperList[1];
        for(int i = 2; i < pathHelperList.Count; i++) {
            if(moveDirectionChecker.MovedDirection != pathHelperList[i].MovedDirection) {
                simplePathHelperList.Add(pathHelperList[i - 1]);
                moveDirectionChecker = pathHelperList[i];
            }
        }
        simplePathHelperList.Add(pathHelperList[pathHelperList.Count - 1]); //������ ��ǥ �߰�

        // coord List�� Vector3 List�� ��ȯ
        // rayRadius�� ����Ͽ� CornerPoint�� ����
        List<Vector3> pathList = new List<Vector3>();
        pathList.Add(startPos); //ó�� ��ġ ����
        PathHelper currentHelper;
        PathHelper nextHelper;
        MazeBlock tempBlock;
        for(int i = 1; i < simplePathHelperList.Count - 1; i++) {
            currentHelper = simplePathHelperList[i];
            nextHelper = simplePathHelperList[i + 1];
            tempBlock = mazeBlocks[currentHelper.Coord.x, currentHelper.Coord.y];
            pathList.Add(tempBlock.GetCornerPoint(currentHelper.MovedDirection, nextHelper.MovedDirection, rayRadius));
        }
        pathList.Add(endPos); //������ ��ġ ����

        // ���� �ܼ�ȭ
        //if(pathList.Count > 2) {
        //    Vector3 tempPast;
        //    Vector3 tempNext;
        //    float distance;
        //    for(int i = 1; i < pathList.Count - 1; i++) {
        //        tempPast = pathList[i - 1];
        //        tempNext = pathList[i + 1];

        //        p1 = tempPast;
        //        p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //���Ƿ� player�� ���̸� ����
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
        if(path.Count < 2) {
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
#endregion

    #region Action
    private void OnWorldSoundAdded() {
        Vector4[] currentRimPosList = SoundManager.Instance.GetSoundObjectPosArray();
        blockFloorMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);
        blockWallMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);

        Array.Copy(currentRimPosList, 0, rimPosArray, 0, currentRimPosList.Length);
        blockFloorMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
        blockWallMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
    }

    private void OnWorldSoundRemoved() {
        Vector4[] currentRimPosList = SoundManager.Instance.GetSoundObjectPosArray();
        blockFloorMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);
        blockWallMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);

        Array.Copy(currentRimPosList, 0, rimPosArray, 0, currentRimPosList.Length);
        blockFloorMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
        blockWallMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
    }
    #endregion

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

    #region ��ã�� Util Func
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
        /// <param name="accumulatedDistance">������ �Ÿ�</param>
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
