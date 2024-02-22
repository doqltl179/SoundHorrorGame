using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {
    public static readonly string TagName = "Player";
    public static readonly string LayerName = "Player";

    public static readonly float PlayerHeight = 0.8f;
    /// <summary>
    /// 플레이어의 두께 설정값
    /// </summary>
    public static readonly float Radius = 0.3f;

    [Header("Components")]
    [SerializeField] private CapsuleCollider collider;
    [SerializeField] private Rigidbody rigidbody;

    [Header("Properties")]
    [SerializeField, Range(0.1f, 10.0f)] private float moveSpeed = 2.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float runSpeed = 3.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float crouchSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float rotateSpeed = 1.5f;
    [SerializeField, Range(0.1f, 10.0f)] private float walkSoundInterval = 0.5f;
    private float walkSoundIntervalChecker = 0.0f;

    public Vector3 Pos {
        get {
            return transform.position;
        }
        set {
            transform.position = value;
        }
    }

    public Vector3 HeadPos { get { return transform.position + Vector3.up * PlayerHeight; } }

    public Vector3 HeadForward { get { return transform.forward; } }

    public enum PlayerState {
        None, 
        Walk, 
        Run, 
        Crouch, 
    }
    public PlayerState CurrentState { get; private set; }

    private KeyCode key_moveF = KeyCode.W;
    private KeyCode key_moveB = KeyCode.S;
    private KeyCode key_moveR = KeyCode.A;
    private KeyCode key_moveL = KeyCode.D;
    private KeyCode key_dash = KeyCode.LeftShift;
    private KeyCode key_crouch = KeyCode.LeftControl;



    private void Awake() {
        // Tag 설정
        gameObject.tag = TagName;

        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer(LayerName);
    }

#if Play_Game_Automatically
    List<Vector3> movePath = null;
    StuckHelper stuckHelper = null;

    public bool playAutomatically = false;

    private void Start() {
        stuckHelper = new StuckHelper(Radius, 1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));
#else
    private void Start() {
#endif
        // Collider 설정
        if(collider == null) {
            GameObject go = new GameObject(nameof(CapsuleCollider));
            go.transform.SetParent(transform);

            CapsuleCollider col = go.AddComponent<CapsuleCollider>();

            collider = col;
        }
        collider.radius = Radius;
        collider.height = PlayerHeight;
        collider.center = Vector3.up * PlayerHeight * 0.5f;
    }

    private void FixedUpdate() {
#if Play_Game_Automatically
        if(!playAutomatically) {
            return;
        }

        if(movePath == null || movePath.Count <= 0) {
            movePath = LevelLoader.Instance.GetRandomPointPathCompareDistance(
                Pos,
                Radius,
                1 << LayerMask.NameToLayer(MazeBlock.WallLayerName), 
                false,
                LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH * 2);

            CurrentState = PlayerState.Run;
        }

        stuckHelper.Raycast(transform.position, transform.forward, Radius * 1.01f); 
        if(stuckHelper.IsHit) {
            Vector3 hitPosToCurrentPos = (transform.position - stuckHelper.HitPos).normalized;
            bool isRightSide = Vector3.Cross(hitPosToCurrentPos, stuckHelper.HitNormal).y > 0;
            Vector3 moveDirection = Quaternion.AngleAxis(isRightSide ? 90 : -90, Vector3.up) * stuckHelper.HitNormal;
            transform.forward = Vector3.Lerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed);
        }
        else {
            Vector3 moveDirection = (movePath[0] - transform.position).normalized;
            transform.forward = Vector3.Lerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed);
        }

        switch(CurrentState) {
            case PlayerState.None: {

                }
                break;
            case PlayerState.Walk: {
                    transform.position += transform.forward * Time.deltaTime * moveSpeed;
                }
                break;
            case PlayerState.Run: {
                    transform.position += transform.forward * Time.deltaTime * runSpeed;
                }
                break;
            case PlayerState.Crouch: {
                    transform.position += transform.forward * Time.deltaTime * crouchSpeed;
                }
                break;
        }

        if(Vector3.Distance(transform.position, movePath[0]) < Radius) {
            movePath.RemoveAt(0);
        }
#else
        IsDash = false;
        IsCrouch = false;
        Vector3 moveDirection = Vector3.zero;
        foreach(KeyCode key in Enum.GetValues(typeof(KeyCode))) {
            if(Input.GetKey(key_moveF)) moveDirection += Vector3.forward;
            else if(Input.GetKey(key_moveB)) moveDirection += Vector3.back;
            else if(Input.GetKey(key_moveR)) moveDirection += Vector3.right;
            else if(Input.GetKey(key_moveL)) moveDirection += Vector3.left;
            else if(Input.GetKey(key_dash)) IsDash = true;
            else if(Input.GetKey(key_crouch)) IsCrouch = true;
        }
        moveDirection = moveDirection.normalized;

        transform.Translate(moveDirection * Time.deltaTime * moveSpeed, Space.Self);
#endif
    }

    private void LateUpdate() {
        switch(CurrentState) {
            case PlayerState.None: {

                }
                break;
            case PlayerState.Walk: {
                    walkSoundIntervalChecker += Time.deltaTime;
                }
                break;
            case PlayerState.Run: {
                    walkSoundIntervalChecker += Time.deltaTime * (runSpeed / moveSpeed);
                }
                break;
            case PlayerState.Crouch: {
                    walkSoundIntervalChecker += Time.deltaTime * (crouchSpeed / moveSpeed);
                }
                break;
        }
        if(walkSoundIntervalChecker > walkSoundInterval) {
            SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.PlayerWalk, SoundManager.SoundFrom.Player);
            LevelLoader.Instance.AddPlayerPosInMaterialProperty(Pos);

            walkSoundIntervalChecker -= walkSoundInterval;
        }
    }
}
