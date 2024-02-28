using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : Singleton<PlayerController> {
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

    [Header("GameObject")]
    [SerializeField] private Transform cameraAnchor;

    [Header("Properties")]
    [SerializeField, Range(0.1f, 10.0f)] private float moveSpeed = 2.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float runSpeed = 3.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float crouchSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float rotateSpeed = 1.5f;
    [SerializeField, Range(0.1f, 10.0f)] private float walkSoundInterval = 0.5f;
    private float walkSoundIntervalChecker = 0.0f;
    [SerializeField, Range(0.0f, 10.0f)] protected float moveBoost = 0.4f;
    private float physicsMoveSpeed = 0.0f;
    private float physicsMoveSpeedMax = 1.0f;

    [SerializeField, Range(0.0f, 90.0f)] private float cameraVerticalAngleLimit = 75.0f;
    private float cameraVerticalAngleLimitChecker = 0.0f;

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
    public PlayerState CurrentState { get; private set; } = PlayerState.None;

    private Vector2Int currentCoord;
    public Vector2Int CurrentCoord {
        get => currentCoord;
        private set {
            if(currentCoord.x != value.x || currentCoord.y != value.y) {
                OnCoordChanged?.Invoke(value);

                currentCoord = value;
            }
        }
    }

    private KeyCode key_moveF = KeyCode.W;
    private KeyCode key_moveB = KeyCode.S;
    private KeyCode key_moveR = KeyCode.D;
    private KeyCode key_moveL = KeyCode.A;
    private KeyCode key_run = KeyCode.LeftShift;
    private KeyCode key_crouch = KeyCode.LeftControl;

    public Action<Vector2Int> OnCoordChanged;



    private void Awake() {
        // Tag 설정
        gameObject.tag = TagName;

        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer(LayerName);
    }

#if Play_Game_Automatically
    List<Vector3> movePath = null;
    StuckHelper stuckHelper = null;

    bool autoMove = false;

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

        cameraAnchor.localPosition = Vector3.up * PlayerHeight;
    }

    private void Update() {
#if Play_Game_Automatically
        if(Input.GetKeyDown(KeyCode.Space)) {
            autoMove = !autoMove;
            CurrentState = autoMove ? PlayerState.Run : PlayerState.None;
        }

        if(!autoMove) return;

        if(movePath == null || movePath.Count <= 0) {
            movePath = LevelLoader.Instance.GetRandomPointPathCompareDistance(
                Pos,
                Radius,
                false,
                LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH * 2);

            CurrentState = PlayerState.Run;
            physicsMoveSpeed = 1.0f;
        }

        stuckHelper.Raycast(transform.position, transform.forward, Radius * 1.01f); 
        if(stuckHelper.IsHit) {
            Vector3 hitPosToCurrentPos = (transform.position - stuckHelper.HitPos).normalized;
            bool isRightSide = Vector3.Cross(hitPosToCurrentPos, stuckHelper.HitNormal).y > 0;
            Vector3 moveDirection = Quaternion.AngleAxis(isRightSide ? 90 : -90, Vector3.up) * stuckHelper.HitNormal;
            transform.forward = Vector3.Lerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed * 2);
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
        #region Rotate
        float mouseX = Input.GetAxis("Mouse X") * UserSettings.DisplaySensitive * Time.deltaTime;
        transform.eulerAngles += transform.rotation * (Vector3.up * mouseX);
        
        float mouseY = Input.GetAxis("Mouse Y") * UserSettings.DisplaySensitive * Time.deltaTime;
        cameraVerticalAngleLimitChecker += (-mouseY);
        if(cameraVerticalAngleLimitChecker < -cameraVerticalAngleLimit) cameraVerticalAngleLimitChecker = -cameraVerticalAngleLimit;
        else if(cameraVerticalAngleLimitChecker > cameraVerticalAngleLimit) cameraVerticalAngleLimitChecker = cameraVerticalAngleLimit;
        cameraAnchor.localEulerAngles = Vector3.right * cameraVerticalAngleLimitChecker;
        #endregion

        #region Move
        Vector3 moveDirection = Vector3.zero;
        if(Input.GetKey(key_moveF)) moveDirection += Vector3.forward;
        if(Input.GetKey(key_moveB)) moveDirection += Vector3.back;
        if(Input.GetKey(key_moveR)) moveDirection += Vector3.right;
        if(Input.GetKey(key_moveL)) moveDirection += Vector3.left;
        moveDirection = moveDirection.normalized;

        if(Vector3.Magnitude(moveDirection) <= 0) CurrentState = PlayerState.None;
        else if(Input.GetKey(key_crouch)) CurrentState = PlayerState.Crouch;
        else if(Input.GetKey(key_run)) CurrentState = PlayerState.Run;
        else CurrentState = PlayerState.Walk;

        float speed = 0.0f;
        if(CurrentState != PlayerState.None) {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed + Time.deltaTime * moveBoost, 0.0f, physicsMoveSpeedMax);

            if(CurrentState == PlayerState.Run) speed = runSpeed * physicsMoveSpeed;
            else if(CurrentState == PlayerState.Walk) speed = moveSpeed * physicsMoveSpeed;
            else speed = crouchSpeed * physicsMoveSpeed;
        }
        else {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed - Time.deltaTime * moveBoost, 0.0f, physicsMoveSpeedMax);
        }
        //transform.Translate(moveDirection * Time.deltaTime * speed, Space.Self);
        rigidbody.velocity = transform.TransformDirection(moveDirection) * speed;
        #endregion

        rigidbody.angularVelocity = Vector3.zero;
#endif

        UtilObjects.Instance.CamPos = cameraAnchor.position;
        UtilObjects.Instance.CamForward = cameraAnchor.forward;

        #region Sound
        if(CurrentState == PlayerState.Walk) walkSoundIntervalChecker += Time.deltaTime * physicsMoveSpeed;
        else if(CurrentState == PlayerState.Run) walkSoundIntervalChecker += Time.deltaTime * physicsMoveSpeed * (runSpeed / moveSpeed);

        if(walkSoundIntervalChecker > walkSoundInterval) {
            SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.PlayerWalk, SoundManager.SoundFrom.Player);
            LevelLoader.Instance.AddPlayerPosInMaterialProperty(Pos);

            walkSoundIntervalChecker -= walkSoundInterval;
        }
        #endregion

        CurrentCoord = LevelLoader.Instance.GetMazeCoordinate(Pos);
    }
}
