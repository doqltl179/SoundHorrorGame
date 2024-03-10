using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandlingCube : MonoBehaviour {
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private BoxCollider collider;
    [SerializeField] private MeshRenderer meshRenderer;

    [SerializeField] private GameObject guideAnchor;

    private Material material = null;
    public Material Material {
        get {
            if(material == null) {
                Material mat = new Material(Shader.Find("MyCustomShader/Maze"));

                mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.2f);
                mat.SetFloat(MAT_RIM_THICKNESS_OFFSET_NAME, 1.0f);
                mat.SetColor("_BaseColor", Color.black);

                mat.SetFloat(MAT_OBJECT_OUTLINE_THICKNESS_NAME, 0.1f);
                mat.SetColor(MAT_OBJECT_OUTLINE_COLOR_NAME, new Color(1.0f, 1.0f, 0.1f));

                mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
                mat.EnableKeyword(MAT_DRAW_MAZEBLOCK_EDGE_KEY);

                mat.SetFloat(MAT_MAZEBLOCK_EDGE_THICKNESS_NAME, 0.1f);
                mat.SetFloat(MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME, MazeBlock.BlockSize * 1.5f);

                meshRenderer.material = mat;
                material = mat;
            }

            return material;
        }
    }

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
                rigidbody.velocity = calculatedVelocity * rigidbody.mass * 5.0f;
            }

            isPickUp = value;
        }
    }

    private bool objectOutlineActive;
    public bool ObjectOutlineActive {
        get => objectOutlineActive;
        set {
            if(value) {
                Material.EnableKeyword(MAT_DRAW_OBJECT_OUTLINE_KEY);
            }
            else {
                Material.DisableKeyword(MAT_DRAW_OBJECT_OUTLINE_KEY);
            }

            objectOutlineActive = value;
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

    public static readonly float PickUpDistance = PlayerController.PlayerHeight * 2.0f;

    private Vector3 posSaver;
    private Vector3 calculatedVelocity;

    public const string TagName = "HandlingCube";
    public const string LayerName = "HandlingCube";

    private const string MAT_RIM_THICKNESS_NAME = "_RimThickness";
    private const string MAT_RIM_THICKNESS_OFFSET_NAME = "_RimThicknessOffset";
    private const string MAT_MAZEBLOCK_EDGE_THICKNESS_NAME = "_MazeBlockEdgeThickness";
    private const string MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME = "_MazeBlockEdgeShowDistance";
    private const string MAT_OBJECT_OUTLINE_THICKNESS_NAME = "_ObjectOutlineThickness";
    private const string MAT_OBJECT_OUTLINE_COLOR_NAME = "_ObjectOutlineColor";

    private const string MAT_USE_BASE_COLOR_KEY = "USE_BASE_COLOR";
    private const string MAT_DRAW_MAZEBLOCK_EDGE_KEY = "DRAW_MAZEBLOCK_EDGE";
    private const string MAT_DRAW_OBJECT_OUTLINE_KEY = "DRAW_OBJECT_OUTLINE";



    private void Start() {
        Transform[] child = GetComponentsInChildren<Transform>();
        foreach(Transform t in child) {
            t.tag = TagName;
            t.gameObject.layer = LayerMask.NameToLayer(LayerName);
        }

        ObjectOutlineActive = false;
    }

    private void Update() {
        if(isPickUp) {
            if(guideAnchor.activeSelf) guideAnchor.SetActive(false);

            calculatedVelocity = transform.position - posSaver;

            posSaver = transform.position;
        }
        else {
            if(Vector3.Distance(UtilObjects.Instance.CamPos, Pos) < PickUpDistance) {
                if(!guideAnchor.activeSelf) guideAnchor.SetActive(true);

                guideAnchor.transform.position = Pos + Vector3.up * 0.45f;
                guideAnchor.transform.rotation = Quaternion.LookRotation((UtilObjects.Instance.CamPos - guideAnchor.transform.position).normalized);
            }
            else {
                if(guideAnchor.activeSelf) guideAnchor.SetActive(false);
            }
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if(collision.collider.CompareTag(MazeBlock.TagName)) {
            // Play Sound
            float mag = rigidbody.velocity.magnitude * rigidbody.mass;
            float normalizedStrength = Mathf.Clamp01(mag / LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH);
            Debug.Log(rigidbody.velocity.magnitude);
            int sound = (int)(normalizedStrength / 0.2f);
            switch(sound) {
                case 0: SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.ObjectHit01, SoundManager.SoundFrom.Player); break;
                case 1: SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.ObjectHit02, SoundManager.SoundFrom.Player); break;
                case 2: SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.ObjectHit03, SoundManager.SoundFrom.Player); break;
                case 3: SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.ObjectHit04, SoundManager.SoundFrom.Player); break;
                default: SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.ObjectHit05, SoundManager.SoundFrom.Player); break;
            }
        }
    }

    #region Utility


    //public void SetMaterial(Material mat) {
    //    meshRenderer.material = mat;
    //}
    #endregion
}
