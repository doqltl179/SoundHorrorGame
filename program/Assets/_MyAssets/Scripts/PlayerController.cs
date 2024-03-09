using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {
    private static PlayerController instance = null;
    public static PlayerController Instance {
        get {
            if(instance == null) {
                PlayerController[] insts = FindObjectsOfType<PlayerController>();
                if(insts.Length > 0) {
                    instance = insts[0];
                    for(int i = 1; i < insts.Length; i++) {
                        Destroy(insts[i].gameObject);
                    }
                }
                else {
                    Debug.LogError($"Component not found. name: {typeof(PlayerController).Name}");
                }
            }

            return instance;
        }
    }

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
    public Vector3 CamPos { get { return cameraAnchor.position; } }
    public Vector3 CamForward { get { return cameraAnchor.forward; } }
    public Quaternion CamRotation { get { return cameraAnchor.rotation; } }

    [Header("Properties")]
    [SerializeField, Range(0.1f, 10.0f)] private float moveSpeed = 2.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float runSpeed = 3.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float crouchSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float walkSoundInterval = 0.5f;
    private float walkSoundIntervalChecker = 0.0f;
    [SerializeField, Range(1.0f, 20.0f)] private float runTimeMax = 10.0f;
    private float runTimeChecker = 0.0f;
    public float NormalizedRunTime { get { return runTimeChecker / runTimeMax; } }
    /// <summary>
    /// runTimeChecker가 runTimeMax보다 높아지면 runTimeMax의 값만큼 run상태가 될 수 없음.
    /// </summary>
    public bool OverHit { get; private set; }
    [SerializeField, Range(0.0f, 10.0f)] private float moveBoost = 0.4f;
    private float physicsMoveSpeed = 0.0f;
    private float physicsMoveSpeedMax = 1.0f;

    [SerializeField, Range(0.0f, 90.0f)] private float cameraVerticalAngleLimit = 75.0f;
    private float cameraVerticalAngleLimitChecker = 0.0f;

    [HideInInspector] public bool IsPlaying = false;

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

    public Vector3 HeadPos { get { return transform.position + Vector3.up * PlayerHeight; } }

    public Vector3 HeadForward { get { return transform.forward; } }

    [SerializeField] private Transform pickUpHandAnchor;
    public HandlingCube PickUpCube { get; private set; } = null;
    private HandlingCube pickUpCubeChecker = null;
    private RaycastHit pickUpHit;
    private int pickUpRayMask;
    private bool pickUpMouseDown;

    [Flags]
    public enum PlayerState {
        None = 0, 
        Walk = 1 << 0,
        Run = 1 << 1,
        Crouch = 1 << 2, 
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

    public static KeyCode key_moveF = KeyCode.W;
    public static KeyCode key_moveB = KeyCode.S;
    public static KeyCode key_moveR = KeyCode.D;
    public static KeyCode key_moveL = KeyCode.A;
    public static KeyCode key_run = KeyCode.LeftShift;
    public static KeyCode key_crouch = KeyCode.LeftControl;

    public Action<Vector2Int> OnCoordChanged;
    public Action<MonsterController> OnPlayerCatched;

    public Action OnEnteredNPCArea;



    private void Awake() {
        // Tag 설정
        gameObject.tag = TagName;

        // Layer 설정
        gameObject.layer = LayerMask.NameToLayer(LayerName);
    }

    private void OnDestroy() {
        OnCoordChanged = null;

        OnEnteredNPCArea = null;
    }

    private void Start() {
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

        pickUpRayMask = ~(1 << LayerMask.NameToLayer("Player"));
    }

    private void Update() {
        if(!IsPlaying) return;

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

        if(OverHit) {
            runTimeChecker -= Time.deltaTime;
            if(runTimeChecker < 0) {
                OverHit = false;
                runTimeChecker = 0.0f;
            }
        }

        CurrentState = PlayerState.None;
        if(Input.GetKey(key_crouch)) CurrentState |= PlayerState.Crouch;
        if(Vector3.Magnitude(moveDirection) > 0) CurrentState |= PlayerState.Walk;
        if(!CurrentState.HasFlag(PlayerState.Crouch) && CurrentState.HasFlag(PlayerState.Walk) && Input.GetKey(key_run)) {
            if(!OverHit) {
                runTimeChecker += Time.deltaTime;
                if(runTimeChecker >= runTimeMax) {
                    OverHit = true;
                    runTimeChecker = runTimeMax;
                }

                CurrentState |= PlayerState.Run;
            }
        }
        else {
            if(!OverHit) {
                runTimeChecker -= Time.deltaTime;
                if(runTimeChecker < 0) {
                    runTimeChecker = 0.0f;
                }
            }
        }

        float speed = 0.0f;
        if(CurrentState != PlayerState.None) {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed + Time.deltaTime * moveBoost, 0.0f, physicsMoveSpeedMax);

            if(CurrentState.HasFlag(PlayerState.Crouch)) speed = crouchSpeed * physicsMoveSpeed;
            else if(CurrentState.HasFlag(PlayerState.Run)) speed = runSpeed * physicsMoveSpeed;
            else if(CurrentState.HasFlag(PlayerState.Walk)) speed = moveSpeed * physicsMoveSpeed;
        }
        else {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed - Time.deltaTime * moveBoost, 0.0f, physicsMoveSpeedMax);
        }
        //transform.Translate(moveDirection * Time.deltaTime * speed, Space.Self);
        rigidbody.velocity = transform.TransformDirection(moveDirection) * speed;
        #endregion

        rigidbody.angularVelocity = Vector3.zero;

        #region Camera
        float anchorHeight = CurrentState.HasFlag(PlayerState.Crouch) ? PlayerHeight * 0.5f : PlayerHeight;
        cameraAnchor.localPosition = Vector3.up * Mathf.Lerp(cameraAnchor.localPosition.y, anchorHeight, Time.deltaTime * Mathf.Pow(2, 4));

        UtilObjects.Instance.CamPos = cameraAnchor.position;
        UtilObjects.Instance.CamForward = cameraAnchor.forward;
        #endregion

        #region Sound
        if(CurrentState.HasFlag(PlayerState.Crouch)) { }
        else if(CurrentState.HasFlag(PlayerState.Run)) walkSoundIntervalChecker += Time.deltaTime * physicsMoveSpeed * (runSpeed / moveSpeed);
        else if(CurrentState.HasFlag(PlayerState.Walk)) walkSoundIntervalChecker += Time.deltaTime * physicsMoveSpeed;

        if(walkSoundIntervalChecker > walkSoundInterval) {
            SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.PlayerWalk, SoundManager.SoundFrom.Player);
            LevelLoader.Instance.AddPlayerPosInMaterialProperty(Pos);

            walkSoundIntervalChecker -= walkSoundInterval;
        }
        #endregion

        CurrentCoord = LevelLoader.Instance.GetMazeCoordinate(Pos);

        #region PickUp
        bool currentMouseDown = Input.GetMouseButton(0);
        if(PickUpCube == null) {
            if(Physics.Raycast(UtilObjects.Instance.CamPos, UtilObjects.Instance.CamForward, out pickUpHit, HandlingCube.PickUpDistance, pickUpRayMask)) {
                if(pickUpCubeChecker != null) 
                    pickUpCubeChecker.ObjectOutlineActive = false;

                pickUpCubeChecker = pickUpHit.rigidbody?.GetComponent<HandlingCube>();
                if(pickUpCubeChecker != null) {
                    pickUpCubeChecker.ObjectOutlineActive = true;

                    if(!pickUpMouseDown && currentMouseDown) {
                        pickUpCubeChecker.ObjectOutlineActive = false;
                        PickUpCube = pickUpCubeChecker;

                        PickUpCube.IsPickUp = true;
                        pickUpCubeChecker = null;
                    }
                }
            }
            else {
                if(pickUpCubeChecker != null) {
                    pickUpCubeChecker.ObjectOutlineActive = false;
                    pickUpCubeChecker = null;
                }
            }
        }
        else {
            if(pickUpMouseDown && !currentMouseDown) {
                PickUpCube.IsPickUp = false;

                PickUpCube = null;
            }
            else {
                PickUpCube.Pos = Vector3.Lerp(PickUpCube.Pos, pickUpHandAnchor.position, Time.deltaTime * Mathf.Pow(2, 6));
                PickUpCube.Forward = Vector3.Lerp(PickUpCube.Forward, pickUpHandAnchor.forward, Time.deltaTime * Mathf.Pow(2, 6));
            }
        }
        pickUpMouseDown = currentMouseDown;
        #endregion
    }

    private void OnTriggerEnter(Collider other) {
        if(!IsPlaying) return;

        if(other.CompareTag("NPC")) {
            OnEnteredNPCArea?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if(!IsPlaying) return;

        if(collision.gameObject.CompareTag(MonsterController.TagName)) {
            // 자꾸 걸리니까 테스트가 안되므로 잠깐 꺼둠.
            //return; 

            OnPlayerCatched?.Invoke(collision.rigidbody.GetComponent<MonsterController>());
        }
    }

    #region Utility
    public void ResetCameraAnchor() {
        cameraVerticalAngleLimitChecker = 0.0f;
        cameraAnchor.localPosition = Vector3.up * PlayerHeight;
        cameraAnchor.localEulerAngles = Vector3.zero;
    }
    #endregion
}
