using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeBlock : MonoBehaviour {
    [SerializeField] private GameObject floor = null;
    [SerializeField] private GameObject ceiling = null;
    [SerializeField] private GameObject[] edges = null;
    [SerializeField] private GameObject[] walls = null;

    private MazeCreator.ActiveWall wallInfo;
    public MazeCreator.ActiveWall WallInfo {
        get { 
            return wallInfo; 
        }
        set {
            bool isActiveR = value.HasFlag(MazeCreator.ActiveWall.R);
            bool isActiveF = value.HasFlag(MazeCreator.ActiveWall.F);
            bool isActiveL = value.HasFlag(MazeCreator.ActiveWall.L);
            bool isActiveB = value.HasFlag(MazeCreator.ActiveWall.B);

            walls[0].SetActive(isActiveR);
            walls[1].SetActive(isActiveF);
            walls[2].SetActive(isActiveL);
            walls[3].SetActive(isActiveB);

            edges[0].SetActive(isActiveR || isActiveF);
            edges[1].SetActive(isActiveF || isActiveL);
            edges[2].SetActive(isActiveL || isActiveB);
            edges[3].SetActive(isActiveB || isActiveR);

            wallInfo = value;

            ceiling.SetActive(false);
        }
    }

    public static readonly string TagName = "MazeBlock";
    public static readonly string WallLayerName = "Wall";

    public static readonly Vector3 StandardBlockAnchor = new Vector3(0.5f, 0.0f, 0.5f);

    private static readonly float FloorScale = 3.0f;
    private static readonly float WallHeightScale = 10.0f;
    public static readonly Vector3 StandardBlockScale = new Vector3(FloorScale, WallHeightScale, FloorScale);

    public static readonly float StandardBlockSize = 2.0f;
    /// <summary>
    /// StandardSize * Scale
    /// </summary>
    public static readonly float BlockSize = StandardBlockSize * FloorScale;

    public static readonly float StandardEdgeSize = StandardBlockSize * 0.1f;
    public static readonly float EdgeSize = BlockSize * 0.1f;

    public int WallLayerIndex { get; private set; }



    private void Start() {
        WallLayerIndex = LayerMask.NameToLayer(WallLayerName);

        floor.tag = TagName;
        ceiling.tag = TagName;
        foreach(GameObject edge in edges) {
            edge.tag = TagName;
            edge.layer = WallLayerIndex;
        }
        foreach(GameObject wall in walls) {
            wall.tag = TagName;
            wall.layer = WallLayerIndex;
        }
    }

    #region Utility
#if Use_Two_Materials_On_MazeBlock
    public void SetMaterial(Material mat1, Material mat2) {
#else
    public void SetMaterial(Material mat1) {
#endif

#if Use_Two_Materials_On_MazeBlock
        floor.GetComponent<MeshRenderer>().material = mat1;
        ceiling.GetComponent<MeshRenderer>().material = mat1;
        foreach(GameObject edge in edges) {
            edge.GetComponent<MeshRenderer>().material = mat2;
        }
        foreach(GameObject wall in walls) {
            wall.GetComponent<MeshRenderer>().material = mat2;
        }
#else
        floor.GetComponent<MeshRenderer>().material = mat1;
        ceiling.GetComponent<MeshRenderer>().material = mat1;
        foreach(GameObject edge in edges) {
            edge.GetComponent<MeshRenderer>().material = mat1;
        }
        foreach(GameObject wall in walls) {
            wall.GetComponent<MeshRenderer>().material = mat1;
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="d1">Current moved direction</param>
    /// <param name="d2">Next moved direction</param>
    /// <param name="radius">Collider의 radius</param>
    /// <returns></returns>
    public Vector3 GetCornerPoint(MazeCreator.ActiveWall d1, MazeCreator.ActiveWall d2, float radius) {
        float angle = 0.0f;

        // 왼쪽 위
        if((d1 == MazeCreator.ActiveWall.R && d2 == MazeCreator.ActiveWall.F) ||
            (d1 == MazeCreator.ActiveWall.B && d2 == MazeCreator.ActiveWall.L)) {
            angle = Mathf.PI * 0.75f;
        }
        // 오른쪽 위
        else if((d1 == MazeCreator.ActiveWall.L && d2 == MazeCreator.ActiveWall.F) ||
            (d1 == MazeCreator.ActiveWall.B && d2 == MazeCreator.ActiveWall.R)) {
            angle = Mathf.PI * 0.25f;
        }
        // 왼쪽 아래
        else if((d1 == MazeCreator.ActiveWall.R && d2 == MazeCreator.ActiveWall.B) ||
            (d1 == MazeCreator.ActiveWall.F && d2 == MazeCreator.ActiveWall.L)) {
            angle = Mathf.PI * -0.75f;
        }
        // 오른쪽 아래
        else {
            angle = Mathf.PI * -0.25f;
        }

        // Collider를 벽에 딱 붙게 하지 않게 하기 위해서 1.05를 곱해줌
        float calculatedRadius = (BlockSize - (EdgeSize * 2)) * 0.5f * Mathf.Sqrt(2) - radius * 1.05f;
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
        return transform.position + direction * calculatedRadius;
    }
#endregion
}
