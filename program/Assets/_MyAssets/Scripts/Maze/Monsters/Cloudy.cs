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

    private Vector2Int coordChecker;

    public static float STANDARD_RIM_RADIUS_SPREAD_LENGTH { get; private set; }



    protected override void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += WorldSoundRemoved;

        UtilObjects.Instance.OnGamePaused += OnGamePaused;

        OnCurrentStateChanged += CurrentStateChanged;
        OnPathEnd += PathEnd;

        base.Awake();
    }

    protected override void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= WorldSoundRemoved;

        UtilObjects.Instance.OnGamePaused -= OnGamePaused;

        OnCurrentStateChanged -= CurrentStateChanged;
        OnPathEnd -= PathEnd;

        base.OnDestroy();
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;
        physicsMoveSpeed = 0.0f;

        int mask = (1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));
        stuckHelper = new StuckHelper(Radius, mask);

        STANDARD_RIM_RADIUS_SPREAD_LENGTH = SoundManager.Instance.GetSpreadLength(SoundManager.SoundType.Whisper) * 0.5f;

        audioSource.minDistance = 0.0f;
        audioSource.maxDistance = STANDARD_RIM_RADIUS_SPREAD_LENGTH;
    }

    private void Update() {
        if(!IsPlaying || CurrentState == MonsterState.None) return;

        if(CurrentState == MonsterState.Rest) {
            restTimeChecker += Time.deltaTime;
            if(restTimeChecker >= restTime) {
                CurrentState = MonsterState.Move;
            }
        }

        Move(Time.deltaTime);

        if(CurrentState != MonsterState.None) {
            // Idle Sound
            //float cameraDist = Vector3.Distance(Pos, UtilObjects.Instance.CamPos);
            //float normalizedDist = cameraDist / STANDARD_RIM_RADIUS_SPREAD_LENGTH;
            //float volume = Mathf.Clamp01(1.0f - normalizedDist) * audioVolumeMax;
            //audioSource.volume = Mathf.Lerp(audioSource.volume, volume, Time.deltaTime * 0.4f);
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
            physicsMoveSpeed = Mathf.Clamp(physicsMoveSpeed - dt, 0.0f, physicsMoveSpeedMax);
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

        if(audioSource.clip == null) audioSource.clip = SoundManager.Instance.GetSfxClip(SoundManager.SoundType.Whisper);
        audioSource.volume = 0.65f;
        audioSource.Play();
    }

    public override void Stop() {
        CurrentState = MonsterState.None;

        StartCoroutine(IdleSoundFadeOut(1.0f));
    }
    #endregion

    #region Action
    private void OnGamePaused(bool isPaused) {
        if(isPaused) {
            audioSource.Pause();
        }
        else {
            audioSource.UnPause();
        }
    }

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
