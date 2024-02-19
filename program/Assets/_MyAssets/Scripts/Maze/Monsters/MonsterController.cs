using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static SoundManager;

public class MonsterController : MonoBehaviour {
    public static readonly string TagName = "Monster";
    public static readonly string LayerName = "Monster";

    [Header("Components")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rigidbody rigidbody;
    [SerializeField] protected CapsuleCollider collider;
    [SerializeField] protected Material material;

    [Header("Properties")]
    [SerializeField] private Transform headPos;
    [SerializeField, Range(0.1f, 5.0f)] protected float scaleScalar = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] protected float moveSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] protected float moveAnimationSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] protected float moveBoost = 0.4f;
    [SerializeField, Range(0.1f, 10.0f)] protected float rotateSpeed = 1.0f;
    [SerializeField, Range(-0.5f, 0.5f)] protected float moveSoundOffset = 0.0f;

    public Material Material { get { return material; } }

    public float Radius { get { return collider.radius * scaleScalar; } }
    public float Height { get { return collider.height * scaleScalar; } }
    public Vector3 HeadPos { get { return headPos.position; } }
    public Vector3 HeadForward { get { return headPos.forward; } }

    protected const string AnimatorLayerName_Motion = "Motion";
    protected const string AnimatorPropertyName_MoveBlend = "MoveBlend";
    protected const string AnimatorPropertyName_MoveSpeed = "MoveSpeed";
    
    protected float physicsMoveSpeed = 0.0f;
    /// <summary>
    /// FollowingSound != null ? 1.0f : 0.5f
    /// </summary>
    protected float physicsMoveSpeedMax = 0.5f;
    public SoundObject FollowingSound { get; protected set; }

    protected Dictionary<string, AnimatorStateHelper> animatorStateInfo = new Dictionary<string, AnimatorStateHelper>();

    protected List<Vector3> movePath = null;
    protected StuckHelper stuckHelper = null;



    protected virtual void Awake() {
        // Tag 설정
        gameObject.tag = TagName;

        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer(LayerName);
    }

    #region Utility
    public void StartMove() {
        if(movePath == null || movePath.Count <= 0) {
            movePath = LevelLoader.Instance.GetRandomPointPath(transform.position, Radius);
        }
    }
    #endregion

    #region Action
    protected virtual void OnWorldSoundAdded(SoundObject so) {

    }

    protected virtual void OnWorldSoundRemoved() {

    }
    #endregion

    /// <summary>
    /// 얻은 값은 animatorStateInfo에 저장
    /// </summary>
    protected bool TryGetAnimatorStateInfo(string layerName) {
        int layerIndex = animator.GetLayerIndex(layerName);
        if(layerIndex >= 0) {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
            AnimatorStateHelper temp;
            if(animatorStateInfo.TryGetValue(layerName, out temp)) {
                animatorStateInfo[layerName].Info = info;
            }
            else {
                AnimatorStateHelper helper = new AnimatorStateHelper();
                helper.Info = info;

                animatorStateInfo.Add(layerName, helper);
            }

            return true;
        }
        else {
            Debug.Log($"Layer not exist. layerName: {layerName}");

            return false;
        }
    }

    protected class StuckHelper {
        public float RayRadius { get; private set; }
        public int Mask { get; private set; }

        private RaycastHit hit;

        public bool IsHit { get; private set; }
        public Vector3 HitNormal { get { return hit.normal; } }
        public Vector3 HitPos { get { return hit.point; } }
        public float HitDistance { get { return hit.distance; } }



        public StuckHelper(float rayRadius, int mask) {
            RayRadius = rayRadius;
            Mask = mask;
        }

        #region Utility
        public void Raycast(Vector3 rayPos, Vector3 direction, float distance) {
            Vector3 p1 = rayPos;
            Vector3 p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //임의로 player의 높이를 적용
            IsHit = Physics.CapsuleCast(p1, p2, RayRadius, direction, out hit, distance, Mask);
        }
        #endregion
    }

    protected class AnimatorStateHelper {
        public AnimatorStateInfo Info;
        public int CompareInteger;
    }
}
