using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : GenericSingleton<LevelLoader> {
    private MazeBlock[,] mazeBlcoks = null;
    public int LevelWidth { get; private set; }
    public int LevelHeight { get; private set; }

    private readonly Vector3 basicBlockPos = new Vector3(1.0f, 0.0f, 1.0f) * MazeBlock.BlockSize;
    private readonly Vector3 blockPosOffset = MazeBlock.StandardBlockAnchor * MazeBlock.BlockSize;

    private Material blockMaterial = null;
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
    // SpreadTime 동안 MazeBlock을 10칸 이동하기 위해 10을 곱함
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
            mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.25f);

            blockMaterial = mat;
        }

        mazeBlcoks = new MazeBlock[width, height];
        for(int x = 0; x < width; x++) {
            for(int z = 0; z < height; z++) {
                GameObject go = Instantiate(resourceObj, transform);
                go.name = componentName;

                go.transform.position = GetBlockPos(x, z);
                go.transform.localScale = MazeBlock.StandardBlockScale;

                MazeBlock mb = go.GetComponent<MazeBlock>();
                mb.WallInfo = MazeCreator.Maze[x, z];
                mb.SetMaterial(blockMaterial);
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
    #endregion

    #region Action
    private void OnWorldSoundAdded() {
        Vector4[] currentRimPosList = SoundManager.Instance.GetSoundObjectPosArray();
        blockMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);

        Array.Copy(currentRimPosList, 0, rimPosArray, 0, currentRimPosList.Length);
        blockMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
    }

    private void OnWorldSoundRemoved() {
        Vector4[] currentRimPosList = SoundManager.Instance.GetSoundObjectPosArray();
        blockMaterial.SetInteger(MAT_RIM_ARRAY_LENGTH_NAME, currentRimPosList.Length);

        Array.Copy(currentRimPosList, 0, rimPosArray, 0, currentRimPosList.Length);
        blockMaterial.SetVectorArray(MAT_RIM_POSITION_ARRAY_NAME, rimPosArray);
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
}
