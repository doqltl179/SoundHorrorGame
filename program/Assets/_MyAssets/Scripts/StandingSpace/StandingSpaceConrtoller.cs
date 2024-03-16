using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandingSpaceConrtoller : MonoBehaviour {
    [SerializeField] private Transform npcAnchor;
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform npcCameraView;

    [SerializeField] private Animator npcAnimator;

    public Vector3 PlayerPos { get { return playerAnchor.position; } }
    public Quaternion PlayerRotation { get { return playerAnchor.rotation; } }

    public bool NPCActive {
        get => npcAnchor.gameObject.activeSelf;
        set => npcAnchor.gameObject.SetActive(value);
    }
    public bool NPCModelActive {
        get => npcAnimator.gameObject.activeSelf;
        set => npcAnimator.gameObject.SetActive(value);
    }
    public Vector3 NPCPos { get { return npcAnchor.position; } }
    public Quaternion NPCRotation { get { return npcAnchor.rotation; } }
    public Vector3 NPCForward { get { return npcAnchor.forward; } }
    public Vector3 NPCCameraViewPos { get { return npcCameraView.position; } }
    public Quaternion NPCCameraViewRotation { get { return npcCameraView.rotation; } }
    public Vector3 NPCCameraViewForward { get { return npcCameraView.forward; } }
    public Vector3 NPCCameraViewUp { get { return npcCameraView.up; } }

    private const string AnimatorTrigger_NPC_Surprised = "Surprised";
    private const string AnimatorTrigger_NPC_Talking = "Talking";
    private const string AnimatorTrigger_NPC_Throw = "Throw";
    private const string AnimatorTrigger_NPC_PlayerCatch = "PlayerCatch";

    private MazeBlock[,] levels = null;
    private Material levelFloorMaterial = null;
    private Material levelWallMaterial = null;
    private PhysicMaterial levelPhysicMaterial = null;

    public MazeBlock BlockR => levels[2, 1];
    public MazeBlock BlockRT => levels[2, 2];
    public MazeBlock BlockT => levels[1, 2];
    public MazeBlock BlockLT => levels[0, 2];
    public MazeBlock BlockL => levels[0, 1];
    public MazeBlock BlockLB => levels[0, 0];
    public MazeBlock BlockB => levels[1, 0];
    public MazeBlock BlockRB => levels[2, 0];
    public MazeBlock BlockCenter => levels[1, 1];

    private const string MAT_BASE_COLOR_NAME = "_BaseColor";
    private const string MAT_MAZEBLOCK_EDGE_COLOR_NAME = "_MazeBlockEdgeColor";
    private const string MAT_MAZEBLOCK_EDGE_THICKNESS_NAME = "_MazeBlockEdgeThickness";
    private const string MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME = "_MazeBlockEdgeShowDistance";

    private const string MAT_USE_BASE_COLOR_KEY = "USE_BASE_COLOR";
    private const string MAT_DRAW_MAZEBLOCK_EDGE_KEY = "DRAW_MAZEBLOCK_EDGE";

    //private IEnumerator npcMoveAnimationCoroutine = null;
    //private IEnumerator npcRotateAnimationCoroutine = null;



    private void OnDisable() {
        //if(npcMoveAnimationCoroutine != null) {
        //    StopCoroutine(npcMoveAnimationCoroutine);
        //    npcMoveAnimationCoroutine = null;
        //}
        //if(npcRotateAnimationCoroutine != null) {
        //    StopCoroutine(npcRotateAnimationCoroutine);
        //    npcRotateAnimationCoroutine = null;
        //}
        StopAllCoroutines();
    }

    private void Start() {
        string componentName = typeof(MazeBlock).Name;
        GameObject resourceObj = ResourceLoader.GetResource<GameObject>(componentName);

        if(levelFloorMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));
            mat.SetColor(MAT_BASE_COLOR_NAME, Color.white);
            mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);

            levelFloorMaterial = mat;
        }
        if(levelWallMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));
            mat.SetColor(MAT_BASE_COLOR_NAME, Color.white);
            mat.SetColor(MAT_MAZEBLOCK_EDGE_COLOR_NAME, Color.black);
            mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
            mat.EnableKeyword(MAT_DRAW_MAZEBLOCK_EDGE_KEY);

            mat.SetFloat(MAT_MAZEBLOCK_EDGE_THICKNESS_NAME, MazeBlock.BlockSize * 0.0002f);
            mat.SetFloat(MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME, MazeBlock.BlockSize * 10.0f);

            levelWallMaterial = mat;
        }

        if(levelPhysicMaterial == null) {
            PhysicMaterial pm = new PhysicMaterial();
            pm.dynamicFriction = 0.0f;
            pm.staticFriction = 0.0f;
            pm.frictionCombine = PhysicMaterialCombine.Minimum;

            levelPhysicMaterial = pm;
        }

        if(levels == null) {
            const int width = 3;
            const int height = 3;
            Vector3 levelPosOffset = new Vector3(
                MazeBlock.BlockSize * width * -0.5f,
                0.0f,
                MazeBlock.BlockSize * height * -1.0f);
            levels = new MazeBlock[width, height];
            for(int x = 0; x < width; x++) {
                for(int y = 0; y < height; y++) {
                    GameObject go = Instantiate(resourceObj);
                    go.name = componentName;

                    go.transform.SetParent(transform);
                    go.transform.localScale = MazeBlock.StandardBlockScale;
                    go.transform.localPosition = new Vector3(
                        (x + MazeBlock.StandardBlockAnchor.x) * MazeBlock.BlockSize,
                        0,
                        (y + MazeBlock.StandardBlockAnchor.z) * MazeBlock.BlockSize) + levelPosOffset;

                    MazeBlock mb = go.GetComponent<MazeBlock>();
                    mb.SetMaterial(levelFloorMaterial, levelWallMaterial);
                    mb.SetPhysicMaterial(levelPhysicMaterial);

                    levels[x, y] = mb;
                }
            }

            levels[0, 0].WallInfo = MazeCreator.ActiveWall.L | MazeCreator.ActiveWall.B;
            levels[0, 1].WallInfo = MazeCreator.ActiveWall.L;
            levels[0, 2].WallInfo = MazeCreator.ActiveWall.L | MazeCreator.ActiveWall.F;
            levels[1, 0].WallInfo = MazeCreator.ActiveWall.B;
            levels[1, 1].WallInfo = MazeCreator.ActiveWall.None;
            levels[1, 2].WallInfo = MazeCreator.ActiveWall.F;
            levels[2, 0].WallInfo = MazeCreator.ActiveWall.R | MazeCreator.ActiveWall.B;
            levels[2, 1].WallInfo = MazeCreator.ActiveWall.R;
            levels[2, 2].WallInfo = MazeCreator.ActiveWall.R | MazeCreator.ActiveWall.F;
        }

        npcAnchor.localPosition = levels[0, 2].transform.localPosition;
        npcAnchor.forward = new Vector3(Mathf.Cos(Mathf.PI * -0.25f), 0.0f, Mathf.Sin(Mathf.PI * -0.25f));

        playerAnchor.localPosition = levels[1, 1].transform.localPosition;
        playerAnchor.forward = (npcAnchor.localPosition - playerAnchor.localPosition).normalized;
    }

    #region Utility
    public void SetColor(Color color, float time = 0.0f) {
        if(time > 0) {
            StartCoroutine(SetColorCoroutine(color, time));
        }
        else {
            levelWallMaterial.SetColor(MAT_BASE_COLOR_NAME, color);
            levelFloorMaterial.SetColor(MAT_BASE_COLOR_NAME, color);
        }
    }

    private IEnumerator SetColorCoroutine(Color color, float time) {
        Color startColor = levelWallMaterial.GetColor(MAT_BASE_COLOR_NAME);

        float timeChecker = 0.0f;
        Color lerpColor;
        while(timeChecker < time) {
            timeChecker += Time.deltaTime;
            lerpColor = Color.Lerp(startColor, color, timeChecker / time);
            levelWallMaterial.SetColor(MAT_BASE_COLOR_NAME, lerpColor);
            levelFloorMaterial.SetColor(MAT_BASE_COLOR_NAME, lerpColor);

            yield return null;
        }
    }

    public void StartNPCRotateAnimation(float animationTime, Quaternion rotateTo, float delay = 0.0f) {
        //if(npcRotateAnimationCoroutine == null) {
        //    npcRotateAnimationCoroutine = NPCMoveAnimationCoroutine(animationTime, rotateTo);
        //    StartCoroutine(npcRotateAnimationCoroutine);
        //}
        StartCoroutine(NPCMoveAnimationCoroutine(animationTime, rotateTo, delay));
    }

    private IEnumerator NPCMoveAnimationCoroutine(float animationTime, Quaternion rotateTo, float delay = 0.0f) {
        if(delay > 0.0f) {
            yield return new WaitForSeconds(delay);
        }

        Quaternion npcStartRotation = npcAnchor.rotation;
        float timeChecker = 0.0f;
        while(timeChecker < animationTime) {
            timeChecker += Time.deltaTime;
            npcAnchor.rotation = Quaternion.Lerp(npcStartRotation, rotateTo, timeChecker / animationTime);

            yield return null;
        }

        npcAnchor.rotation = rotateTo;

        //npcMoveAnimationCoroutine = null;
    }

    public void StartNPCMoveAnimation(float animationTime, Vector3 moveTo, float delay = 0.0f) {
        //if(npcMoveAnimationCoroutine == null) {
        //    npcMoveAnimationCoroutine = NPCMoveAnimationCoroutine(animationTime, moveTo);
        //    StartCoroutine(npcMoveAnimationCoroutine);
        //}
        StartCoroutine(NPCMoveAnimationCoroutine(animationTime, moveTo, delay));
    }

    private IEnumerator NPCMoveAnimationCoroutine(float animationTime, Vector3 moveTo, float delay = 0.0f) {
        if(delay > 0.0f) {
            yield return new WaitForSeconds(delay);
        }

        Vector3 npcStartPos = npcAnchor.position;
        float timeChecker = 0.0f;
        while(timeChecker < animationTime) {
            timeChecker += Time.deltaTime;
            npcAnchor.position = Vector3.Lerp(npcStartPos, moveTo, timeChecker / animationTime);

            yield return null;
        }

        npcAnchor.position = moveTo;

        //npcMoveAnimationCoroutine = null;
    }

    public void SetAnimationTrigger_Talking() => npcAnimator.SetTrigger(AnimatorTrigger_NPC_Talking);
    public void SetAnimationResetTrigger_Talking() => npcAnimator.ResetTrigger(AnimatorTrigger_NPC_Talking);
    public void SetAnimationTrigger_Surprised() => npcAnimator.SetTrigger(AnimatorTrigger_NPC_Surprised);
    public void SetAnimationResetTrigger_Surprised() => npcAnimator.ResetTrigger(AnimatorTrigger_NPC_Surprised);
    public void SetAnimationTrigger_Throw() => npcAnimator.SetTrigger(AnimatorTrigger_NPC_Throw);
    public void SetAnimationResetTrigger_Throw() => npcAnimator.ResetTrigger(AnimatorTrigger_NPC_Throw);
    public void SetAnimationTrigger_PlayerCatch() => npcAnimator.SetTrigger(AnimatorTrigger_NPC_PlayerCatch);
    public void SetAnimationResetTrigger_PlayerCatch() => npcAnimator.ResetTrigger(AnimatorTrigger_NPC_PlayerCatch);

    public void InitializeNPCAnchor(Vector3 pos, Vector3? forward = null) {
        npcAnchor.position = pos;
        if(forward != null) npcAnchor.forward = forward.Value;
    }
    #endregion
}
