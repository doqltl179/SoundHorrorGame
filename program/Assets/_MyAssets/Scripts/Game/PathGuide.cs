using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGuide : MonoBehaviour {
    [SerializeField] private GameObject guideObject;

    private List<Vector3> path = null;
    public List<Vector3> Path { 
        get => path;
        set => path = value;
    }

    public readonly float Radius = MazeBlock.BlockSize * 0.25f;
    public readonly float MoveSpeed = MazeBlock.BlockSize * 1.5f;



    private void Start() {
        guideObject.transform.localScale = Vector3.up * PlayerController.PlayerHeight;
    }

    private Vector3 moveDirection;
    private float distanceBetweenPathAndGuide;
    private Vector3 currentPathEnd;
    private Vector2Int currentPathEndCoord;
    private void Update() {
        if(path != null && path.Count > 0) {
            currentPathEnd = path[0];
            currentPathEndCoord = LevelLoader.Instance.GetMazeCoordinate(currentPathEnd);
            moveDirection = (currentPathEnd - transform.position).normalized;
            distanceBetweenPathAndGuide = Vector3.Distance(transform.position, currentPathEnd);
            transform.position += moveDirection * MoveSpeed * Time.deltaTime * Mathf.InverseLerp(0, MazeBlock.BlockSize * 0.25f, distanceBetweenPathAndGuide);

            if(currentPathEndCoord.x == PlayerController.Instance.CurrentCoord.x &&
                currentPathEndCoord.y == PlayerController.Instance.CurrentCoord.y) {
                path.RemoveAt(0);
                if(path.Count <= 0) {
                    Debug.Log("Guide End");
                }
            }
        }
    }

    #region Utility
    #endregion
}
