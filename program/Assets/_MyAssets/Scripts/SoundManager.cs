using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class SoundManager : GenericSingleton<SoundManager> {
    public enum SoundType {
        None, 

        // Fake Sound
        Empty00_5s, 
        Empty01s, 
        Empty02s, 
        Empty03s, 
        Empty04s, 
        Empty05s, 

        // Monster
        MonsterWalk01,
        MonsterWalk02,
        MonsterWalk05,
        MonsterWalk06,

        Scream, 
        Whisper,
        CatchScream, 

        // Item
        Crystal, 

        Teleport, 

        Mining01,
        Mining02,
        Mining03,
        Mining04,
        MiningEnd,

        ObjectHit01,
        ObjectHit02,
        ObjectHit03,
        ObjectHit04,
        ObjectHit05,

        ToyHammerHit, 

        // Player
        PlayerWalk,

        // BGM
        Main,
        Game, 
        Warning, 

        // Etc
        MouseClick, 
        ButtonClick, 

        GameEnter, 

        WallAnimation, 

        PathGuide, 
    }

    public enum SoundFrom {
        None,
        Player,
        Monster,
        Item
    }

    private readonly string BASIC_PATH_OF_SFX = "Audios/SFX";
    private readonly string BASIC_PATH_OF_BGM = "Audios/BGM";

    private List<SoundObject> noneFromSoundObjectList = new List<SoundObject>();
    private List<SoundObject> playerSoundObjectList = new List<SoundObject>();
    private List<SoundObject> monsterSoundObjectList = new List<SoundObject>();
    private List<SoundObject> itemSoundObjectList = new List<SoundObject>();

    private List<SoundObject> soundObjectPool = new List<SoundObject>(); //Pool

    private Dictionary<SoundType, AudioSource> bgmSources = new Dictionary<SoundType, AudioSource>();

    private AudioSource oneShotSource = null;

    public Action<SoundObject, SoundFrom> OnWorldSoundAdded;
    public Action<SoundFrom> OnWorldSoundRemoved;

    private IEnumerator micDecibelCheckCoroutine = null;



    private void Awake() {
        UserSettings.OnUseMicChanged += OnUseMicChanged;
    }

    private void OnDestroy() {
        UserSettings.OnUseMicChanged -= OnUseMicChanged;
    }

    private void Start() {
        if(UserSettings.UseMicBoolean) {
            micDecibelCheckCoroutine = MicDecibelCheckCoroutine();
            StartCoroutine(micDecibelCheckCoroutine);
        }
    }

    private IEnumerator MicDecibelCheckCoroutine() {
        const float timeInterval = 0.25f;
        WaitForSeconds wait = new WaitForSeconds(timeInterval);

        while(true) {
            yield return wait;

            if(MicrophoneRecorder.Instance.OverCritical) {
                Debug.Log($"{MicrophoneRecorder.Instance.Decibel} | {MicrophoneRecorder.Instance.DecibelRatio}");

                float overRatio = Mathf.InverseLerp(
                    MicrophoneRecorder.Instance.DecibelCriticalRatio,
                    1.0f,
                    MicrophoneRecorder.Instance.DecibelRatio);
                int soundValue = Mathf.FloorToInt(overRatio / 0.2f);
                switch(soundValue) {
                    case 0: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty01s, SoundFrom.Player, 0.0f); break;
                    case 1: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty02s, SoundFrom.Player, 0.0f); break;
                    case 2: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty03s, SoundFrom.Player, 0.0f); break;
                    case 3: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty04s, SoundFrom.Player, 0.0f); break;
                    case 4: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty05s, SoundFrom.Player, 0.0f); break;
                }
            }
        }

        micDecibelCheckCoroutine = null;
    }

    private void Update() {
        if(noneFromSoundObjectList.Count > 0) CheckSoundObjct(ref noneFromSoundObjectList, SoundFrom.None);
        if(playerSoundObjectList.Count > 0) CheckSoundObjct(ref playerSoundObjectList, SoundFrom.Player);
        if(monsterSoundObjectList.Count > 0) CheckSoundObjct(ref monsterSoundObjectList, SoundFrom.Monster);
        if(itemSoundObjectList.Count > 0) CheckSoundObjct(ref itemSoundObjectList, SoundFrom.Item);
    }

    private void CheckSoundObjct(ref List<SoundObject> list, SoundFrom from) {
        bool listChanged = false;
        for(int i = 0; i < list.Count; i++) {
            if(!list[i].IsPaused && !list[i].IsPlaying) {
                SoundObject so = list[i];
                list.RemoveAt(i);

                //so.Stop();
                //soundObjectPool.Add(so);
                StartCoroutine(StopAsync(so));

                i--;
                listChanged = true;
            }
        }
        if(listChanged) OnWorldSoundRemoved?.Invoke(from);
    }

    private void DestroyAllSoundObjectImmediately(ref List<SoundObject> list, SoundFrom from) {
        for(int i = 0; i < list.Count; i++) {
            Destroy(list[i].Source.gameObject);

            list.RemoveAt(i);
            i--;

            OnWorldSoundRemoved?.Invoke(from);
        }
    }

    private IEnumerator StopAsync(SoundObject so) {
        so.Stop();
        yield return null;

        soundObjectPool.Add(so);
    }

    #region Action
    private void OnUseMicChanged(bool value) {
        if(micDecibelCheckCoroutine != null) StopCoroutine(micDecibelCheckCoroutine);

        if(value) {
            micDecibelCheckCoroutine = MicDecibelCheckCoroutine();
            StartCoroutine(micDecibelCheckCoroutine);
        }
    }
    #endregion

    #region Utility
    public void ResetAllSoundObjects() {
        if(noneFromSoundObjectList.Count > 0) DestroyAllSoundObjectImmediately(ref noneFromSoundObjectList, SoundFrom.None);
        if(playerSoundObjectList.Count > 0) DestroyAllSoundObjectImmediately(ref playerSoundObjectList, SoundFrom.Player);
        if(monsterSoundObjectList.Count > 0) DestroyAllSoundObjectImmediately(ref monsterSoundObjectList, SoundFrom.Monster);
        if(itemSoundObjectList.Count > 0) DestroyAllSoundObjectImmediately(ref itemSoundObjectList, SoundFrom.Item);
    }

    public void UnPauseAllSound() {
        foreach(SoundObject so in noneFromSoundObjectList) {
            so.UnPause();
        }
        foreach(SoundObject so in playerSoundObjectList) {
            so.UnPause();
        }
        foreach(SoundObject so in monsterSoundObjectList) {
            so.UnPause();
        }
        foreach(SoundObject so in itemSoundObjectList) {
            so.UnPause();
        }
    }

    public void PauseAllSounds() {
        foreach(SoundObject so in noneFromSoundObjectList) {
            so.Pause();
        }
        foreach(SoundObject so in playerSoundObjectList) {
            so.Pause();
        }
        foreach(SoundObject so in monsterSoundObjectList) {
            so.Pause();
        }
        foreach(SoundObject so in itemSoundObjectList) {
            so.Pause();
        }
    }

    public void PlayOneShot(SoundType type, float volume = 1.0f) {
        if(oneShotSource == null) {
            GameObject go = new GameObject("OneShotSource");
            go.transform.SetParent(transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.reverbZoneMix = 0.0f;

            oneShotSource = source;
        }

        oneShotSource.PlayOneShot(GetSfxClip(type), volume);
    }

    //public bool IsPlayingBGM(SoundType type) {
    //    AudioSource source = null;
    //    if(bgmSources.TryGetValue(type, out source)) {
    //        return source.isPlaying;
    //    }
    //    else {
    //        return false;
    //    }
    //}

    public void StopBGM(SoundType type, float fadeTime = 0.0f) {
        AudioSource bgmSource = null;
        if(bgmSources.TryGetValue(type, out bgmSource)) {
            if(fadeTime > 0.0f) {
                StartCoroutine(FadeCoroutine(bgmSource, fadeTime, bgmSource.volume, 0.0f, () => {
                    bgmSource.Stop();
                }));
            }
            else {
                bgmSource.volume = 0.0f;
                bgmSource.Stop();
            }
        }
        else {
            Debug.Log($"BGM not found. BGM: {type.ToString()}");
        }
    }

    public void PlayBGM(SoundType type, float fadeTime = 0.0f, float volume = 1.0f) {
        AudioSource bgmSource = null;
        if(bgmSources.TryGetValue(type, out bgmSource)) {
            if(bgmSource.isPlaying) return;
        }
        else {
            GameObject go = new GameObject(type.ToString());
            go.transform.SetParent(transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.reverbZoneMix = 0.0f;

            AudioClip clip = ResourceLoader.GetResource<AudioClip>(GetBgmPath(type));
            source.clip = clip;

            bgmSources.Add(type, source);
            bgmSource = source;
        }

        if(fadeTime > 0.0f) {
            bgmSource.Play();
            StartCoroutine(FadeCoroutine(bgmSource, fadeTime, 0.0f, volume));
        }
        else {
            bgmSource.volume = volume;
            bgmSource.Play();
        }
    }

    public void PlayOnWorld(Vector3 worldPos, SoundType type, SoundFrom from, float volume = 1.0f) {
        SoundObject so = GetSoundObject(type);
        so.Position = worldPos;

        switch(type) {
            case SoundType.Empty00_5s:
            case SoundType.Empty01s:
            case SoundType.Empty02s:
            case SoundType.Empty03s:
            case SoundType.Empty04s:
            case SoundType.Empty05s:
                so.Volume = 0.0f;
                break;

            default:
                so.Volume = volume;
                break;
        }

        so.Play();
        switch(from) {
            case SoundFrom.None: noneFromSoundObjectList.Add(so); break;
            case SoundFrom.Player: playerSoundObjectList.Add(so); break;
            case SoundFrom.Monster: monsterSoundObjectList.Add(so); break;
            case SoundFrom.Item: itemSoundObjectList.Add(so); break;
        }

        OnWorldSoundAdded?.Invoke(so, from);
    }

    public AudioClip GetBgmClip(SoundType type) => ResourceLoader.GetResource<AudioClip>(GetBgmPath(type));
    public AudioClip GetSfxClip(SoundType type) => ResourceLoader.GetResource<AudioClip>(GetSfxPath(type));

    #region Material Property Util Func
    public Vector4[] GetSoundObjectPosArray(SoundFrom from) {
        Vector4 vec3ToVec4(Vector3 v) => new Vector4(v.x, v.y, v.z);
        switch(from) {
            case SoundFrom.None: return noneFromSoundObjectList.Select(t => vec3ToVec4(t.Position)).ToArray();
            case SoundFrom.Player: return playerSoundObjectList.Select(t => vec3ToVec4(t.Position)).ToArray();
            case SoundFrom.Monster: return monsterSoundObjectList.Select(t => vec3ToVec4(t.Position)).ToArray();
            case SoundFrom.Item: return itemSoundObjectList.Select(t => vec3ToVec4(t.Position)).ToArray();
            default: return new Vector4[0];
        }
    }

    public float[] GetSoundObjectRadiusArray(SoundFrom from, float spreadTime, float spreadLength) {
        float calculateFunc(SoundObject so) {
            //float radius = so.CurrentTime / spreadTime * spreadLength;

            //float dist = Vector3.Distance(so.Position, UtilObjects.Instance.CamPos);
            //float radiusOffset = Mathf.Clamp01(1.0f - dist / spreadLength);

            //return radius * radiusOffset;

            return so.SpreadLength * so.NormalizedTime;
        }
        switch(from) {
            case SoundFrom.None: return noneFromSoundObjectList.Select(t => calculateFunc(t)).ToArray();
            case SoundFrom.Player: return playerSoundObjectList.Select(t => calculateFunc(t)).ToArray();
            case SoundFrom.Monster: return monsterSoundObjectList.Select(t => calculateFunc(t)).ToArray();
            case SoundFrom.Item: return itemSoundObjectList.Select(t => calculateFunc(t)).ToArray();
            default: return new float[0];
        }
    }

    public float[] GetSoundObjectAlphaArray(SoundFrom from, float spreadTime, float spreadLength) {
        const float minRatio = 0.3f;
        const float maxRatio = 1.0f;
        float calculateFunc(SoundObject so) {
            float alpha = 0.0f;
            if(so.Length > spreadTime) {
                alpha = 1.0f - Mathf.Clamp01(Mathf.InverseLerp(minRatio, maxRatio, so.CurrentTime / spreadTime));
            }
            else {
                alpha = 1.0f - Mathf.InverseLerp(minRatio, maxRatio, so.NormalizedTime);
            }

            float dist = Vector3.Distance(so.Position, UtilObjects.Instance.CamPos);
            float alphaOffset = Mathf.Clamp01(1.0f - dist / spreadLength);

            return alpha * alphaOffset;
        }
        switch(from) {
            case SoundFrom.None: return noneFromSoundObjectList.Select(t => calculateFunc(t)).ToArray();
            case SoundFrom.Player: return playerSoundObjectList.Select(t => calculateFunc(t)).ToArray();
            case SoundFrom.Monster: return monsterSoundObjectList.Select(t => calculateFunc(t)).ToArray();
            case SoundFrom.Item: return itemSoundObjectList.Select(t => calculateFunc(t)).ToArray();
            default: return new float[0];
        }
    }
    #endregion

    public float GetSpreadLength(SoundType type) {
        return LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH * Mathf.Clamp01(GetSfxClip(type).length / LevelLoader.STANDARD_RIM_RADIUS_SPREAD_TIME);
    }
    #endregion

    private IEnumerator FadeCoroutine(AudioSource source, float fadeTime, float startVolume, float endVolume, Action callback = null) {
        source.volume = startVolume;

        float fadeTimeChecker = 0.0f;
        while(fadeTimeChecker < fadeTime) {
            fadeTimeChecker += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, endVolume, fadeTimeChecker / fadeTime);

            yield return null;
        }

        source.volume = endVolume;

        callback?.Invoke();
    }

    private SoundObject GetSoundObject(SoundType type) {
        SoundObject so = null;

        if(soundObjectPool.Count > 0) {
            so = soundObjectPool[0];
            soundObjectPool.RemoveAt(0);

            // Pool�� ������Ʈ�� �ϳ��� ������ ���Ƿ� �ϳ��� �����ؼ� ����� ����
            if(soundObjectPool.Count <= 0) {
                SoundObject waitSO = new SoundObject();
                waitSO.Source.gameObject.SetActive(false);

                soundObjectPool.Add(waitSO);
            }
        }
        else {
            so = new SoundObject();
        }

        so.ChangeSoundType(type);

        return so;
    }

    private string GetBgmPath(SoundType type) => Path.Combine(BASIC_PATH_OF_BGM, type.ToString());
    private string GetSfxPath(SoundType type) => Path.Combine(BASIC_PATH_OF_SFX, type.ToString());
}

public class SoundObject {
    public AudioSource Source { get; private set; }
    public SoundManager.SoundType Type { get; private set; }
    public float CurrentTime { get { return Source.time; } }
    public float Length { get { return Source.clip.length; } }
    public float NormalizedTime { get { return CurrentTime / Length; } }

    public bool IsPlaying { get { return Source.isPlaying; } }
    public bool IsPaused { get; private set; }
    public float SpreadLength { get; private set; }

    public Vector3 Position {
        get => Source.transform.position;
        set {
            Source.transform.position = value;
        }
    }

    private float volume = 1.0f;
    public float Volume {
        get => Source.volume;
        set {
            Source.volume = value;
        }
    }



    public SoundObject() {
        if(Source == null) {
            GameObject go = new GameObject(nameof(SoundObject));
            go.transform.SetParent(SoundManager.Instance.transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1.0f;
            source.dopplerLevel = 0.2f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.0f;
            source.maxDistance = LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH;

            Source = source;
        }
    }

    #region Utility
    public void Play() {
        if(Source.clip == null) {
            Debug.LogWarning("Clip is NULL");

            return;
        }

        Source.gameObject.SetActive(true);

        IsPaused = false;
        Source.Play();
    }

    public void Stop() {
        Source.Stop();

        Source.gameObject.SetActive(false);
    }

    public void Pause() {
        IsPaused = true;
        Source.Pause();
    }

    public void UnPause() {
        IsPaused = false;
        Source.UnPause();
    }

    public void ChangeSoundType(SoundManager.SoundType type) {
        AudioClip clip = SoundManager.Instance.GetSfxClip(type); ;
        SpreadLength = LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH * clip.length / LevelLoader.STANDARD_RIM_RADIUS_SPREAD_TIME;

        Source.clip = clip;

        Type = type;
    }
    #endregion
}
