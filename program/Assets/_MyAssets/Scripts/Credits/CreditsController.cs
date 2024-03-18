using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static SoundManager;

public class CreditsController : MonoBehaviour {
    private bool isBadEnding;

    [SerializeField] private Transform canvasAnchor;

    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0.001f, 10.0f)] private float sampleHeightMax = 0.2f;
    private SoundManager.SoundType bgmType;
    private float soundStrengthCheckValue;
    private Vector3 levelCenterPos;
    private readonly Color[] rimColors_BadEnding = new Color[] {
        Color.white, Color.white, Color.white, Color.white, Color.white, Color.white
    };
    private readonly Color[] rimColors_HappyEnding = new Color[] {
        Color.red, Color.green, Color.blue, Color.yellow, new Color(1.0f, 0.0f, 1.0f), new Color(0.0f, 1.0f, 1.0f)
    };
    private Color[] usingRimColors;

    private const int SampleLength = 64;
    private float[] samples = new float[SampleLength];
    private float[] sampleSum;
    private float[] sampleSumMax;

    [SerializeField, Range(0.01f, 1.0f)] private float soundInterval = 0.25f;
    private float soundInterChecker = 0.0f;

    private bool startSampling = false;



    private void OnDestroy() {
        StopAllCoroutines();
    }

    private IEnumerator Start() {
        Application.targetFrameRate = UserSettings.FPS;

        //isBadEnding = SceneLoader.Instance.Param != null && (bool)SceneLoader.Instance.Param[0];
        isBadEnding = true;

        // Init Level
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(3, 3, true);
        levelCenterPos = LevelLoader.Instance.GetCenterPos();

        // Monster
        Vector3 monsterLeftEndPos = LevelLoader.Instance.GetMazeBlock(0, 1).GetSidePos(MazeCreator.ActiveWall.F);
        Vector3 monsterRightEndPos = LevelLoader.Instance.GetMazeBlock(2, 1).GetSidePos(MazeCreator.ActiveWall.F);
        Vector3 monsterCenterPos = Vector3.Lerp(monsterLeftEndPos, monsterRightEndPos, 0.5f);

        // Init Camera
        Vector3 camPos = Vector3.zero;
        if(isBadEnding) camPos = LevelLoader.Instance.GetBlockPos(1, 0) + Vector3.up * MazeBlock.WallHeight * 0.35f;
        else camPos = LevelLoader.Instance.GetBlockPos(1, 0) + Vector3.up * MazeBlock.WallHeight * 0.35f;
        Quaternion camRotation = Quaternion.LookRotation(((monsterCenterPos + Vector3.up * 2.5f) - camPos).normalized);
        UtilObjects.Instance.CamPos = camPos;
        UtilObjects.Instance.CamRotation = camRotation;

        // Set Canvas Anchor
        canvasAnchor.position = monsterCenterPos + Vector3.up * 5;
        canvasAnchor.rotation = Quaternion.LookRotation((camPos - canvasAnchor.position).normalized);

        // 임의의 자리에 몬스터 생성
        foreach(LevelLoader.MonsterType type in Enum.GetValues(typeof(LevelLoader.MonsterType))) {
            LevelLoader.Instance.AddMonsterOnLevel(type, new Vector2Int(0, 0));
        }
        yield return null;

        // Init Monsters
        const float posAngleOffset = Mathf.PI * 0.35f;
        const float leftAngle = Mathf.PI * 0.5f + posAngleOffset * 0.5f;
        const float rightAngle = Mathf.PI * 0.5f - posAngleOffset * 0.5f;
        float calculatedAngle;

        Vector3 normalPos = new Vector3(camPos.x, 0.0f, camPos.z);
        float centerToCameraDist = Vector3.Distance(monsterCenterPos, normalPos);
        int monsterCount = LevelLoader.Instance.MonsterCount;
        MonsterController tempMonster;
        for(int i = 0; i < monsterCount; i++) {
            tempMonster = LevelLoader.Instance.Monsters[i];
            tempMonster.Rigidbody.isKinematic = true;

            calculatedAngle = Mathf.Lerp(leftAngle, rightAngle, (float)i / (monsterCount - 1));
            tempMonster.Pos = normalPos + new Vector3(Mathf.Cos(calculatedAngle), 0.0f, Mathf.Sin(calculatedAngle)) * centerToCameraDist;
            tempMonster.Rotation = Quaternion.LookRotation((new Vector3(camPos.x, 0.0f, camPos.z) - tempMonster.Pos).normalized);

            LevelLoader.Instance.SetBaseColor(tempMonster.Material, Color.black);
            LevelLoader.Instance.SetRimThickness(tempMonster.Material, MazeBlock.BlockSize * 2.0f);
            LevelLoader.Instance.SetRimThicknessOffset(tempMonster.Material, 2.0f);
            LevelLoader.Instance.SetUseBaseColor(tempMonster.Material, true);
            LevelLoader.Instance.SetDrawRim(tempMonster.Material, false);
            LevelLoader.Instance.SetDrawMonsterOutline(tempMonster.Material, true);
        }

        // Wait Loading
        while(SceneLoader.Instance.IsLoading) {
            yield return null;
        }
        yield return null;

        for(int i = 0; i < monsterCount; i++) {
            tempMonster = LevelLoader.Instance.Monsters[i];

            if(isBadEnding) tempMonster.SetIdleToBadEnding();
            else tempMonster.SetIdleToHappyEnding();
        }

        soundStrengthCheckValue = sampleHeightMax / 5;
        sampleSum = new float[LevelLoader.Instance.MonsterCount];
        sampleSumMax = new float[sampleSum.Length];

        usingRimColors = isBadEnding ? rimColors_BadEnding : rimColors_HappyEnding;

        bgmType = isBadEnding? SoundType.BadEnding : SoundType.HappyEnding;
        audioSource.clip = SoundManager.Instance.GetBgmClip(bgmType);
        audioSource.Play();

        startSampling = true;

#if UNITY_EDITOR
        #region 나중에 삭제하세요
        SceneLoader.Instance.ChangeCurrentLoadedSceneImmediately(SceneLoader.SceneType.Credits);
        #endregion
#endif

        yield return null;
        yield return StartCoroutine(EndCredits());
    }

    private void Update() {
        if(!startSampling) return;

        audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        int copyStartIndex;
        int copyEndIndex;
        float sum;
        for(int i = 0; i < sampleSum.Length; i++) {
            copyStartIndex = Mathf.FloorToInt(samples.Length * ((float)i / sampleSum.Length));
            copyEndIndex = Mathf.FloorToInt(samples.Length * ((float)(i + 1) / sampleSum.Length));
            sum = samples[copyStartIndex..copyEndIndex].Sum();
            if(sum > sampleSum[i]) sampleSum[i] = Mathf.Lerp(sampleSum[i], sum, Time.deltaTime * 16);
            else sampleSum[i] = Mathf.Lerp(sampleSum[i], 0.0f, Time.deltaTime);

            if(sampleSum[i] > sampleSumMax[i]) sampleSumMax[i] = Mathf.Lerp(sampleSumMax[i], sampleSum[i], Time.deltaTime * 16);
            else sampleSumMax[i] = Mathf.Lerp(sampleSumMax[i], 0.0f, Time.deltaTime * 0.2f);
            LevelLoader.Instance.SetBaseColor(LevelLoader.Instance.Monsters[i].Material, usingRimColors[i] * Mathf.InverseLerp(0.0f, sampleSumMax[i], sampleSum[i]));
        }
    }

    private IEnumerator EndCredits() {
        while(audioSource.time / audioSource.clip.length < 0.9f) {
            yield return null;
        }

        StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(true, 5.0f));

        const float fadeTime = 5.0f;
        float timeChecker = 0.0f;
        float startVolume = audioSource.volume;
        while(timeChecker < fadeTime) {
            timeChecker += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0.0f, timeChecker / fadeTime);

            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        SceneLoader.Instance.LoadScene(SceneLoader.SceneType.Main);
    }
}
