using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupItem : MonoBehaviour {
    [SerializeField] protected Rigidbody rigidbody;
                     
    [SerializeField] protected GameObject guideAnchor;

    [SerializeField] protected Vector3 picupAngleOffset = Vector3.zero;
    public Vector3 PicupAngleOffset { get { return picupAngleOffset; } }

    [SerializeField] private bool autoPickup = false;
    public bool AutoPickup { get { return autoPickup; } }

    public bool IsPlaying { get; protected set; }

    protected bool isPickup = false;
    public virtual bool IsPickup {
        get => isPickup;
        set {
            rigidbody.useGravity = !value;

            isPickup = value;
        }
    }

    protected Material material = null;
    public virtual Material Material {
        get {
            //if(material == null) {
            //    Material mat = new Material(Shader.Find("MyCustomShader/Maze"));

            //    mat.SetFloat(MAT_RIM_THICKNESS_NAME, MazeBlock.BlockSize * 0.2f);
            //    mat.SetFloat(MAT_RIM_THICKNESS_OFFSET_NAME, 1.0f);
            //    mat.SetColor("_BaseColor", Color.black);

            //    mat.SetFloat(MAT_OBJECT_OUTLINE_THICKNESS_NAME, 0.1f);
            //    mat.SetColor(MAT_OBJECT_OUTLINE_COLOR_NAME, new Color(1.0f, 1.0f, 0.1f));

            //    mat.EnableKeyword(MAT_USE_BASE_COLOR_KEY);
            //    mat.EnableKeyword(MAT_DRAW_MAZEBLOCK_EDGE_KEY);

            //    mat.SetFloat(MAT_MAZEBLOCK_EDGE_THICKNESS_NAME, 0.1f);
            //    mat.SetFloat(MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME, MazeBlock.BlockSize * 1.5f);

            //    meshRenderer.material = mat;
            //    material = mat;
            //}

            return material;
        }
    }

    protected bool objectOutlineActive;
    public virtual bool ObjectOutlineActive {
        get => objectOutlineActive;
        set {
        //    if(value) {
        //        Material.EnableKeyword(MAT_DRAW_OBJECT_OUTLINE_KEY);
        //    }
        //    else {
        //        Material.DisableKeyword(MAT_DRAW_OBJECT_OUTLINE_KEY);
        //    }

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
    public Quaternion Rotation {
        get => transform.rotation;
        set => transform.rotation = value;
    }

    public static readonly float PickupDistance = PlayerController.PlayerHeight * 2.0f;

    public const string TagName = "PickupItem";
    public const string LayerName = "PickupItem";

    protected const string MAT_RIM_THICKNESS_NAME = "_RimThickness";
    protected const string MAT_RIM_THICKNESS_OFFSET_NAME = "_RimThicknessOffset";
    protected const string MAT_MAZEBLOCK_EDGE_THICKNESS_NAME = "_MazeBlockEdgeThickness";
    protected const string MAT_MAZEBLOCK_EDGE_SHOW_DISTANCE_NAME = "_MazeBlockEdgeShowDistance";
    protected const string MAT_OBJECT_OUTLINE_THICKNESS_NAME = "_ObjectOutlineThickness";
    protected const string MAT_OBJECT_OUTLINE_COLOR_NAME = "_ObjectOutlineColor";
    
    protected const string MAT_USE_BASE_COLOR_KEY = "USE_BASE_COLOR";
    protected const string MAT_DRAW_MAZEBLOCK_EDGE_KEY = "DRAW_MAZEBLOCK_EDGE";
    protected const string MAT_DRAW_OBJECT_OUTLINE_KEY = "DRAW_OBJECT_OUTLINE";



    protected virtual void Start() {
        Transform[] child = GetComponentsInChildren<Transform>();
        foreach(Transform t in child) {
            t.tag = TagName;
            t.gameObject.layer = LayerMask.NameToLayer(LayerName);
        }

        ObjectOutlineActive = false;
    }

    private void Update() {
        if(!IsPlaying) return;

        if(isPickup) {
            if(guideAnchor != null && guideAnchor.activeSelf) guideAnchor.SetActive(false);
        }
        else {
            if(Vector3.Distance(UtilObjects.Instance.CamPos, Pos) < PickupDistance) {
                if(guideAnchor != null) {
                    if(!guideAnchor.activeSelf) guideAnchor.SetActive(true);

                    guideAnchor.transform.position = Pos + Vector3.up * 0.45f;
                    guideAnchor.transform.rotation = Quaternion.LookRotation((UtilObjects.Instance.CamPos - guideAnchor.transform.position).normalized);
                }
            }
            else {
                if(guideAnchor != null) {
                    if(guideAnchor.activeSelf) guideAnchor.SetActive(false);
                }
            }
        }
    }

    #region Utility
    public virtual void Play() {
        IsPlaying = true;
    }

    public virtual void Stop() {
        IsPlaying = false;
    }
    #endregion
}
