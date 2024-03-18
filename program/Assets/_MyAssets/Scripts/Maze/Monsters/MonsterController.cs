using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Random = UnityEngine.Random;

public class MonsterController : MonoBehaviour {
    public static readonly string TagName = "Monster";
    public static readonly string LayerName = "Monster";

    [SerializeField] private LevelLoader.MonsterType type;
    public LevelLoader.MonsterType Type { get { return type; } }

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

    public bool IsPlaying { get { return CurrentState != MonsterState.None; } }

    public Vector2Int CurrentCoord { get; protected set; }

    [Header("Components")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rigidbody rigidbody;
    [SerializeField] protected CapsuleCollider collider;
    [SerializeField] protected Material material;

    public Rigidbody Rigidbody { get { return rigidbody; } }

    [Header("Properties")]
    [SerializeField] private Transform headPos;
    [SerializeField, Range(0.1f, 5.0f)] protected float scaleScalar = 1.0f;
    [SerializeField, Range(0.0f, 10.0f)] protected float moveSpeed = 1.0f;
    /// <summary>
    /// <br/> 두 걸음 동안의 거리.
    /// <br/> Move, Run 애니메이션이 모두 두 걸음을 걷기 때문에 이에 맞는 이동 거리를 입력.
    /// </summary>
    [SerializeField, Range(0.0f, 10.0f)] protected float twoStepDistance = 1.0f;
    [SerializeField, Range(0.0f, 10.0f)] protected float moveBoost = 0.4f;
    [SerializeField, Range(0.0f, 10.0f)] protected float rotateSpeed = 1.0f;
    [SerializeField, Range(-0.5f, 0.5f)] protected float moveSoundOffset = 0.0f;

    private Material copyMaterial = null;
    public Material Material {
        get { 
            if(copyMaterial == null) {
                copyMaterial = new Material(material.shader);
                copyMaterial.CopyPropertiesFromMaterial(material);

                SkinnedMeshRenderer[] renderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach(SkinnedMeshRenderer r in renderers) {
                    r.material = copyMaterial;
                }
            }
            return copyMaterial; 
        } 
    }

    private ParticleSystem throwAwayEffect = null;

    public float Radius { get { return collider.radius * scaleScalar; } }
    public float Height { get { return collider.height * scaleScalar; } }
    public Vector3 HeadPos { get { return headPos.position; } }
    public Vector3 HeadForward { get { return headPos.forward; } }
    public Vector3 Pos { 
        get => transform.position;
        set => transform.position = value;
    }
    public Quaternion Rotation {
        get => transform.rotation;
        set => transform.rotation = value;
    }

    protected const string AnimatorLayerName_Motion = "Motion";
    protected const string AnimatorPropertyName_MoveBlend = "MoveBlend";
    protected const string AnimatorPropertyName_MoveSpeed = "MoveSpeed";
    protected const string AnimatorPropertyName_PlayerCatch = "PlayerCatch";
    protected const string AnimatorPropertyName_TPos = "T-Pos";
    protected const string AnimatorPropertyName_BadEndingIdle = "BadEnding";
    protected const string AnimatorPropertyName_HappyEndingIdle = "HappyEnding";

    protected float physicsMoveSpeed = 0.0f;
    protected float physicsMoveSpeedMax = 0.5f;
    public SoundObject FollowingSound { get; protected set; }

    protected Dictionary<string, AnimatorStateHelper> animatorStateInfo = new Dictionary<string, AnimatorStateHelper>();

    protected List<Vector3> movePath = null;
    protected StuckHelper stuckHelper = null;

    public bool IsPlayerCatchAnimationPlaying { get { return playerCatchAnimationCoroutine != null; } }

    protected IEnumerator playerCatchAnimationCoroutine = null;
    protected IEnumerator throwAwayAnimationCoroutine = null;

    public Action<MonsterState> OnCurrentStateChanged;
    public Action OnPathEnd;



    protected virtual void Awake() {
        gameObject.tag = TagName;
        gameObject.layer = LayerMask.NameToLayer(LayerName);

        for(int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.tag = gameObject.tag;
            transform.GetChild(i).gameObject.layer = gameObject.layer;
        }
    }

    protected virtual void OnDestroy() {
        if(playerCatchAnimationCoroutine != null) {
            StopCoroutine(playerCatchAnimationCoroutine);
            playerCatchAnimationCoroutine = null;
        }
        if(throwAwayAnimationCoroutine != null) {
            StopCoroutine(throwAwayAnimationCoroutine);
            throwAwayAnimationCoroutine = null;
        }
    }

    #region Virtual
    public virtual void Play() { }

    public virtual void Stop() { }

    public virtual void PlayerCatchAnimation() {
        if(playerCatchAnimationCoroutine == null) {
            playerCatchAnimationCoroutine = PlayerCatchAnimationCoroutine();
            StartCoroutine(playerCatchAnimationCoroutine);
        }
    }

    protected virtual IEnumerator PlayerCatchAnimationCoroutine() {
        SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.CatchScream, SoundManager.SoundFrom.Monster, 0.75f);
        animator.SetTrigger(AnimatorPropertyName_PlayerCatch);

        Vector3 camStartPos = UtilObjects.Instance.CamPos;
        Vector3 camStartForward = UtilObjects.Instance.CamForward;
        Vector3 camPos;
        Vector3 camForward;
        float cameraDistance = Radius * scaleScalar;
        float animationTime = 0.5f;
        float timeChecker = 0.0f;
        float timeRatio = 0.0f;
        float lerp;

        // 카메라 마주보기
        while(timeChecker < animationTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / animationTime;
            lerp = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);

            camPos = HeadPos + HeadForward * cameraDistance;
            UtilObjects.Instance.CamPos = Vector3.Lerp(camStartPos, camPos, lerp);

            camForward = (HeadPos - UtilObjects.Instance.CamPos).normalized;
            UtilObjects.Instance.CamForward = Vector3.Lerp(camStartForward, camForward, lerp);

            yield return null;
        }

        // 딜레이
        camStartPos = UtilObjects.Instance.CamPos;
        camStartForward = UtilObjects.Instance.CamForward;
        const float shakeStrength = 0.1f;
        Vector3 shake;
        animationTime = 0.5f;
        timeChecker = 0.0f;
        while(timeChecker < animationTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / animationTime;
            lerp = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);

            camPos = HeadPos + HeadForward * cameraDistance;
            shake = UtilObjects.Instance.CamRight * Random.Range(-shakeStrength, shakeStrength);
            shake += UtilObjects.Instance.CamUp * Random.Range(-shakeStrength, shakeStrength);
            UtilObjects.Instance.CamPos = Vector3.Lerp(camStartPos, camPos + shake, lerp);

            camForward = (HeadPos - UtilObjects.Instance.CamPos).normalized;
            UtilObjects.Instance.CamForward = Vector3.Lerp(camStartForward, camForward, lerp);

            yield return null;
        }

        // 줌 인
        camStartPos = UtilObjects.Instance.CamPos;
        camStartForward = UtilObjects.Instance.CamForward;
        animationTime = 0.5f;
        timeChecker = 0.0f;
        while(timeChecker < animationTime) {
            timeChecker += Time.deltaTime;
            timeRatio = timeChecker / (animationTime * 1.77f);
            lerp = Mathf.Sin(Mathf.PI * 0.5f * timeRatio);

            UtilObjects.Instance.CamPos = Vector3.Lerp(camStartPos, HeadPos, lerp);

            camForward = (HeadPos - UtilObjects.Instance.CamPos).normalized;
            UtilObjects.Instance.CamForward = Vector3.Lerp(camStartForward, camForward, lerp);

            yield return null;
        }

        playerCatchAnimationCoroutine = null;
    }
    #endregion

    #region Utility
    public void ThrowArray(Vector3 dir) {
        if(throwAwayAnimationCoroutine == null) {
            throwAwayAnimationCoroutine = ThrowArrayAnimationCoroutine(dir);
            StartCoroutine(throwAwayAnimationCoroutine);
        }
    }

    private IEnumerator ThrowArrayAnimationCoroutine(Vector3 dir) {
        Stop();

        if(throwAwayEffect == null) {
            GameObject resourceObj = ResourceLoader.GetResource<GameObject>(Path.Combine("Effects", "ToyHammerHit"));
            GameObject go = Instantiate(resourceObj, transform);
            go.transform.localPosition = Vector3.up * Height * 0.5f;

            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            throwAwayEffect = ps;
        }
        throwAwayEffect.Play();

        rigidbody.useGravity = false;
        rigidbody.AddForce(dir * rigidbody.mass * 1000);

        Color originalColor = LevelLoader.Instance.GetBaseColor(Material);
        LevelLoader.Instance.SetBaseColor(Material, Color.white);
        SetTPos(true);

        yield return new WaitForSeconds(1.0f);
        rigidbody.useGravity = true;

        // 바닥에 닿을 때까지 대기
        Vector2Int currentCoord;
        while(Pos.y > 0.1f) {
            currentCoord = LevelLoader.Instance.GetMazeCoordinate(Pos);
            if(!LevelLoader.Instance.IsCoordInLevelSize(currentCoord, 0)) {
                LevelLoader.Instance.DestroyMonster(this);

                yield break;
            }

            yield return null;
        }

        LevelLoader.Instance.SetBaseColor(Material, originalColor);
        SetTPos(false);

        Play();
        throwAwayEffect.Stop();

        throwAwayAnimationCoroutine = null;
    }

    public void SetTPos(bool value) => animator.SetBool(AnimatorPropertyName_TPos, value);

    public void SetIdleToBadEnding() => animator.SetTrigger(AnimatorPropertyName_BadEndingIdle);
    public void SetIdleToHappyEnding() => animator.SetTrigger(AnimatorPropertyName_HappyEndingIdle);

    public void ResetAnimator() {
        animator.enabled = false;
        animator.enabled = true;
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

    protected class AnimatorStateHelper {
        public AnimatorStateInfo Info;
        public int CompareInteger;
    }
}
