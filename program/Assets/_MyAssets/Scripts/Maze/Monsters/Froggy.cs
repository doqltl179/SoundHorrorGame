using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <br/>다른 몬스터들과 다르게 움직이지 않음
/// <br/>플레이어가 찾아야하는 아이템의 소리를 내어 플레이어를 자신의 위치로 유도
/// </summary>
public class Froggy : MonsterController {
    private const string AnimationTriggerName_Scream = "Scream";



    protected override void Awake() {
        SoundManager.Instance.OnWorldSoundAdded += WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved += WorldSoundRemoved;

        OnCurrentStateChanged += CurrentStateChanged;

        base.Awake();
    }

    private void OnDestroy() {
        SoundManager.Instance.OnWorldSoundAdded -= WorldSoundAdded;
        SoundManager.Instance.OnWorldSoundRemoved -= WorldSoundRemoved;

        OnCurrentStateChanged -= CurrentStateChanged;
    }

    private void Start() {
        transform.localScale = Vector3.one * scaleScalar;
        transform.eulerAngles = Vector3.up * Random.Range(-180.0f, 180.0f);
    }

    private void Update() {
        if(!IsPlaying) return;


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

                }
                break;
            case MonsterState.Search: {

                }
                break;
            case MonsterState.Scream: {


                    movePath = null;
                }
                break;
        }
    }
    #endregion
}
