using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bunny : MonsterController, IMove {
    protected override void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += OnWorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += OnWorldSoundRemoved;

        base.Awake();
    }

    private void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= OnWorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= OnWorldSoundRemoved;
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;
        physicsMoveSpeed = 0.0f;

        stuckHelper = new StuckHelper(Radius, 1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));

        animator.SetFloat(AnimatorPropertyName_MoveSpeed, moveSpeed * moveAnimationSpeed);
    }

    private void FixedUpdate() {
        Move(Time.deltaTime);

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

    #region Interface
    public void Move(float dt) {
        if(movePath != null && movePath.Count > 0) {
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed + dt * moveBoost, 0.0f, physicsMoveSpeedMax);

            stuckHelper.Raycast(transform.position, transform.forward, Radius * 1.01f);
            if(stuckHelper.IsHit) {
                Vector3 hitPosToCurrentPos = (transform.position - stuckHelper.HitPos).normalized;
                bool isRightSide = Vector3.Cross(hitPosToCurrentPos, stuckHelper.HitNormal).y > 0;
                Vector3 moveDirection = Quaternion.AngleAxis(isRightSide ? 90 : -90, Vector3.up) * stuckHelper.HitNormal;

                transform.forward = Vector3.Lerp(transform.forward, moveDirection, dt * rotateSpeed);
            }
            else {
                Vector3 moveDirection = (movePath[0] - transform.position).normalized;
                transform.forward = Vector3.Lerp(transform.forward, moveDirection, dt * rotateSpeed);
            }

            if(Vector3.Distance(transform.position, movePath[0]) < Radius) {
                movePath.RemoveAt(0);
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
    }
    #endregion

    #region Override
    #region Action
    protected override void OnWorldSoundAdded(SoundObject so) {
        switch(so.Type) {
            case SoundManager.SoundType.MouseClick: {

                }
                break;
        }
    }

    protected override void OnWorldSoundRemoved() {

    }
    #endregion
    #endregion
}
