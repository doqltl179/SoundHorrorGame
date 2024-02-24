using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������ ���� �߼Ҹ��� ���� ����. ��� ���������� �Ҹ��� ���� �ڽ��� ��ġ�� �˷���
/// </summary>
public class Cloudy : MonsterController, IMove {
    private const float restTime = 5.0f;
    private float restTimeChecker = 0.0f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0.0f, 1.0f)] private float audioVolumeMax = 0.85f;



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

        audioSource.minDistance = 0.0f;
        audioSource.maxDistance = LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH;

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
    }

    private void Update() {
        if(CurrentState != MonsterState.None) {
            // Idle Sound
            float cameraDist = Vector3.Distance(Pos, UtilObjects.Instance.CamPos);
            float normalizedDist = cameraDist / LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH;
            float volume = Mathf.Clamp01(1.0f - normalizedDist) * audioVolumeMax;
            audioSource.volume = Mathf.Lerp(audioSource.volume, volume, Time.deltaTime * 0.5f);
        }
    }

    #region Interface
    public void Move(float dt) {
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

        audioSource.Play();
    }

    public override void Stop() {
        CurrentState = MonsterState.None;

        StartCoroutine(IdleSoundFadeOut(1.0f));
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
                    audioSource.volume = 0.0f;

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
                    audioSource.volume = 0.0f;

                    movePath = null;
                }
                break;
        }
    }

    private void PathEnd() {
        CurrentState = MonsterState.Rest;
    }
    #endregion

    private IEnumerator IdleSoundFadeOut(float fadeTime) {
        float timeChecker = 0.0f;
        float startVolume = audioSource.volume;
        while(timeChecker < fadeTime) {
            timeChecker += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0.0f, timeChecker / fadeTime);

            yield return null;
        }
        audioSource.volume = 0.0f;
        audioSource.Stop();
    }
}
