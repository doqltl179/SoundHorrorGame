using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : GenericSingleton<LevelLoader> {
    private MazeBlock[,] mazeBlcoks = null;
    public int LevelWidth { get; private set; }
    public int LevelHeight { get; private set; }

    private readonly Vector3 basicBlockPos = 
        new Vector3(
            1.0f * MazeBlock.BlockScale.x, 
            0.0f * MazeBlock.BlockScale.y, 
            1.0f * MazeBlock.BlockScale.z
            ) * MazeBlock.BasicBlockSize;
    private readonly Vector3 blockPosOffset = 
        new Vector3(
            MazeBlock.BlockAnchor.x * MazeBlock.BlockScale.x, 
            MazeBlock.BlockAnchor.y * MazeBlock.BlockScale.y, 
            MazeBlock.BlockAnchor.z * MazeBlock.BlockScale.z
            ) * MazeBlock.BasicBlockSize;

    private Material blockMaterial = null;
    private readonly string MAT_RIM_THICKNESS_NAME = "_RimThickness";
    private readonly string MAT_RIM_ARRAY_LENGTH_NAME = "_RimArrayLength";
    private readonly string MAT_RIM_POSITION_ARRAY_NAME = "_RimPosArray";
    private readonly string MAT_RIM_RADIUS_ARRAY_NAME = "_RimRadiusArray";
    //private readonly string MAT_RIM_THICKNESS_ARRAY_NAME = "_RimThicknessArray";
    private readonly string MAT_RIM_ALPHA_ARRAY_NAME = "_RimAlphaArray";

    private List<Vector4> rimPosList = new List<Vector4>();
    private List<float> rimRadiusList = new List<float>();
    //private List<float> rimThicknessList = new List<float>();
    private List<float> rimAlphaList = new List<float>();

    private static readonly int LIST_MAX_LENGTH = 256;
    private Vector4[] rimPosArray = new Vector4[LIST_MAX_LENGTH];
    private float[] rimRadiusArray = new float[LIST_MAX_LENGTH];
    //private float[] rimThicknessArray = new float[LIST_MAX_LENGTH];
    private float[] rimAlphaArray = new float[LIST_MAX_LENGTH];

    private readonly float rimRadiusThatSpreadOneSecond = MazeBlock.BasicBlockSize * 10;



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
        rimRadiusList = SoundManager.Instance.GetSoudObjectRadiusList(rimRadiusThatSpreadOneSecond);
        rimAlphaList = SoundManager.Instance.GetSoundObjectAlphaList();
        foreach(var t in rimAlphaList) {
            Debug.Log(t);
        }

        Array.Copy(rimRadiusList.ToArray(), 0, rimRadiusArray, 0, rimRadiusList.Count);
        Array.Copy(rimAlphaList.ToArray(), 0, rimAlphaArray, 0, rimAlphaList.Count);

        blockMaterial.SetFloatArray(MAT_RIM_RADIUS_ARRAY_NAME, rimRadiusArray);
        blockMaterial.SetFloatArray(MAT_RIM_ALPHA_ARRAY_NAME, rimAlphaArray);
    }

    #region Utility
    public void LoadLevel(int width, int height, bool createEmpty = false) {
        if(createEmpty) MazeCreator.CreateEmptyMaze(width, height);
        else MazeCreator.CreateMaze(width, height);

        string componentName = typeof(MazeBlock).Name;
        GameObject resourceObj = ResourceLoader.GetResource<GameObject>(componentName);

        if(blockMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/MazeBlock"));
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BasicBlockSize * 1.0f);

            blockMaterial = mat;
        }

        mazeBlcoks = new MazeBlock[width, height];
        for(int x = 0; x < width; x++) {
            for(int z = 0; z < height; z++) {
                GameObject go = Instantiate(resourceObj, transform);
                go.name = componentName;

                go.transform.position = GetBlockPos(x, z);
                go.transform.localScale = MazeBlock.BlockScale;

                MazeBlock mb = go.GetComponent<MazeBlock>();
                mb.WallInfo = MazeCreator.Maze[x, z];
                mb.SetMaterial(blockMaterial);
            }
        }

        LevelWidth = width;
        LevelHeight = height;
    }

    public Vector3 GetBlockPos(float x, float y) {
        return new Vector3(basicBlockPos.x * x, basicBlockPos.y, basicBlockPos.z * y) + blockPosOffset;
    }

    public Vector3 GetCenterPos() {
        return GetBlockPos((LevelWidth - 1) * 0.5f, (LevelHeight - 1) * 0.5f);
    }
    #endregion

    #region Action
    private void OnWorldSoundAdded() {
        rimPosList = SoundManager.Instance.GetSoundObjectPosList();
        blockMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, rimPosList.Count);

        Array.Copy(rimPosList.ToArray(), 0, rimPosArray, 0, rimPosList.Count);
        blockMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
    }

    private void OnWorldSoundRemoved() {
        rimPosList = SoundManager.Instance.GetSoundObjectPosList();
        blockMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, rimPosList.Count);

        Array.Copy(rimPosList.ToArray(), 0, rimPosArray, 0, rimPosList.Count);
        blockMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
    }
    #endregion
}
