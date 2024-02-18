using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour {
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private CapsuleCollider collider;
    [SerializeField] private Material material;

    [Header("Properties")]
    [SerializeField] private Transform headPos;
    [SerializeField, Range(0.1f, 5.0f)] private float scaleScalar = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float moveSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float moveAnimationSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float moveBoost = 0.4f;
    [SerializeField, Range(0.1f, 10.0f)] private float rotateSpeed = 1.0f;
    [SerializeField, Range(-0.5f, 0.5f)] private float moveSoundOffset = 0.0f;

    public Material Material { get { return material; } }

    public float Radius { get { return collider.radius * scaleScalar; } }
    public float Height { get { return collider.height * scaleScalar; } }
    public Vector3 HeadPos { get { return headPos.position; } }
    public Vector3 HeadForward { get { return headPos.forward; } }

    private const string AnimatorLayerName_Motion = "Motion";
    private const string AnimatorPropertyName_MoveBlend = "MoveBlend";
    private const string AnimatorPropertyName_MoveSpeed = "MoveSpeed";

    private float physicsMoveSpeed = 0.0f;
    /// <summary>
    /// FollowingSound != null ? 1.0f : 0.5f
    /// </summary>
    private float physicsMoveSpeedMax = 0.5f;
    public SoundObject FollowingSound { get; private set; }

    private Dictionary<string, AnimatorStateHelper> animatorStateInfo = new Dictionary<string, AnimatorStateHelper>();

    private List<Vector3> movePath = null;
    private StuckHelper stuckHelper = null;


    private void OnDrawGizmos() {
        if(movePath == null || movePath.Count < 2)
            return;

        // Gizmos ���� ����
        Gizmos.color = Color.green;

        // points �迭�� �ִ� ��� ��ǥ���� ������� �մ� ���� �׸��ϴ�.
        for(int i = 0; i < movePath.Count - 1; i++) {
            Gizmos.DrawLine(movePath[i], movePath[i + 1]);
        }
    }

    private void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += OnWorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += OnWorldSoundRemoved;
    }

    private void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= OnWorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= OnWorldSoundRemoved;
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;

        stuckHelper = new StuckHelper(Radius, 1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));

        animator.SetFloat(AnimatorPropertyName_MoveSpeed, moveSpeed * moveAnimationSpeed);
    }

    private void FixedUpdate() {
        if(movePath != null && movePath.Count > 0) {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed + Time.deltaTime * moveBoost, 0.0f, physicsMoveSpeedMax);

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

            if(Vector3.Distance(transform.position, movePath[0]) < Radius) {
                movePath.RemoveAt(0);
            }
        }
        else {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed - Time.deltaTime * moveBoost, 0.0f, physicsMoveSpeedMax);
        }

        // ��ġ �̵�
        if(physicsMoveSpeed > 0) {
            transform.position += transform.forward * physicsMoveSpeed * Time.deltaTime * moveSpeed * scaleScalar;
        }

        // �ִϸ��̼��� �ӵ� ����
        // physicsMoveSpeed�� 0�� �������� Idle�� ��ȯ
        animator.SetFloat(AnimatorPropertyName_MoveBlend, physicsMoveSpeed);

        // ��� �ִϸ��̼��� Idle -> Walk -> Run ������ BlendTree�� ������ ����
        // Walk, Run �ִϸ��̼ǿ����� ��ü �ð� ���� �� ������ �����̹Ƿ� normalizedTime�� 0���� ū 0.5�� ����� �� ���� �߼Ҹ��� �߰�
        if(TryGetAnimatorStateInfo(AnimatorLayerName_Motion)) {
            if(physicsMoveSpeed > 0) {
                float normalizedTime = animatorStateInfo[AnimatorLayerName_Motion].Info.normalizedTime;
                int normalizedTimeInteger = (int)Mathf.Floor((normalizedTime + moveSoundOffset) / 0.5f);
                if(animatorStateInfo[AnimatorLayerName_Motion].CompareInteger < normalizedTimeInteger) {
                    // �÷��̾������ �Ÿ��� ���� �Ÿ� �̻��̶�� ���� SoundObject�� �������� ����
                    List<Vector3> pathToPlayer = LevelLoader.Instance.GetPath(transform.position, UtilObjects.Instance.CamPos, Radius);
                    float dist = LevelLoader.Instance.GetPathDistance(pathToPlayer);
                    if(dist < LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH) {
                        SoundManager.Instance.PlayOnWorld(transform.position, SoundManager.SoundType.MouseClick);
                    }

                    animatorStateInfo[AnimatorLayerName_Motion].CompareInteger = normalizedTimeInteger;
                }
            }
        }
    }

    #region Utility
    public void StartMove() {
        if(movePath == null || movePath.Count <= 0) {
            movePath = LevelLoader.Instance.GetRandomPointPath(transform.position, Radius);
        }
    }
    #endregion

    #region Action
    private void OnWorldSoundAdded(SoundObject so) {

    }

    private void OnWorldSoundRemoved() {

    }
    #endregion

    /// <summary>
    /// ���� ���� animatorStateInfo�� ����
    /// </summary>
    private bool TryGetAnimatorStateInfo(string layerName) {
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

    private class StuckHelper {
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
            Vector3 p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //���Ƿ� player�� ���̸� ����
            IsHit = Physics.CapsuleCast(p1, p2, RayRadius, direction, out hit, distance, Mask);
        }
        #endregion
    }

    private class AnimatorStateHelper {
        public AnimatorStateInfo Info;
        public int CompareInteger;
    }
}
