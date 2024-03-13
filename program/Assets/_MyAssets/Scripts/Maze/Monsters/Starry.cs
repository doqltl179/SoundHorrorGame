using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다른 몬스터들과 다르게 소리를 쫒는게 아닌 플레이어가 일정 범위 안에 있을 때에 플레이어의 위치를 직접 쫒음
/// </summary>
public class Starry : MonsterController, IMove {
    private StuckHelper followHelper;
    private const float followTimeInvertal = 1.0f;
    private float followTimeChecker = 0.0f;

    private const float restTime = 5.0f;
    private float restTimeChecker = 0.0f;

    private Vector2Int coordChecker;



    protected override void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += WorldSoundRemoved;

        //PlayerController.Instance.OnCoordChanged += PlayerCoordChanged;

        OnCurrentStateChanged += CurrentStateChanged;
        OnPathEnd += PathEnd;

        base.Awake();
    }

    protected override void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= WorldSoundRemoved;

        //PlayerController.Instance.OnCoordChanged -= PlayerCoordChanged;

        OnCurrentStateChanged -= CurrentStateChanged;
        OnPathEnd -= PathEnd;

        base.OnDestroy();
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;
        physicsMoveSpeed = 0.0f;

        int mask = (1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));
        stuckHelper = new StuckHelper(Radius, mask);

        followHelper = new StuckHelper(Radius, mask | (1 << LayerMask.NameToLayer(PlayerController.LayerName)));
    }

    private void Update() {
        if(!IsPlaying) return;

        if(CurrentState == MonsterState.Rest) {
            restTimeChecker += Time.deltaTime;
            if(restTimeChecker >= restTime) {
                CurrentState = MonsterState.Move;
            }
        }

        Move(Time.deltaTime);

        // 모션 애니메이션은 Idle -> Walk -> Run 형태의 BlendTree를 가지고 있음
        // Walk, Run 애니메이션에서는 전체 시간 동안 두 걸음을 움직이므로 normalizedTime이 0보다 큰 0.5의 배수일 때 마다 발소리를 추가
        if(TryGetAnimatorStateInfo(AnimatorLayerName_Motion)) {
            if(physicsMoveSpeed > 0) {
                float normalizedTime = animatorStateInfo[AnimatorLayerName_Motion].Info.normalizedTime;
                int normalizedTimeInteger = (int)Mathf.Floor((normalizedTime + moveSoundOffset) / 0.5f);
                if(animatorStateInfo[AnimatorLayerName_Motion].CompareInteger < normalizedTimeInteger) {
                    // 플레이어까지의 거리가 일정 거리 이상이라면 굳이 SoundObject를 생성하지 않음
                    float dist = Vector3.Distance(Pos, UtilObjects.Instance.CamPos);
                    float clipSpreadLength = SoundManager.Instance.GetSpreadLength(SoundManager.SoundType.MonsterWalk01);
                    if(dist < clipSpreadLength) {
                        SoundManager.Instance.PlayOnWorld(
                            transform.position,
                            SoundManager.SoundType.MonsterWalk06,
                            SoundManager.SoundFrom.Monster,
                            1.0f - dist / clipSpreadLength);
                    }

                    animatorStateInfo[AnimatorLayerName_Motion].CompareInteger = normalizedTimeInteger;
                }
            }
        }

        // 위치 체크
        coordChecker = LevelLoader.Instance.GetMazeCoordinate(Pos);
        if(CurrentCoord.x != coordChecker.x || CurrentCoord.y != coordChecker.y) {
            CurrentCoord = coordChecker;


        }
    }

    #region Interface
    public void Move(float dt) {
        followTimeChecker += Time.deltaTime;
        if(followTimeChecker >= followTimeInvertal) {
            followHelper.Raycast(Pos, (UtilObjects.Instance.CamPos - Pos).normalized, float.MaxValue);
            if(followHelper.IsHit && (followHelper.HitTag == PlayerController.TagName)) {
                Vector3 playerPos = PlayerController.Instance.Pos;
                movePath = new List<Vector3>() { new Vector3(playerPos.x, 0.0f, playerPos.z) };
            }

            followTimeChecker -= followTimeInvertal;
        }

        if(movePath != null && movePath.Count > 0) {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed + dt * moveBoost, 0.0f, 1.0f);

            stuckHelper.Raycast(transform.position, transform.forward, Radius * 1.01f);
            if(stuckHelper.IsHit) {
                // 부딪힌 곳의 normal을 기준으로 가야하는 방향
                Vector3 hitPosToPathPos = (movePath[0] - stuckHelper.HitPos).normalized;
                bool isRightSide = Vector3.Cross(stuckHelper.HitNormal, hitPosToPathPos).y > 0;
                Vector3 lookForward = Quaternion.AngleAxis(isRightSide ? 90 : -90, Vector3.up) * stuckHelper.HitNormal;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookForward), Time.deltaTime * rotateSpeed * 2.0f);
            }
            else {
                Vector3 moveDirection = (movePath[0] - transform.position).normalized;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveDirection), dt * rotateSpeed);
            }

            if(Vector3.Distance(transform.position, movePath[0]) < Radius) {
                movePath.RemoveAt(0);
                if(movePath.Count <= 0) {
                    OnPathEnd?.Invoke();
                }
            }
        }
        else {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed - dt * moveBoost, 0.0f, physicsMoveSpeedMax);
        }

        // 위치 이동
        if(physicsMoveSpeed > 0) {
            float moveDistanceOfOneSecond = physicsMoveSpeed * moveSpeed * scaleScalar;
            //transform.position += transform.forward * dt * moveDistanceOfOneSecond;
            rigidbody.velocity = transform.forward * moveDistanceOfOneSecond;
        }
        rigidbody.angularVelocity = Vector3.zero;

        // physicsMoveSpeed가 0에 가까울수록 Idle로 전환
        animator.SetFloat(AnimatorPropertyName_MoveBlend, physicsMoveSpeed);

        // 애니메이션의 속도 조정
        float animationSpeed;
        if(physicsMoveSpeed >= 0.5f) {
            float moveDistanceOfOneSecond = physicsMoveSpeed * moveSpeed * scaleScalar;
            animationSpeed = moveDistanceOfOneSecond / twoStepDistance;
        }
        else {
            animationSpeed = 1.0f - Mathf.InverseLerp(0.0f, 0.5f, physicsMoveSpeed);
        }
        animator.SetFloat(AnimatorPropertyName_MoveSpeed, animationSpeed);
    }
    #endregion

    #region Override
    public override void Play() {
        CurrentState = MonsterState.Move;
    }

    public override void Stop() {
        CurrentState = MonsterState.None;
    }
    #endregion

    #region Action
    //private void PlayerCoordChanged(Vector2Int coord) {
    //    Vector3 coordPos = LevelLoader.Instance.GetBlockPos(coord);
    //    if(Vector3.Distance(coordPos, Pos) < STANDARD_RIM_RADIUS_SPREAD_LENGTH) {
    //        movePath = LevelLoader.Instance.GetPath(Pos, coordPos, Radius);

    //        CurrentState = MonsterState.Move;
    //    }
    //}

    private void WorldSoundAdded(SoundObject so, SoundManager.SoundFrom from) {
        switch(so.Type) {
            case SoundManager.SoundType.PlayerWalk: {

                }
                break;
        }
    }

    private void WorldSoundRemoved(SoundManager.SoundFrom from) {

    }

    private void CurrentStateChanged(MonsterState state) {
        switch(state) {
            case MonsterState.None: {
                    movePath = null;
                }
                break;
            case MonsterState.Move: {
                    if(movePath == null || movePath.Count <= 0) {
                        movePath = LevelLoader.Instance.GetRandomPointPathCompareDistance(
                            Pos,
                            Radius,
                            false,
                            LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH);
                    }
                }
                break;
            case MonsterState.Rest: {
                    restTimeChecker = 0.0f;

                    movePath = null;
                }
                break;
        }
    }

    private void PathEnd() {
        CurrentState = MonsterState.Rest;
    }
    #endregion
}
