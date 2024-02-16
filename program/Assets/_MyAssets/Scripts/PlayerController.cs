using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : GenericSingleton<PlayerController> {
    public static readonly string TagName = "Player";
    public static readonly string LayerName = "Player";

    public static readonly float PlayerHeight = 1.7f;
    /// <summary>
    /// 플레이어의 두께 설정값
    /// </summary>
    public static readonly float Radius = 0.3f * MazeBlock.BlockSize;

    public Vector3 Pos { get { return transform.position; } }

    private CapsuleCollider collider = null;



    private void Start() {
        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer(LayerName);

        // Collider 설정
        if(collider == null) {
            GameObject go = new GameObject(nameof(CapsuleCollider));
            go.transform.SetParent(transform);

            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.radius = Radius;
            col.height = PlayerHeight;
            col.center = Vector3.up * PlayerHeight * 0.5f;

            collider = col;
        }
    }
}
