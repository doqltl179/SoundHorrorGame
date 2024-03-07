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

            if(value) {
                posSaver = transform.position;
            }
            else {
                rigidbody.velocity = calculatedVelocity * rigidbody.mass * 10.0f;
            }

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

    public static readonly float PickUpDistance = PlayerController.PlayerHeight * 1.5f;

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

    private void Update() {
        calculatedVelocity = transform.position - posSaver;

        posSaver = transform.position;
    }

    private void OnCollisionEnter(Collision collision) {
        if(collision.collider.CompareTag(MazeBlock.TagName)) {
            // Play Sound
            SoundManager.Instance.PlayOnWorld(collision.contacts[0].point, SoundManager.SoundType.Empty03s, SoundManager.SoundFrom.None);
        }
    }

    #region Utility


    public void SetMaterial(Material mat) {
        meshRenderer.material = mat;
    }
    #endregion
}
