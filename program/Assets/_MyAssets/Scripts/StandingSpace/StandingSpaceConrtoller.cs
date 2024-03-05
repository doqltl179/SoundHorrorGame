using System.Collections;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

public class StandingSpaceConrtoller : MonoBehaviour {
    [SerializeField] private Transform npcAnchor;
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform npcCameraView;

    public Vector3 PlayerPos { get { return playerAnchor.position; } }
    public Quaternion PlayerRotation { get { return playerAnchor.rotation; } }

    public Vector3 NPCCameraViewPos { get { return npcCameraView.position; } }
    public Quaternion NPCCameraViewRotation { get { return npcCameraView.rotation; } }

    private const string AnimatorTrigger_NPC_Surprised = "Surprised";
    private const string AnimatorTrigger_NPC_Talking = "Talking";

    private MazeBlock[,] levels = null;
    private Material levelMaterial = null;
    private PhysicMaterial levelPhysicMaterial = null;

    private const string MAT_BASE_COLOR_NAME = "_BaseColor";

    private const string MAT_USE_BASE_COLOR_KEY = "USE_BASE_COLOR";



    private void Start() {
        string componentName = typeof(MazeBlock).Name;
        GameObject resourceObj = ResourceLoader.GetResource<GameObject>(componentName);

        if(levelMaterial == null) {
            Material mat = new Material(Shader.Find("MyCustomShader/Maze"));
            mat.SetColor(MAT_BASE_COLOR_NAME, Color.white);
            mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);

            levelMaterial = mat;
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
                    mb.SetMaterial(levelMaterial, levelMaterial);
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

        playerAnchor.localPosition = levels[1, 0].transform.localPosition;
        playerAnchor.forward = (npcAnchor.localPosition - playerAnchor.localPosition).normalized;
    }
}
