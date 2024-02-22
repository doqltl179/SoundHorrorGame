using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour {
    public static readonly string TagName = "Monster";
    public static readonly string LayerName = "Monster";

    public bool IsPlaying { get { return CurrentState != MonsterState.None; } }

    public enum MonsterState {
        None, // ==> IsPlaying to false
        Move, 
        Wait, //IMove가 상속되지 않은 몬스터는 Move 대신 Wait를 설정
        Rest,
        Search, //특정 몬스터만 사용
        Scream, //특정 몬스터만 사용
    }
    private MonsterState currentState = MonsterState.None;
    public MonsterState CurrentState {
        get => currentState;
        set {
            currentState = value;
            OnCurrentStateChanged?.Invoke(value);
        }
    }

    [Header("Components")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rigidbody rigidbody;
    [SerializeField] protected CapsuleCollider collider;
    [SerializeField] protected Material material;

    [Header("Properties")]
    [SerializeField] private Transform headPos;
    [SerializeField, Range(0.1f, 5.0f)] protected float scaleScalar = 1.0f;
    [SerializeField, Range(0.0f, 10.0f)] protected float moveSpeed = 1.0f;
    [SerializeField, Range(0.0f, 10.0f)] protected float moveAnimationSpeed = 1.0f;
    [SerializeField, Range(0.0f, 10.0f)] protected float moveBoost = 0.4f;
    [SerializeField, Range(0.0f, 10.0f)] protected float rotateSpeed = 1.0f;
    [SerializeField, Range(-0.5f, 0.5f)] protected float moveSoundOffset = 0.0f;

    public Material Material { get { return material; } }

    public float Radius { get { return collider.radius * scaleScalar; } }
    public float Height { get { return collider.height * scaleScalar; } }
    public Vector3 HeadPos { get { return headPos.position; } }
    public Vector3 HeadForward { get { return headPos.forward; } }
    public Vector3 Pos { 
        get { 
            return transform.position; 
        } 
        set {
            transform.position = value;
        }
    }

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

    public Action<MonsterState> OnCurrentStateChanged;
    public Action OnPathEnd;



    protected virtual void Awake() {
        // Tag 설정
        gameObject.tag = TagName;

        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer(LayerName);
    }

    #region Virtual
    public virtual void Play() { }

    public virtual void Stop() { }
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

    protected class AnimatorStateHelper {
        public AnimatorStateInfo Info;
        public int CompareInteger;
    }
}
