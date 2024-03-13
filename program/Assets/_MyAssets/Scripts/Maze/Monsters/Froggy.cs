using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <br/>다른 몬스터들과 다르게 움직이지 않음
/// <br/>플레이어가 찾아야하는 아이템의 소리를 내어 플레이어를 자신의 위치로 유도
/// </summary>
public class Froggy : MonsterController {
    private const int checkCoordRange = 1;
    private bool isPlayerInsideCheckCoordRange;

    private const float fakeItemSoundPlayTimeInterval = 10.0f;
    private float fakeItemSoundPlayTimeChecker = 0.0f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0.0f, 1.0f)] private float audioVolumeMax = 0.85f;

    private RaycastHit screamCheckHit;
    private int screamCheckRayMask;

    private const string AnimationTriggerName_Scream = "Scream";

    public static readonly float STANDARD_RIM_RADIUS_SPREAD_LENGTH = MazeBlock.BlockSize * 10.0f;

    private IEnumerator screamActionCoroutine = null;



    protected override void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += WorldSoundAdded;

        PlayerController.Instance.OnCoordChanged += PlayerCoordChanged;

        OnCurrentStateChanged += CurrentStateChanged;

        base.Awake();
    }

    protected override void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= WorldSoundAdded;

        PlayerController.Instance.OnCoordChanged -= PlayerCoordChanged;

        OnCurrentStateChanged -= CurrentStateChanged;

        base.OnDestroy();
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;
        transform.eulerAngles = Vector3.up * Random.Range(-180.0f, 180.0f);

        screamCheckRayMask =
            (1 << LayerMask.NameToLayer(MazeBlock.WallLayerName)) |
            (1 << LayerMask.NameToLayer(MazeBlock.EdgeLayerName)) | 
            (1 << LayerMask.NameToLayer(PlayerController.LayerName));

        audioSource.clip = SoundManager.Instance.GetSfxClip(SoundManager.SoundType.Scream);
        audioSource.minDistance = 0.0f;
        audioSource.maxDistance = STANDARD_RIM_RADIUS_SPREAD_LENGTH;
    }

    private void Update() {
        if(!IsPlaying) return;

        if(screamActionCoroutine == null && CurrentState != MonsterState.Scream) {
            //if(!isPlayerInsideCheckCoordRange) {
                // Fake Item Sound
                fakeItemSoundPlayTimeChecker += Time.deltaTime;
                if(fakeItemSoundPlayTimeChecker >= fakeItemSoundPlayTimeInterval) {
                    float dist = Vector3.Distance(Pos, UtilObjects.Instance.CamPos);
                    float spreadLength = SoundManager.Instance.GetSpreadLength(SoundManager.SoundType.Crystal);
                    if(dist < spreadLength) {
                        SoundManager.Instance.PlayOnWorld(
                            Pos,
                            SoundManager.SoundType.Crystal,
                            SoundManager.SoundFrom.Item,
                            1.0f - dist / spreadLength);
                    }

                    fakeItemSoundPlayTimeChecker -= fakeItemSoundPlayTimeInterval;
                }
            //}
        }
    }

    #region Override
    public override void Play() {
        CurrentState = MonsterState.Search;
    }

    public override void Stop() {
        CurrentState = MonsterState.None;
    }
    #endregion

    #region Action
    private void PlayerCoordChanged(Vector2Int coord) {
        if(CurrentState == MonsterState.Scream) {
            return;
        }

        Vector2Int currentCoord = LevelLoader.Instance.GetMazeCoordinate(Pos);
        Vector2Int lb = new Vector2Int(currentCoord.x - checkCoordRange, currentCoord.y - checkCoordRange);
        Vector2Int rt = new Vector2Int(currentCoord.x + checkCoordRange, currentCoord.y + checkCoordRange);
        isPlayerInsideCheckCoordRange = (lb.x <= coord.x && coord.x <= rt.x) && (lb.y <= coord.y && coord.y <= rt.y);
    }

    private void WorldSoundAdded(SoundObject so, SoundManager.SoundFrom from) {
        if(!IsPlaying) return;

        switch(so.Type) {
            case SoundManager.SoundType.PlayerWalk: {
                    if(isPlayerInsideCheckCoordRange && CurrentState != MonsterState.Scream) {
                        if(Physics.Raycast(
                        Pos,
                        (PlayerController.Instance.Pos - Pos).normalized,
                        out screamCheckHit,
                        float.MaxValue,
                        screamCheckRayMask)) {
                            if(screamCheckHit.collider.CompareTag(PlayerController.TagName) &&
                                PlayerController.Instance.CurrentState != PlayerController.PlayerState.None &&
                                PlayerController.Instance.CurrentState != PlayerController.PlayerState.Crouch) {
                                CurrentState = MonsterState.Scream;
                            }
                        }
                    }
                }
                break;
        }
    }

    private void CurrentStateChanged(MonsterState state) {
        switch(state) {
            case MonsterState.None: {

                }
                break;
            case MonsterState.Search: {
                    fakeItemSoundPlayTimeChecker = 0.0f;
                }
                break;
            case MonsterState.Scream: {
                    transform.forward = (PlayerController.Instance.Pos - Pos).normalized;

                    if(screamActionCoroutine != null) {
                        StopCoroutine(screamActionCoroutine);
                        audioSource.Stop();
                    }
                    screamActionCoroutine = ScreamActionCoroutine();
                    StartCoroutine(screamActionCoroutine);
                }
                break;
        }
    }
    #endregion

    private IEnumerator ScreamActionCoroutine() {
        Debug.Log("Start screaming!");

        audioSource.Play();
        animator.SetTrigger(AnimationTriggerName_Scream);
        yield return null;

        //const string animationName_Scream = "Scream";
        const float screamFakeSoundTimeInterval = 0.5f;
        float screamSoundTimeChecker = 0.0f;
        const int screamCount = 6;
        int screamCountChecker = 0;
        while(screamCountChecker < screamCount) {
            //if(TryGetAnimatorStateInfo(AnimatorLayerName_Motion)) {
            //    if(!animatorStateInfo[AnimatorLayerName_Motion].Info.IsName(animationName_Scream)) {
            //        break;
            //    }
            //}

            screamSoundTimeChecker += Time.deltaTime;
            if(screamSoundTimeChecker >= screamFakeSoundTimeInterval) {
                SoundManager.Instance.PlayOnWorld(Pos, SoundManager.SoundType.Empty05s, SoundManager.SoundFrom.Monster, 0.0f);

                screamSoundTimeChecker -= screamFakeSoundTimeInterval;
                screamCountChecker++;
            }

            yield return null;
        }

        CurrentState = MonsterState.Search;

        screamActionCoroutine = null;
    }
}
