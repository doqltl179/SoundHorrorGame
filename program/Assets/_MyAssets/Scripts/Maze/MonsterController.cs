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
    [SerializeField, Range(0.1f, 5.0f)] private float scaleScalar = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float moveSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)] private float rotateSpeed = 1.0f;

    public Material Material { get { return material; } }

    public float Radius { get { return collider.radius * scaleScalar; } }
    public float Height { get { return collider.height * scaleScalar; } }

    private const string AnimatorPropertyName_MoveAnimationBlend = "MoveAnimationBlend";

    private List<Vector3> movePath = null;
    private StuckHelper stuckHelper = null;


    private float physicsMoveSpeed = 0.0f;



    private void OnDrawGizmos() {
        if(movePath == null || movePath.Count < 2)
            return;

        // Gizmos 색상 설정
        Gizmos.color = Color.green;

        // points 배열에 있는 모든 좌표들을 순서대로 잇는 선을 그립니다.
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
    }

    private void FixedUpdate() {
        if(movePath != null && movePath.Count > 0) {
            physicsMoveSpeed = Mathf.Clamp01(physicsMoveSpeed + Time.deltaTime * moveSpeed);

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
            physicsMoveSpeed = Mathf.Clamp01(physicsMoveSpeed - Time.deltaTime * moveSpeed);
        }

        transform.position += transform.forward * physicsMoveSpeed * Time.deltaTime * moveSpeed;
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
            Vector3 p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //임의로 player의 높이를 적용
            if(Physics.CapsuleCast(p1, p2, RayRadius, direction, out hit, distance, Mask)) {
                IsHit = true;

                Debug.Log(hit.collider.name);
                Debug.DrawRay(rayPos, direction * hit.distance, Color.yellow);
            }
            else {
                IsHit = false;
            }
        }
        #endregion
    }
}
