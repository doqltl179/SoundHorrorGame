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

    public static readonly Vector3 StandardBlockAnchor = new Vector3(0.5f, 0.0f, 0.5f);

    private static readonly float FloorScale = 4.0f;
    private static readonly float WallHeightScale = 3.0f;
    public static readonly Vector3 StandardBlockScale = new Vector3(FloorScale, WallHeightScale, FloorScale);

    public static readonly float StandardBlockSize = 2.0f;
    /// <summary>
    /// StandardSize * Scale
    /// </summary>
    public static readonly float BlockSize = StandardBlockSize * FloorScale;



    private void Start() {
        floor.tag = TagName;
        ceiling.tag = TagName;
        foreach(GameObject edge in edges) {
            edge.tag = TagName;
        }
        foreach(GameObject wall in walls) {
            wall.tag = TagName;
        }
    }

    #region Utility
    public void SetMaterial(Material material) {
        floor.GetComponent<MeshRenderer>().material = material;
        ceiling.GetComponent<MeshRenderer>().material = material;
        foreach(GameObject edge in edges) {
            edge.GetComponent<MeshRenderer>().material = material;
        }
        foreach(GameObject wall in walls) {
            wall.GetComponent<MeshRenderer>().material = material;
        }
    }
    #endregion
}
