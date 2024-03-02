using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 움직일 때에 발소리가 나지 않음. 대신 지속적으로 소리를 내어 자신의 위치를 알려줌
/// </summary>
public class Cloudy : MonsterController, IMove {
    private const float restTime = 5.0f;
    private float restTimeChecker = 0.0f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0.0f, 1.0f)] private float audioVolumeMax = 0.85f;

    public static readonly float STANDARD_RIM_RADIUS_SPREAD_LENGTH = MazeBlock.BlockSize * 5.0f;



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

        audioSource.clip = SoundManager.Instance.GetSfxClip(SoundManager.SoundType.Whisper);
        audioSource.minDistance = 0.0f;
        audioSource.maxDistance = STANDARD_RIM_RADIUS_SPREAD_LENGTH;
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
            float normalizedDist = cameraDist / STANDARD_RIM_RADIUS_SPREAD_LENGTH;
            float volume = Mathf.Clamp01(1.0f - normalizedDist) * audioVolumeMax;
            audioSource.volume = Mathf.Lerp(audioSource.volume, volume, Time.deltaTime * 0.4f);
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
                    if(Vector3.Distance(so.Position, Pos) < STANDARD_RIM_RADIUS_SPREAD_LENGTH) {
                        List<Vector3> newPath = LevelLoader.Instance.GetPath(Pos, so.Position, Radius);
                        float dist = LevelLoader.Instance.GetPathDistance(newPath);
                        if(dist <= STANDARD_RIM_RADIUS_SPREAD_LENGTH * 2) {
                            movePath = newPath;

                            physicsMoveSpeedMax = 1.0f;
                            FollowingSound = so;

                            CurrentState = MonsterState.Move;
                        }
                    }
                }
                break;
            case SoundManager.SoundType.Empty00_5s: {
                    if(FollowingSound == null &&
                        from == SoundManager.SoundFrom.Monster &&
                        Vector3.Distance(so.Position, Pos) < Froggy.STANDARD_RIM_RADIUS_SPREAD_LENGTH) {
                        movePath = LevelLoader.Instance.GetPath(Pos, so.Position, Radius);

                        physicsMoveSpeedMax = 1.0f;
                        FollowingSound = so;

                        CurrentState = MonsterState.Move;
                    }
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
