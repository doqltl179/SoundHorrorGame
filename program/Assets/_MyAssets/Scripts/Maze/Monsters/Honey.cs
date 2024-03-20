using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Honey : MonsterController, IMove {
    private const float restTime = 5.0f;
    private float restTimeChecker = 0.0f;

    private Vector2Int coordChecker;



    protected override void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += WorldSoundRemoved;

        OnCurrentStateChanged += CurrentStateChanged;
        OnPathEnd += PathEnd;

        base.Awake();
    }

    protected override void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= WorldSoundRemoved;

        OnCurrentStateChanged -= CurrentStateChanged;
        OnPathEnd -= PathEnd;

        base.OnDestroy();
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;
        physicsMoveSpeed = 0.0f;

        int mask = (1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));
        stuckHelper = new StuckHelper(Radius, mask);
    }

    private void Update() {
        if(!IsPlaying || CurrentState == MonsterState.None) return;

        if(CurrentState == MonsterState.Rest) {
            restTimeChecker += Time.deltaTime;
            if(restTimeChecker >= restTime) {
                CurrentState = MonsterState.Move;
            }

            rigidbody.velocity = Vector3.zero;
        }

        Move(Time.deltaTime);

        // 모션 애니메이션은 Idle -> Walk -> Run 형태의 BlendTree를 가지고 있음
        // Walk, Run 애니메이션에서는 전체 시간 동안 두 걸음을 움직이므로 normalizedTime이 0보다 큰 0.5의 배수일 때 마다 발소리를 추가
        if(TryGetAnimatorStateInfo(AnimatorLayerName_Motion)) {
            if(physicsMoveSpeed > 0) {
                float normalizedTime = animatorStateInfo[AnimatorLayerName_Motion].Info.normalizedTime;
                int normalizedTimeInteger = (int)Mathf.Floor((normalizedTime + moveSoundOffset) / 0.5f);
                if(animatorStateInfo[AnimatorLayerName_Motion].CompareInteger != normalizedTimeInteger) {
                    // 플레이어까지의 거리가 일정 거리 이상이라면 굳이 SoundObject를 생성하지 않음
                    float dist = Vector3.Distance(Pos, UtilObjects.Instance.CamPos);
                    float clipSpreadLength = SoundManager.Instance.GetSpreadLength(SoundManager.SoundType.MonsterWalk02);
                    float compareDist = clipSpreadLength * 2;
                    if(dist < compareDist) {
                        SoundManager.Instance.PlayOnWorld(
                            transform.position,
                            SoundManager.SoundType.MonsterWalk02,
                            SoundManager.SoundFrom.Monster,
                            1.0f - dist / compareDist);
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
        if(movePath != null && movePath.Count > 0) {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed + dt * moveBoost, 0.0f, physicsMoveSpeedMax);

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

            if(Vector3.Distance(transform.position, movePath[0]) < Radius * 1.05f) {
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
    private void WorldSoundAdded(SoundObject so, SoundManager.SoundFrom from) {
        if(!IsPlaying) return;

        bool moveTo = false;
        switch(from) {
            case SoundManager.SoundFrom.Player:
                moveTo = true;
                break;

            default:
                moveTo = so.Type == SoundManager.SoundType.Empty00_5s ||
                    so.Type == SoundManager.SoundType.Empty01s ||
                    so.Type == SoundManager.SoundType.Empty02s ||
                    so.Type == SoundManager.SoundType.Empty03s ||
                    so.Type == SoundManager.SoundType.Empty04s ||
                    so.Type == SoundManager.SoundType.Empty05s;
                break;
        }

        if(moveTo) {
            Vector2Int coordChecker = LevelLoader.Instance.GetMazeCoordinate(so.Position);
            if(!LevelLoader.Instance.IsCoordInLevelSize(coordChecker, 0)) return;

            if(Vector3.Distance(so.Position, Pos) < so.SpreadLength) {
                List<Vector3> newPath = LevelLoader.Instance.GetPath(Pos, so.Position, Radius);
                float dist = LevelLoader.Instance.GetPathDistance(newPath);
                if(dist <= so.SpreadLength * 1.5f) {
                    movePath = newPath;

                    physicsMoveSpeedMax = 1.0f;
                    FollowingSound = so;

                    CurrentState = MonsterState.Move;
                }
            }
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
                    FollowingSound = null;
                    physicsMoveSpeedMax = 0.5f;
                }
                break;
        }
    }

    private void PathEnd() {
        CurrentState = MonsterState.Rest;
    }
    #endregion
}
