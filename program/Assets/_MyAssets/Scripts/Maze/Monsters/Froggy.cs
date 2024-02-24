using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <br/>다른 몬스터들과 다르게 움직이지 않음
/// <br/>플레이어가 찾아야하는 아이템의 소리를 내어 플레이어를 자신의 위치로 유도
/// </summary>
public class Froggy : MonsterController {
    private const int checkRange = 1;
    private bool isPlayerInsideCheckRange;

    private RaycastHit screamCheckHit;
    private int screamCheckRayMask;

    private const string AnimationTriggerName_Scream = "Scream";



    protected override void Awake() {
        PlayerController.Instance.OnCoordChanged += PlayerCoordChanged;

        OnCurrentStateChanged += CurrentStateChanged;

        base.Awake();
    }

    private void OnDestroy() {
        PlayerController.Instance.OnCoordChanged -= PlayerCoordChanged;

        OnCurrentStateChanged -= CurrentStateChanged;
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;
        transform.eulerAngles = Vector3.up * Random.Range(-180.0f, 180.0f);

        screamCheckRayMask =
            (1 << LayerMask.NameToLayer(MazeBlock.WallLayerName)) |
            (1 << LayerMask.NameToLayer(PlayerController.LayerName));
    }

    private void Update() {
        if(!IsPlaying) return;

        if(isPlayerInsideCheckRange && CurrentState != MonsterState.Scream) {
            if(Physics.Raycast(
                Pos,
                (PlayerController.Instance.Pos - Pos).normalized, 
                out screamCheckHit,
                float.MaxValue, 
                screamCheckRayMask)) {
                if(screamCheckHit.collider.CompareTag(PlayerController.TagName) && 
                    PlayerController.Instance.CurrentState != PlayerController.PlayerState.Crouch) {
                    CurrentState = MonsterState.Scream;
                }
            }
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
        Vector2Int lb = new Vector2Int(currentCoord.x - checkRange, currentCoord.y - checkRange);
        Vector2Int rt = new Vector2Int(currentCoord.x + checkRange, currentCoord.y + checkRange);
        isPlayerInsideCheckRange = (lb.x <= coord.x && coord.x <= rt.x) && (lb.y <= coord.y && coord.y <= rt.y);
    }

    private void CurrentStateChanged(MonsterState state) {
        switch(state) {
            case MonsterState.None: {

                }
                break;
            case MonsterState.Search: {

                }
                break;
            case MonsterState.Scream: {
                    transform.forward = (PlayerController.Instance.Pos - Pos).normalized;

                    StartCoroutine(ScreamActionCoroutine());
                }
                break;
        }
    }
    #endregion

    private IEnumerator ScreamActionCoroutine() {
        Debug.Log("Start screaming!");
        const string animationName_Scream = "Scream";

        while(true) {
            animator.SetTrigger(AnimationTriggerName_Scream);
            yield return null;

            if(TryGetAnimatorStateInfo(AnimatorLayerName_Motion)) {
                if(animatorStateInfo[AnimatorLayerName_Motion].Info.IsName(animationName_Scream)) {
                    break;
                }
            }
        }
        yield return null;

        while(true) {
            if(TryGetAnimatorStateInfo(AnimatorLayerName_Motion)) {
                if(!animatorStateInfo[AnimatorLayerName_Motion].Info.IsName(animationName_Scream)) {
                    break;
                }
            }

            yield return null;
        }

        CurrentState = MonsterState.Search;
    }
}
