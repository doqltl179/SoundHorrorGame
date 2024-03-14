using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
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
        // Interact
        Crystal,

        // Pick Up
        HandlingCube, 
        ToyHammer, 

        // Trigger Enter
        Teleport, 
    }

    public const string ROOT_PATH_OF_ITEMS = "Items";
    public const string ROOT_PATH_OF_MATERIALS = "Materials";

    private List<ItemController> items = new List<ItemController>();
    public List<ItemController> Items { get { return items; } }
    public int ItemCount { get { return items.Count; } }
    private Material itemMaterial = null;
    public int CollectedItemCount { get; private set; }

    private List<HandlingCube> handlingCubes = new List<HandlingCube>();
    public List<HandlingCube> HandlingCubes { get { return handlingCubes; } }
    public int HandlingCubeCount { get { return handlingCubes.Count; } }
    //private Material handlingCubeMaterial = null;

    private List<Teleport> teleports = new List<Teleport>();
    public List<Teleport> Teleports { get { return teleports; } }
    public int TeleportCount { get { return teleports.Count; } }
    #endregion

    #region Maze
    private MazeBlock[,] mazeBlocks = null;
    public int LevelWidth { get; private set; }
    public int LevelHeight { get; private set; }
    public int ZoomMaxX { get; private set; }
    public int ZoomMaxY { get; private set; }
    public int HighestZoomMax { get { return ZoomMaxX > ZoomMaxY ? ZoomMaxX : ZoomMaxY; } }

    private readonly Vector3 basicBlockPos = new Vector3(1.0f, 0.0f, 1.0f) * MazeBlock.BlockSize;
    private readonly Vector3 blockPosOffset = MazeBlock.StandardBlockAnchor * MazeBlock.BlockSize;

    private Material blockFloorMaterial = null;
#if Use_Two_Materials_On_MazeBlock
    private Material blockWallMaterial = null;
#endif
    private static readonly int LIST_MAX_LENGTH = 256;

    private PhysicMaterial commonPhysicMaterial = null;

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
    private const string MAT_DRAW_MAZEBLOCK_EDGE_KEY = "DRAW_MAZEBLOCK_EDGE";
    private const string MAT_DRAW_MONSTER_OUTLINE_KEY = "DRAW_OUTLINE";
    protected const string MAT_DRAW_OBJECT_OUTLINE_KEY = "DRAW_OBJECT_OUTLINE";
    #endregion

    private const string MAT_BASE_COLOR_NAME = "_BaseColor";
    private const string MAT_COLOR_STRENGTH_MAX_NAME = "_ColorStrengthMax";
    private const string MAT_RIM_THICKNESS_NAME = "_RimThickness";
    private const string MAT_RIM_THICKNESS_OFFSET_NAME = "_RimThicknessOffset";
    private const string MAT_PLAYER_PAST_POSITION_RADIUS_NAME = "_PlayerPastPosRadius";
    private const string MAT_MAZEBLOCK_EDGE_COLOR_NAME = "_MazeBlockEdgeColor";
    private const string MAT_MAZEBLOCK_EDGE_THICKNESS_NAME = "_MazeBlockEdgeThickness";
    private const string MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME = "_MazeBlockEdgeShowDistance";
    private const string MAT_MONSTER_OUTLINE_THICKNESS_NAME = "_MonsterOutlineThickness";
    private const string MAT_MONSTER_OUTLINE_COLOR_NAME = "_MonsterOutlineColor";
    private const string MAT_OBJECT_OUTLINE_THICKNESS_NAME = "_ObjectOutlineThickness";
    private const string MAT_OBJECT_OUTLINE_COLOR_NAME = "_ObjectOutlineColor";
    #endregion

    public static readonly float STANDARD_RIM_RADIUS_SPREAD_TIME = 5.0f;
    // SpreadTime 동안 MazeBlock을 8칸 이동하기 위해 10을 곱함
    public static readonly float STANDARD_RIM_RADIUS_SPREAD_LENGTH = MazeBlock.BlockSize * 8;
    #endregion

    private float[] noneFromRimRadiusArray;
    private float[] playerRimRadiusArray;
    private float[] monsterRimRadiusArray;
    private float[] itemRimRadiusArray;

    private float[] noneFromRimAlphaArray;
    private float[] playerRimAlphaArray;
    private float[] monsterRimAlphaArray;
    private float[] itemRimAlphaArray;

    private Vector4[] noneFromRimPosArray;
    private Vector4[] playerRimPosArray;
    private Vector4[] monsterRimPosArray;
    private Vector4[] itemRimPosArray;

    private AudioReverbZone reverbZone = null;

    public Action OnItemCollected;



    private void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += WorldSoundRemoved;
    }

    private void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= WorldSoundRemoved;
    }

    private void LateUpdate() {
        noneFromRimRadiusArray = SoundManager.Instance.GetSoundObjectRadiusArray(
                SoundManager.SoundFrom.None,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
        playerRimRadiusArray = SoundManager.Instance.GetSoundObjectRadiusArray(
                SoundManager.SoundFrom.Player,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
        monsterRimRadiusArray = SoundManager.Instance.GetSoundObjectRadiusArray(
                SoundManager.SoundFrom.Monster,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
        itemRimRadiusArray = SoundManager.Instance.GetSoundObjectRadiusArray(
                SoundManager.SoundFrom.Item,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);

        noneFromRimAlphaArray = SoundManager.Instance.GetSoundObjectAlphaArray(
                SoundManager.SoundFrom.None,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
        playerRimAlphaArray = SoundManager.Instance.GetSoundObjectAlphaArray(
                SoundManager.SoundFrom.Player,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
        monsterRimAlphaArray = SoundManager.Instance.GetSoundObjectAlphaArray(
                SoundManager.SoundFrom.Monster,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);
        itemRimAlphaArray = SoundManager.Instance.GetSoundObjectAlphaArray(
                SoundManager.SoundFrom.Item,
                STANDARD_RIM_RADIUS_SPREAD_TIME,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH);

        foreach(MaterialPropertiesGroup group in rimMaterialPropertiesGroups) {
            if(group.CurrentArrayLength > 0) {
                switch(group.From) {
                    case SoundManager.SoundFrom.None: {
                            group.SetUpdateRadiusArray(blockFloorMaterial, noneFromRimRadiusArray);
                            group.SetUpdateAlphaArray(blockFloorMaterial, noneFromRimAlphaArray);
#if Use_Two_Materials_On_MazeBlock
                            group.SetUpdateRadiusArray(blockWallMaterial, noneFromRimRadiusArray);
                            group.SetUpdateAlphaArray(blockWallMaterial, noneFromRimAlphaArray);
#endif

                            foreach(MonsterController mc in monsters) {
                                group.SetUpdateRadiusArray(mc.Material, noneFromRimRadiusArray);
                                group.SetUpdateAlphaArray(mc.Material, noneFromRimAlphaArray);
                            }
                        }
                        break;
                    case SoundManager.SoundFrom.Player: {
                            group.SetUpdateRadiusArray(blockFloorMaterial, playerRimRadiusArray);
                            group.SetUpdateAlphaArray(blockFloorMaterial, playerRimAlphaArray);
#if Use_Two_Materials_On_MazeBlock
                            group.SetUpdateRadiusArray(blockWallMaterial, playerRimRadiusArray);
                            group.SetUpdateAlphaArray(blockWallMaterial, playerRimAlphaArray);
#endif

                            foreach(MonsterController mc in monsters) {
                                group.SetUpdateRadiusArray(mc.Material, playerRimRadiusArray);
                                group.SetUpdateAlphaArray(mc.Material, playerRimAlphaArray);
                            }
                        }
                        break;
                    case SoundManager.SoundFrom.Monster: {
                            group.SetUpdateRadiusArray(blockFloorMaterial, monsterRimRadiusArray);
                            group.SetUpdateAlphaArray(blockFloorMaterial, monsterRimAlphaArray);
#if Use_Two_Materials_On_MazeBlock
                            group.SetUpdateRadiusArray(blockWallMaterial, monsterRimRadiusArray);
                            group.SetUpdateAlphaArray(blockWallMaterial, monsterRimAlphaArray);
#endif

                            foreach(MonsterController mc in monsters) {
                                group.SetUpdateRadiusArray(mc.Material, monsterRimRadiusArray);
                                group.SetUpdateAlphaArray(mc.Material, monsterRimAlphaArray);
                            }
                        }
                        break;
                    case SoundManager.SoundFrom.Item: {
                            group.SetUpdateRadiusArray(blockFloorMaterial, itemRimRadiusArray);
                            group.SetUpdateAlphaArray(blockFloorMaterial, itemRimAlphaArray);
#if Use_Two_Materials_On_MazeBlock
                            group.SetUpdateRadiusArray(blockWallMaterial, itemRimRadiusArray);
                            group.SetUpdateAlphaArray(blockWallMaterial, itemRimAlphaArray);
#endif

                            foreach(MonsterController mc in monsters) {
                                group.SetUpdateRadiusArray(mc.Material, itemRimRadiusArray);
                                group.SetUpdateAlphaArray(mc.Material, itemRimAlphaArray);
                            }
                        }
                        break;
                }
            }
        }

        if(playerMaterialPropertiesGroup.CurrentArrayLength >= 0) {
            // Material의 Player Property는 MazeBlock 오브젝트에만 추가함
            playerMaterialPropertiesGroup.SetUpdateAlphaArray(
                blockFloorMaterial,
                Time.deltaTime,
                STANDARD_RIM_RADIUS_SPREAD_LENGTH); //임의의 길이 설정

            // `blockWallMaterial`에는 pastPos를 입력하지 않을 예정
//#if Use_Two_Materials_On_MazeBlock
//            playerMaterialPropertiesGroup.SetUpdateAlphaArray(
//                blockWallMaterial,
//                Time.deltaTime,
//                STANDARD_RIM_RADIUS_SPREAD_LENGTH); //임의의 길이 설정
//#endif
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

        if(items.Count > 0) {
            foreach(ItemController ic in items) {
                Destroy(ic.gameObject);
            }

            items.Clear();
        }

        if(handlingCubes.Count > 0) {
            foreach(HandlingCube hc in handlingCubes) {
                Destroy(hc.gameObject);
            }

            handlingCubes.Clear();
        }

        if(teleports.Count > 0) {
            foreach(Teleport t in teleports) {
                Destroy(t.gameObject);
            }

            teleports.Clear();
        }

        playerMaterialPropertiesGroup.ClearArray();
    }

    public void LevelToEmpty() {
        if(mazeBlocks == null) {
            Debug.Log("Maze not exist.");

            return;
        }

        MazeCreator.CreateEmptyMaze(LevelWidth, LevelHeight);

        for(int x = 0; x < LevelWidth; x++) {
            for(int y = 0; y < LevelHeight; y++) {
                mazeBlocks[x, y].WallInfo = MazeCreator.Maze[x, y].WallInfo;
            }
        }
    }

    public void LoadLevel(int width, int height, bool createEmpty = false) {
        if(createEmpty) MazeCreator.CreateEmptyMaze(width, height);
        else MazeCreator.CreateMaze(width, height);

        if(blockFloorMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));

            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.2f);
            mat.SetFloat(MAT_RIM_THICKNESS_OFFSET_NAME, 1.0f);
            foreach(MaterialPropertiesGroup group in rimMaterialPropertiesGroups) {
                mat.SetVector(group.MAT_COLOR_NAME, group.Color);
            }

            mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
            mat.EnableKeyword(MAT_DRAW_RIM_KEY);
            mat.EnableKeyword(MAT_DRAW_PLAYER_PAST_POSITION_KEY);

            mat.SetFloat(MAT_PLAYER_PAST_POSITION_RADIUS_NAME, 0.15f);

            blockFloorMaterial = mat;
        }
#if Use_Two_Materials_On_MazeBlock
        if(blockWallMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));

            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.2f);
            mat.SetFloat(MAT_RIM_THICKNESS_OFFSET_NAME, 1.0f);
            mat.SetColor(MAT_BASE_COLOR_NAME, Color.black);
            foreach(MaterialPropertiesGroup group in rimMaterialPropertiesGroups) {
                mat.SetVector(group.MAT_COLOR_NAME, group.Color);
            }

            mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
            mat.EnableKeyword(MAT_DRAW_RIM_KEY);
            mat.EnableKeyword(MAT_DRAW_MAZEBLOCK_EDGE_KEY);

            mat.SetFloat(MAT_MAZEBLOCK_EDGE_THICKNESS_NAME, MazeBlock.BlockSize * 0.0002f);
            mat.SetFloat(MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME, MazeBlock.BlockSize * 0.75f);

            blockWallMaterial = mat;
        }
#endif

        if(commonPhysicMaterial == null) {
            PhysicMaterial pm = new PhysicMaterial();
            //pm.dynamicFriction = 0.2f;
            pm.dynamicFriction = 0.01f;
            //pm.staticFriction = 0.2f;
            pm.staticFriction = 0.01f;
            pm.frictionCombine = PhysicMaterialCombine.Minimum;

            commonPhysicMaterial = pm;
        }

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
                mb.SetPhysicMaterial(commonPhysicMaterial);

                mazeBlocks[x, z] = mb;
            }
        }

        LevelWidth = width;
        LevelHeight = height;

        ZoomMaxX = GetMaxZoom(width);
        ZoomMaxY = GetMaxZoom(height);

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
        startPos = new Vector3(startPos.x, 0, startPos.z);
        endPos = new Vector3(endPos.x, 0, endPos.z);

        #region Check Straight Path
        int rayMask = (1 << LayerMask.NameToLayer(MazeBlock.WallLayerName)) +
            (1 << LayerMask.NameToLayer(MazeBlock.EdgeLayerName)) +
            (1 << LayerMask.NameToLayer(PlayerController.LayerName));
        Vector3 p1 = startPos;
        Vector3 p2 = startPos + Vector3.up * PlayerController.PlayerHeight;
        Vector3 rayDirection = (endPos - startPos).normalized;
        if(Physics.CapsuleCast(p1, p2, rayRadius, rayDirection, out tempPathHit, float.MaxValue, rayMask)) {
            if(tempPathHit.rigidbody != null && tempPathHit.rigidbody.CompareTag(PlayerController.TagName)) {
                Debug.Log("Hit Player");

                return new List<Vector3>() { startPos, new Vector3(endPos.x, 0.0f, endPos.z) };
            }
        }
        #endregion

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

        pathList.Add(endPos); //마지막 위치 설정
        //pathList.Add(new Vector3(endPos.x, 0.0f, endPos.z));
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

    //public Vector2Int GetMazeCoordinate(Vector3 pos) =>
    //    new Vector2Int(Mathf.FloorToInt(pos.x / MazeBlock.BlockSize), Mathf.FloorToInt(pos.z / MazeBlock.BlockSize));

    public Vector2Int GetMazeCoordinate(Vector3 pos, int zoom = 0) {
        int includeBlockCount = Mathf.FloorToInt(Mathf.Pow(2, zoom));
        float calculatedCoordSize = MazeBlock.BlockSize * includeBlockCount;
        return new Vector2Int(Mathf.FloorToInt(pos.x / calculatedCoordSize), Mathf.FloorToInt(pos.z / calculatedCoordSize));
    }

    /// <summary>
    /// <br/> zoomInCoord 구역에 해당하는 zoom이 0일 때의 startCoord, endCoord를 반환한다.
    /// <br/> startCoord: 포함
    /// <br/> endCoord: 미포함
    /// </summary>
    /// <param name="zoomInCoord"></param>
    /// <param name="zoom"></param>
    /// <param name="startCoord"></param>
    /// <param name="endCoord"></param>
    /// <returns></returns>
    public void GetStartEndCoordOnZoomInCoord(Vector2Int zoomInCoord, int zoom, out Vector2Int startCoord, out Vector2Int endCoord) {
        int includeBlockCount = Mathf.FloorToInt(Mathf.Pow(2, zoom));
        startCoord = zoomInCoord * includeBlockCount;
        endCoord = startCoord + Vector2Int.one * includeBlockCount;
        if(endCoord.x > LevelWidth) endCoord.x = LevelWidth;
        if(endCoord.y > LevelHeight) endCoord.y = LevelHeight;
    }

    public int GetMaxZoom(int length) {
        int includeBlockCount = 0;
        int zoom = 0;
        while(true) {
            includeBlockCount = Mathf.FloorToInt(Mathf.Pow(2, zoom));
            if(includeBlockCount >= length) {
                break;
            }

            zoom++;
        }

        return zoom;
    }

    public Vector2Int GetLevelSize(int zoom) {
        float includeBlockCount = Mathf.Floor(Mathf.Pow(2, zoom));
        int x = Mathf.CeilToInt(LevelWidth / includeBlockCount);
        int y = Mathf.CeilToInt(LevelHeight / includeBlockCount);
        return new Vector2Int(x, y);
    }

    public bool IsCoordInLevelSize(Vector2Int zoomInCoord, int zoom) {
        Vector2Int calculatedLevelSize = GetLevelSize(zoom);
        return (0 <= zoomInCoord.x && zoomInCoord.x < calculatedLevelSize.x && 0 <= zoomInCoord.y && zoomInCoord.y < calculatedLevelSize.y);
    }

    public bool IsCoordIncludeInZoomInCoord(Vector2Int coord, Vector2Int zoomInCoord, int zoom) {
        Vector2Int startCoord;
        Vector2Int endCoord;
        GetStartEndCoordOnZoomInCoord(zoomInCoord, zoom, out startCoord, out endCoord);

        return (startCoord.x <= coord.x && coord.x < endCoord.x && startCoord.y <= coord.y && coord.y < endCoord.y);
    }

    public void DestroyMonster(MonsterController monster) {
        monsters.Remove(monster);

        Destroy(monster.gameObject);
    }

    public void ResetMonsterOnLevel(int monsterIndex, Vector2Int coord, int zoom = 0, Vector2Int[] ignoreCoords = null) {
        MonsterController mc = monsters[monsterIndex];
        if(mc == null) {
            Debug.LogWarning($"Monster not exist. index: {monsterIndex}");

            return;
        }

        if(zoom <= 0) {
            mc.Pos = GetBlockPos(coord);
        }
        else {
            Vector2Int startCoord;
            Vector2Int endCoord;
            GetStartEndCoordOnZoomInCoord(coord, zoom, out startCoord, out endCoord);

            Vector2Int randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
            while(true) {
                if(ignoreCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
                }
                else {
                    break;
                }
            }
            mc.Pos = GetBlockPos(randomCoord);
        }
    }

    public void AddMonsterOnLevel(MonsterType type, Vector2Int coord, int zoom = 0, Vector2Int[] ignoreCoords = null) {
        GameObject resource = ResourceLoader.GetResource<GameObject>(Path.Combine(ROOT_PATH_OF_MONSTERS, type.ToString()));
        if(resource == null) {
            Debug.LogError($"Monster Resource not found. type: {type}");

            return;
        }

        GameObject go = Instantiate(resource, transform);
        if(zoom <= 0) {
            go.transform.position = GetBlockPos(coord);
        }
        else {
            Vector2Int startCoord;
            Vector2Int endCoord;
            GetStartEndCoordOnZoomInCoord(coord, zoom, out startCoord, out endCoord);

            Vector2Int randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
            while(true) {
                if(ignoreCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
                }
                else {
                    break;
                }
            }
            go.transform.position = GetBlockPos(randomCoord);
        }

        MonsterController mc = go.GetComponent<MonsterController>();
        mc.Material.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 2.0f);
        mc.Material.SetFloat(MAT_RIM_THICKNESS_OFFSET_NAME, 4.0f);
        mc.Material.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
        mc.Material.EnableKeyword(MAT_DRAW_RIM_KEY);
        mc.Material.EnableKeyword(MAT_DRAW_MONSTER_OUTLINE_KEY);

        if(type == MonsterType.Cloudy) mc.Material.SetColor(MAT_BASE_COLOR_NAME, Color.white * 30f / 255f);

        monsters.Add(mc);
    }

    /// <summary>
    /// <br/> overDistance == true : currentPos를 기준으로 distance 보다 먼 좌표의 경로 반환
    /// <br/> overDistance == false : currentPos를 기준으로 distance 보다 가까운 좌표의 경로 반환
    /// </summary>
    public List<Vector3> GetRandomPointPathCompareDistance(Vector3 currentPos, float rayRadius, bool overDistance, float distance) {
        Vector2Int endpoint = overDistance ? GetRandomCoordOverDistance(currentPos, distance) : GetRandomCoordNearbyDistance(currentPos, distance);

        return GetPath(currentPos, GetBlockPos(endpoint), rayRadius);
    }

    public void ResetItemOnLevel(int itemIndex, Vector2Int coord, int zoom = 0, Vector2Int[] ignoreCoords = null) {
        ItemController ic = items[itemIndex];
        if(ic == null) {
            Debug.LogWarning($"Item not exist. index: {itemIndex}");

            return;
        }

        if(zoom <= 0) {
            ic.Pos = GetBlockPos(coord);
        }
        else {
            Vector2Int startCoord;
            Vector2Int endCoord;
            GetStartEndCoordOnZoomInCoord(coord, zoom, out startCoord, out endCoord);

            Vector2Int randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
            while(true) {
                if(ignoreCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
                }
                else {
                    break;
                }
            }
            ic.Pos = GetBlockPos(randomCoord);
        }
    }

    public void AddItemOnLevel(ItemType type, Vector2Int coord, int zoom = 0, Vector2Int[] ignoreCoords = null) {
        GameObject resource = ResourceLoader.GetResource<GameObject>(Path.Combine(ROOT_PATH_OF_ITEMS, type.ToString()));
        if(resource == null) {
            Debug.LogError($"Monster Resource not found. type: {type}");

            return;
        }

        if(itemMaterial == null) {
            Material material = ResourceLoader.GetResource<Material>(Path.Combine(ROOT_PATH_OF_MATERIALS, type.ToString()));
            if(material == null) {
                Debug.LogError($"Material Resource not found. type: {type}");

                return;
            }

            itemMaterial = new Material(material.shader);
            itemMaterial.CopyMatchingPropertiesFromMaterial(material);
        }

        GameObject go = Instantiate(resource, transform);
        if(zoom <= 0) {
            go.transform.position = GetBlockPos(coord);
        }
        else {
            Vector2Int startCoord;
            Vector2Int endCoord;
            GetStartEndCoordOnZoomInCoord(coord, zoom, out startCoord, out endCoord);

            Vector2Int randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
            while(true) {
                if(ignoreCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
                }
                else {
                    break;
                }
            }
            go.transform.position = GetBlockPos(randomCoord);
        }

        ItemController ic = go.GetComponent<ItemController>();
        ic.SetMaterial(itemMaterial);

        items.Add(ic);
    }

    public void ResetPickupItemOnLevel(int itemIndex, Vector2Int coord, int zoom = 0, Vector2Int[] ignoreCoords = null) {
        HandlingCube hc = handlingCubes[itemIndex];
        if(hc == null) {
            Debug.LogWarning($"Pickup Item not found. index: ${itemIndex}");

            return;
        }

        if(zoom <= 0) {
            hc.Pos = GetBlockPos(coord);
        }
        else {
            Vector2Int startCoord;
            Vector2Int endCoord;
            GetStartEndCoordOnZoomInCoord(coord, zoom, out startCoord, out endCoord);

            Vector2Int randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
            while(true) {
                if(ignoreCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
                }
                else {
                    break;
                }
            }
            hc.Pos = GetBlockPos(randomCoord);
        }
    }

    public void AddPickupItemOnLevel(ItemType type, Vector2Int coord, int zoom = 0, Vector2Int[] ignoreCoords = null) {
        GameObject resource = ResourceLoader.GetResource<GameObject>(Path.Combine(ROOT_PATH_OF_ITEMS, type.ToString()));
        if(resource == null) {
            Debug.LogError($"Monster Resource not found. type: {type}");

            return;
        }

        GameObject go = Instantiate(resource, transform);
        if(zoom <= 0) {
            go.transform.position = GetBlockPos(coord);
        }
        else {
            Vector2Int startCoord;
            Vector2Int endCoord;
            GetStartEndCoordOnZoomInCoord(coord, zoom, out startCoord, out endCoord);

            Vector2Int randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
            while(true) {
                if(ignoreCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
                }
                else {
                    break;
                }
            }
            go.transform.position = GetBlockPos(randomCoord);
        }

        HandlingCube hc = go.GetComponent<HandlingCube>();
        //hc.SetMaterial(handlingCubeMaterial);

        handlingCubes.Add(hc);
    }

    public void ResetTeleportOnLevel(int index, Vector2Int coord, int zoom = 0, Vector2Int[] ignoreCoords = null) {
        Teleport t = teleports[index];
        if(t == null) {
            Debug.LogWarning($"Teleport not exist. index: {index}");

            return;
        }

        if(zoom <= 0) {
            t.Pos = GetBlockPos(coord);
        }
        else {
            Vector2Int startCoord;
            Vector2Int endCoord;
            GetStartEndCoordOnZoomInCoord(coord, zoom, out startCoord, out endCoord);

            // Item과 겹치지 않게 생성
            Vector2Int randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
            while(true) {
                if(ignoreCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
                }
                else {
                    break;
                }
            }
            t.Pos = GetBlockPos(randomCoord);
        }
    }

    public void AddTeleportOnLevel(ItemType type, Vector2Int coord, int zoom = 0, Vector2Int[] ignoreCoords = null) {
        GameObject resource = ResourceLoader.GetResource<GameObject>(Path.Combine(ROOT_PATH_OF_ITEMS, type.ToString()));
        if(resource == null) {
            Debug.LogError($"Monster Resource not found. type: {type}");

            return;
        }

        GameObject go = Instantiate(resource, transform);
        if(zoom <= 0) {
            go.transform.position = GetBlockPos(coord);
        }
        else {
            Vector2Int startCoord;
            Vector2Int endCoord;
            GetStartEndCoordOnZoomInCoord(coord, zoom, out startCoord, out endCoord);

            // Item과 겹치지 않게 생성
            Vector2Int randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
            while(true) {
                if(ignoreCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
                }
                else {
                    break;
                }
            }
            go.transform.position = GetBlockPos(randomCoord);
        }

        Teleport t = go.GetComponent<Teleport>();
        //hc.SetMaterial(handlingCubeMaterial);

        teleports.Add(t);
    }

    public void PlayPickupItems() {
        foreach(HandlingCube hc in handlingCubes) {
            hc.Play();
        }
    }

    public void StopPickupItems() {
        foreach(HandlingCube hc in handlingCubes) {
            hc.Stop();
        }
    }

    public void PlayItems() {
        foreach(ItemController ic in items) {
            ic.Play();
        }
    }

    public void StopItems() {
        foreach(ItemController ic in items) {
            ic.Stop();
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

    public Vector2Int[] GetAllMonsterCoords() => monsters.Select(t => GetMazeCoordinate(t.Pos)).ToArray();

    public Vector2Int[] GetAllItemCoords() => items.Select(t => GetMazeCoordinate(t.Pos)).ToArray();

    public Vector2Int[] GetAllPickupItemCoords() => handlingCubes.Select(t => GetMazeCoordinate(t.Pos)).ToArray();

    public Vector2Int[] GetAllTeleportCoords() => teleports.Select(t => GetMazeCoordinate(t.Pos)).ToArray();

    public void AddPlayerPosInMaterialProperty(Vector3 pos) {
        playerMaterialPropertiesGroup.AddPos(new Vector4(pos.x, pos.y, pos.z));
        playerMaterialPropertiesGroup.SetPosArray(blockFloorMaterial);
//#if Use_Two_Materials_On_MazeBlock
//        playerMaterialPropertiesGroup.SetPosArray(blockWallMaterial);
//#endif
    }

    public MazeBlock GetMazeBlock(Vector2Int coord) => GetMazeBlock(coord.x, coord.y);
    public MazeBlock GetMazeBlock(int x, int y) => mazeBlocks[x, y];

    public void CollectItem(ItemController collectingItem) {
        int itemIndex = items.IndexOf(collectingItem);
        if(itemIndex >= 0) {
            items.RemoveAt(itemIndex);
        }

        StartCoroutine(CollectItemAnimation(collectingItem));

        OnItemCollected?.Invoke();
    }

    private IEnumerator CollectItemAnimation(ItemController collectingItem) {
        collectingItem.Explode(10f);

        yield return new WaitForSeconds(3.0f);

        Destroy(collectingItem.gameObject);
    }

    #region Material Property Setting Func
    public void SetUseBaseColor(Material mat, bool value) {
        if(value) mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
        else mat.DisableKeyword(MAT_USE_BASE_COLOR_KEY);
    }
    public void SetDrawRim(Material mat, bool value) {
        if(value) mat.EnableKeyword(MAT_DRAW_RIM_KEY);
        else mat.DisableKeyword(MAT_DRAW_RIM_KEY);
    }
    public void SetDrawPlayerPastPos(Material mat, bool value) {
        if(value) mat.EnableKeyword(MAT_DRAW_PLAYER_PAST_POSITION_KEY);
        else mat.DisableKeyword(MAT_DRAW_PLAYER_PAST_POSITION_KEY);
    }
    public void SetDrawMazeBlockEdge(Material mat, bool value) {
        if(value) mat.EnableKeyword(MAT_DRAW_MAZEBLOCK_EDGE_KEY);
        else mat.DisableKeyword(MAT_DRAW_MAZEBLOCK_EDGE_KEY);
    }
    public void SetDrawMonsterOutline(Material mat, bool value) {
        if(value) mat.EnableKeyword(MAT_DRAW_MONSTER_OUTLINE_KEY);
        else mat.DisableKeyword(MAT_DRAW_MONSTER_OUTLINE_KEY);
    }
    public void SetDrawObjectOutline(Material mat, bool value) {
        if(value) mat.EnableKeyword(MAT_DRAW_OBJECT_OUTLINE_KEY);
        else mat.DisableKeyword(MAT_DRAW_OBJECT_OUTLINE_KEY);
    }

    public void SetBaseColor(Material mat, Color color) => mat.SetColor(MAT_BASE_COLOR_NAME, color);
    public Color GetBaseColor(Material mat) => mat.GetColor(MAT_BASE_COLOR_NAME);

    public void SetMazeBlockEdgeColor(Material mat, Color color) => mat.SetColor(MAT_MAZEBLOCK_EDGE_COLOR_NAME, color);
    public void SetMazeBlockEdgeThickness(Material mat, float value) => mat.SetFloat(MAT_MAZEBLOCK_EDGE_THICKNESS_NAME, value);
    public void SetMazeBlockEdgeShowDistance(Material mat, float value) => mat.SetFloat(MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME, value);

    public void SetMonsterOutlineColor(Material mat, Color color) => mat.SetColor(MAT_MONSTER_OUTLINE_COLOR_NAME, color);
    public void SetMonsterOutlineThickness(Material mat, float value) => mat.SetFloat(MAT_MONSTER_OUTLINE_THICKNESS_NAME, value);

    public void SetObjectOutlineColor(Material mat, Color color) => mat.SetColor(MAT_OBJECT_OUTLINE_COLOR_NAME, color);
    public void SetObjectOutlineThickness(Material mat, float value) => mat.SetFloat(MAT_OBJECT_OUTLINE_THICKNESS_NAME, value);
    #endregion
    #endregion

    #region Action
    private void WorldSoundAdded(SoundObject so, SoundManager.SoundFrom from) {
        MaterialPropertiesGroup fromGroup = rimMaterialPropertiesGroups.Where(t => t.From == from).FirstOrDefault();
        List<Material> updateMats = new List<Material>();

        updateMats.Add(blockFloorMaterial);
#if Use_Two_Materials_On_MazeBlock
        updateMats.Add(blockWallMaterial);
#endif
        foreach(MonsterController mc in monsters) {
            updateMats.Add(mc.Material);
        }

        switch(from) {
            case SoundManager.SoundFrom.None: {
                    noneFromRimPosArray = SoundManager.Instance.GetSoundObjectPosArray(from);
                    fromGroup.SetUpdatePosArray(updateMats, noneFromRimPosArray);
                }
                break;
            case SoundManager.SoundFrom.Player: {
                    playerRimPosArray = SoundManager.Instance.GetSoundObjectPosArray(from);
                    fromGroup.SetUpdatePosArray(updateMats, playerRimPosArray);
                }
                break;
            case SoundManager.SoundFrom.Monster: {
                    monsterRimPosArray = SoundManager.Instance.GetSoundObjectPosArray(from);
                    fromGroup.SetUpdatePosArray(updateMats, monsterRimPosArray);
                }
                break;
            case SoundManager.SoundFrom.Item: {
                    itemRimPosArray = SoundManager.Instance.GetSoundObjectPosArray(from);
                    fromGroup.SetUpdatePosArray(updateMats, itemRimPosArray);
                }
                break;
        }
    }

    private void WorldSoundRemoved(SoundManager.SoundFrom from) {
        MaterialPropertiesGroup fromGroup = rimMaterialPropertiesGroups.Where(t => t.From == from).FirstOrDefault();
        List<Material> updateMats = new List<Material>();

        updateMats.Add(blockFloorMaterial);
#if Use_Two_Materials_On_MazeBlock
        updateMats.Add(blockWallMaterial);
#endif
        foreach(MonsterController mc in monsters) {
            updateMats.Add(mc.Material);
        }

        switch(from) {
            case SoundManager.SoundFrom.None: {
                    noneFromRimPosArray = SoundManager.Instance.GetSoundObjectPosArray(from);
                    fromGroup.SetUpdatePosArray(updateMats, noneFromRimPosArray);
                }
                break;
            case SoundManager.SoundFrom.Player: {
                    playerRimPosArray = SoundManager.Instance.GetSoundObjectPosArray(from);
                    fromGroup.SetUpdatePosArray(updateMats, playerRimPosArray);
                }
                break;
            case SoundManager.SoundFrom.Monster: {
                    monsterRimPosArray = SoundManager.Instance.GetSoundObjectPosArray(from);
                    fromGroup.SetUpdatePosArray(updateMats, monsterRimPosArray);
                }
                break;
            case SoundManager.SoundFrom.Item: {
                    itemRimPosArray = SoundManager.Instance.GetSoundObjectPosArray(from);
                    fromGroup.SetUpdatePosArray(updateMats, itemRimPosArray);
                }
                break;
        }
    }
    #endregion

    public Vector2Int[] GetAllCoordsOfTeleports(int zoom = 0) => teleports.Select(t => GetMazeCoordinate(t.Pos, zoom)).ToArray();

    public List<Vector2Int> GetAllCoordsNotExistMonsters(int zoom = 0) {
        List<Vector2Int> coords = new List<Vector2Int>();

        // zoomInCoords
        Vector2Int[] monsterCoords = monsters.Select(t => GetMazeCoordinate(t.Pos, zoom)).ToArray();

        Vector2Int calculatedLevelSize = GetLevelSize(zoom);
        for(int x = 0; x < calculatedLevelSize.x; x++) {
            for(int y = 0; y < calculatedLevelSize.y; y++) {
                if(!monsterCoords.Where(t => t.x == x && t.y == y).Any()) {
                    coords.Add(new Vector2Int(x, y));
                }
            }
        }

        return coords;
    }

    public Vector2Int GetRandomCoordOnZoomInCoordArea(Vector2Int zoomInCoord, int zoom) {
        if(!IsCoordInLevelSize(zoomInCoord, zoom)) {
            Debug.LogWarning($"Out of range. LevelWidth: {LevelWidth}, LevelHeight: {LevelHeight}, zoomInCoord: {zoomInCoord}, zoom: {zoom}");

            return Vector2Int.one * -1;
        }

        Vector2Int startCoord;
        Vector2Int endCoord;
        GetStartEndCoordOnZoomInCoord(zoomInCoord, zoom, out startCoord, out endCoord);

        return new Vector2Int(Random.Range(startCoord.x, endCoord.x), Random.Range(startCoord.y, endCoord.y));
    }

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
            r.reverbPreset = AudioReverbPreset.Cave;

            reverbZone = r;
        }

        float mazeLengthW = GetMazeLengthWidth();
        float mazeLengthH = GetMazeLengthHeight();
        reverbZone.minDistance = (mazeLengthW > mazeLengthH ? mazeLengthW : mazeLengthH) * 0.5f * 1.414f;
        reverbZone.maxDistance = reverbZone.minDistance * 2.0f;

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
        public void ClearArray() {
            if(CurrentArrayLength > 0) {
                RemovePos(CurrentArrayLength);
            }
        }

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

        public void SetPosArray(Material mat) {
            mat.SetInteger(MAT_ARRAY_LENGTH_NAME, CurrentArrayLength);
            mat.SetVectorArray(MAT_POSITION_ARRAY_NAME, PosArray);
        }
        #endregion

        #region Rim Property Update Func
        public void SetUpdatePosArray(List<Material> mats, Vector4[] array) {
            Array.Copy(array, 0, PosArray, 0, array.Length);
            CurrentArrayLength = array.Length;
            foreach(Material mat in mats) {
                mat.SetInteger(MAT_ARRAY_LENGTH_NAME, CurrentArrayLength);
                mat.SetVectorArray(MAT_POSITION_ARRAY_NAME, PosArray);
            }
        }

        // 매 프레임마다 업데이트 해야하는 properties
        public void SetUpdateRadiusArray(Material mat, float[] array) {
            Array.Copy(array, 0, RadiusArray, 0, array.Length);
            mat.SetFloatArray(MAT_RADIUS_ARRAY_NAME, RadiusArray);
        }

        // 매 프레임마다 업데이트 해야하는 properties
        public void SetUpdateAlphaArray(Material mat, float[] array) {
            Array.Copy(array, 0, AlphaArray, 0, array.Length);
            mat.SetFloatArray(MAT_ALPHA_ARRAY_NAME, AlphaArray);
        }
        #endregion

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
