using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �÷��̾ �ٶ󺸰� ���� �� �������� ����. ��� �̵� �ӵ��� ������ ����
/// </summary>
public class Kitty : MonsterController, IMove {
    private StuckHelper stopHelper;

    private const float restTime = 5.0f;
    private float restTimeChecker = 0.0f;



    protected override void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += WorldSoundRemoved;

        OnCurrentStateChanged += CurrentStateChanged;
        OnPathEnd += PathEnd;

        base.Awake();
    }

    private void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= WorldSoundRemoved;

        OnCurrentStateChanged -= CurrentStateChanged;
        OnPathEnd -= PathEnd;
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;
        physicsMoveSpeed = 0.0f;

        int mask = (1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));
        stuckHelper = new StuckHelper(Radius, mask);

        stopHelper = new StuckHelper(Radius, mask | (1 << LayerMask.NameToLayer(PlayerController.LayerName)));

        animator.SetFloat(AnimatorPropertyName_MoveSpeed, moveSpeed * moveAnimationSpeed);
    }

    private void FixedUpdate() {
        if(!IsPlaying) return;

        if(CurrentState == MonsterState.Rest) {
            restTimeChecker += Time.deltaTime;
            if(restTimeChecker >= restTime) {
                CurrentState = MonsterState.Move;
            }
        }

        Move(Time.deltaTime);

        // ��� �ִϸ��̼��� Idle -> Walk -> Run ������ BlendTree�� ������ ����
        // Walk, Run �ִϸ��̼ǿ����� ��ü �ð� ���� �� ������ �����̹Ƿ� normalizedTime�� 0���� ū 0.5�� ����� �� ���� �߼Ҹ��� �߰�
        if(TryGetAnimatorStateInfo(AnimatorLayerName_Motion)) {
            if(physicsMoveSpeed > 0) {
                float normalizedTime = animatorStateInfo[AnimatorLayerName_Motion].Info.normalizedTime;
                int normalizedTimeInteger = (int)Mathf.Floor((normalizedTime + moveSoundOffset) / 0.5f);
                if(animatorStateInfo[AnimatorLayerName_Motion].CompareInteger < normalizedTimeInteger) {
                    // �÷��̾������ �Ÿ��� ���� �Ÿ� �̻��̶�� ���� SoundObject�� �������� ����
                    float dist = Vector3.Distance(Pos, UtilObjects.Instance.CamPos);
                    if(dist < LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH) {
                        List<Vector3> tempPath = LevelLoader.Instance.GetPath(Pos, UtilObjects.Instance.CamPos, Radius);
                        dist = LevelLoader.Instance.GetPathDistance(tempPath);
                        SoundManager.Instance.PlayOnWorld(
                            transform.position,
                            SoundManager.SoundType.MonsterWalk02,
                            SoundManager.SoundFrom.Monster,
                            1.0f - dist / LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH);
                    }

                    animatorStateInfo[AnimatorLayerName_Motion].CompareInteger = normalizedTimeInteger;
                }
            }
        }
    }

    #region Interface
    public void Move(float dt) {
        stopHelper.Raycast(UtilObjects.Instance.CamPos, (Pos - UtilObjects.Instance.CamPos).normalized, float.MaxValue);
        if(stopHelper.IsHit && (stopHelper.HitTag == PlayerController.TagName)) {
            Vector3 viewPoint = UtilObjects.Instance.Cam.WorldToViewportPoint(Pos);
            if(viewPoint.z > 0.0f && 
                0.0f <= viewPoint.x && viewPoint.x <= 1.0f && 
                0.0f <= viewPoint.y && viewPoint.y <= 1.0f) {
                physicsMoveSpeed = 0.0f;
                animator.SetFloat(AnimatorPropertyName_MoveSpeed, physicsMoveSpeed);

                return;
            }
            else {
                animator.SetFloat(AnimatorPropertyName_MoveSpeed, moveSpeed * moveAnimationSpeed);
            }
        }

        if(movePath != null && movePath.Count > 0) {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed + dt * moveBoost, 0.0f, physicsMoveSpeedMax);

            stuckHelper.Raycast(transform.position, transform.forward, Radius * 1.01f);
            if(stuckHelper.IsHit) {
                // �ε��� ���� normal�� �������� �����ϴ� ����
                Vector3 hitPosToPathPos = (movePath[0] - stuckHelper.HitPos).normalized;
                bool isRightSide = Vector3.Cross(stuckHelper.HitNormal, hitPosToPathPos).y > 0;
                Vector3 lookForward = Quaternion.AngleAxis(isRightSide ? 90 : -90, Vector3.up) * stuckHelper.HitNormal;
                transform.forward = Vector3.Lerp(transform.forward, lookForward, Time.deltaTime * rotateSpeed * 2.0f);
            }
            else {
                Vector3 moveDirection = (movePath[0] - transform.position).normalized;
                transform.forward = Vector3.Lerp(transform.forward, moveDirection, dt * rotateSpeed);
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

        // ��ġ �̵�
        if(physicsMoveSpeed > 0) {
            transform.position += transform.forward * physicsMoveSpeed * dt * moveSpeed * scaleScalar;
        }

        // �ִϸ��̼��� �ӵ� ����
        // physicsMoveSpeed�� 0�� �������� Idle�� ��ȯ
        animator.SetFloat(AnimatorPropertyName_MoveBlend, physicsMoveSpeed);

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
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
                            LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH * 2);
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
