using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandlingCube : PickupItem {
    [SerializeField] private BoxCollider collider;
    [SerializeField] protected MeshRenderer meshRenderer;

    public override Material Material {
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

    public override bool IsPickup {
        get => isPickup;
        set {
            collider.enabled = !value;
            rigidbody.useGravity = !value;

            if(value) {
                posSaver = transform.position;
            }
            else {
                rigidbody.velocity = calculatedVelocity * rigidbody.mass * 5.0f;
            }

            isPickup = value;
        }
    }

    public override bool ObjectOutlineActive {
        get => objectOutlineActive;
        set {
            if(value) {
                Material.EnableKeyword(MAT_DRAW_OBJECT_OUTLINE_KEY);

                if(guideAnchor != null) guideAnchor.gameObject.SetActive(true);
            }
            else {
                Material.DisableKeyword(MAT_DRAW_OBJECT_OUTLINE_KEY);

                if(guideAnchor != null) guideAnchor.gameObject.SetActive(false);
            }

            objectOutlineActive = value;
        }
    }

    private Vector3 posSaver;
    private Vector3 calculatedVelocity;



    private void Update() {
        if(guideAnchor != null && guideAnchor.gameObject.activeSelf) {
            guideAnchor.transform.position = Pos + Vector3.up * 0.45f;
            guideAnchor.transform.rotation = Quaternion.LookRotation((UtilObjects.Instance.CamPos - guideAnchor.transform.position).normalized);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if(!IsPlaying) return;

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
    public override void Play() {
        IsPlaying = true;
    }

    public override void Stop() {
        IsPlaying = false;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }
    #endregion
}
