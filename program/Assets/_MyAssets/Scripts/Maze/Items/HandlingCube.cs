using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandlingCube : MonoBehaviour {
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private BoxCollider collider;
    [SerializeField] private MeshRenderer meshRenderer;

    private bool isPickUp = false;
    public bool IsPickUp {
        get => isPickUp;
        set {
            collider.enabled = !value;
            rigidbody.useGravity = !value;

            isPickUp = value;
        }
    }

    public Vector3 Pos {
        get => transform.position;
        set => transform.position = value;
    }
    public Vector3 Forward {
        get => transform.forward;
        set => transform.forward = value;
    }

    private Vector3 posSaver;
    private Vector3 calculatedVelocity;

    public static string TagName = "HandlingCube";
    public static string LayerName = "HandlingCube";



    private void Start() {
        Transform[] child = GetComponentsInChildren<Transform>();
        foreach(Transform t in child) {
            t.tag = TagName;
            t.gameObject.layer = LayerMask.NameToLayer(LayerName);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if(collision.collider.CompareTag(MazeBlock.TagName)) {
            // Play Sound
            Debug.Log("HandlingCube hit on MazeBlock!");
        }
    }

    #region Utility


    public void SetMaterial(Material mat) {
        meshRenderer.material = mat;
    }
    #endregion
}
