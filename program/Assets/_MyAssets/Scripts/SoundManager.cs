using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class SoundManager : GenericSingleton<SoundManager> {
    public enum SoundType {
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

        // Item
        Crystal, 

        // Player
        PlayerWalk,

        // Etc
        MouseClick, //'Main'에서만 사용됨
        ButtonClick, 
        GameEnter, 
    }

    public enum SoundFrom {
        None,
        Player,
        Monster,
        Item
    }

    private readonly string BASIC_PATH_OF_SFX = "Audios/SFX";
    private readonly string BASIC_PATH_OF_BGM = "Audios/BGM";

    private Dictionary<SoundType, AudioClip> clipResources = new Dictionary<SoundType, AudioClip>();

    private List<SoundObject> noneFromSoundObjectList = new List<SoundObject>();
    private List<SoundObject> playerSoundObjectList = new List<SoundObject>();
    private List<SoundObject> monsterSoundObjectList = new List<SoundObject>();
    private List<SoundObject> itemSoundObjectList = new List<SoundObject>();
    private List<SoundObject> soundObjectPool = new List<SoundObject>(); //Pool

    private AudioSource oneShotSource = null;

    public Action<SoundObject, SoundFrom> OnWorldSoundAdded;
    public Action<SoundFrom> OnWorldSoundRemoved;

    private IEnumerator micDecibelCheckCoroutine = null;



    protected override void Awake() {
        base.Awake();

        UserSettings.OnMasterVolumeChanged += OnMasterVolumeChanged;
        UserSettings.OnUseMicChanged += OnUseMicChanged;
    }

    private void OnDestroy() {
        UserSettings.OnMasterVolumeChanged -= OnMasterVolumeChanged;
        UserSettings.OnUseMicChanged -= OnUseMicChanged;
    }

    private void Start() {
        if(UserSettings.UseMicBoolean) {
            micDecibelCheckCoroutine = MicDecibelCheckCoroutine();
            StartCoroutine(micDecibelCheckCoroutine);
        }
    }

    private IEnumerator MicDecibelCheckCoroutine() {
        const float timeInterval = 0.15f;
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
                    case 0: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty01s, SoundFrom.Player, 1.0f); break;
                    case 1: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty02s, SoundFrom.Player, 1.0f); break;
                    case 2: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty03s, SoundFrom.Player, 1.0f); break;
                    case 3: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty04s, SoundFrom.Player, 1.0f); break;
                    case 4: PlayOnWorld(UtilObjects.Instance.CamPos, SoundType.Empty05s, SoundFrom.Player, 1.0f); break;
                }
            }
        }

        micDecibelCheckCoroutine = null;
    }

    private void FixedUpdate() {
        if(noneFromSoundObjectList.Count > 0) CheckSoundObjct(ref noneFromSoundObjectList, SoundFrom.None);
        if(playerSoundObjectList.Count > 0) CheckSoundObjct(ref playerSoundObjectList, SoundFrom.Player);
        if(monsterSoundObjectList.Count > 0) CheckSoundObjct(ref monsterSoundObjectList, SoundFrom.Monster);
        if(itemSoundObjectList.Count > 0) CheckSoundObjct(ref itemSoundObjectList, SoundFrom.Item);
    }

    private void CheckSoundObjct(ref List<SoundObject> list, SoundFrom from) {
        bool listChanged = false;
        for(int i = 0; i < list.Count; i++) {
            if(!list[i].Source.isPlaying) {
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

    private IEnumerator StopAsync(SoundObject so) {
        so.Stop();
        yield return null;

        soundObjectPool.Add(so);
    }

    #region Action
    private void OnMasterVolumeChanged(float value) {
        float ratio = UserSettings.CalculateMasterVolumeRatio(value);
        AudioListener.volume = ratio;
    }

    private void OnUseMicChanged(bool value) {
        if(micDecibelCheckCoroutine != null) StopCoroutine(micDecibelCheckCoroutine);

        if(value) {
            micDecibelCheckCoroutine = MicDecibelCheckCoroutine();
            StartCoroutine(micDecibelCheckCoroutine);
        }
    }
    #endregion

    #region Utility
    public void PlayOneShot(SoundType type, float volumeOffset = 1.0f) {
        if(oneShotSource == null) {
            GameObject go = new GameObject("OneShotSource");
            go.transform.SetParent(transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.reverbZoneMix = 0.0f;

            oneShotSource = source;
        }

        oneShotSource.PlayOneShot(GetAudioClip(type), volumeOffset);
    }

    public void PlayOnWorld(Vector3 worldPos, SoundType type, SoundFrom from, float volumeOffset = 1.0f) {
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
                so.Volume = 1.0f * volumeOffset;
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

    public AudioClip GetAudioClip(SoundType type) {
        AudioClip clip = null;
        if(!clipResources.TryGetValue(type, out clip)) {
            string path = GetSoundPath(type);
            clip = ResourceLoader.GetResource<AudioClip>(path);

            clipResources.Add(type, clip);
        }

        return clip;
    }

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
            float radius = so.CurrentTime / spreadTime * spreadLength;

            float dist = Vector3.Distance(so.Position, UtilObjects.Instance.CamPos);
            float radiusOffset = Mathf.Clamp01(1.0f - dist / spreadLength);

            return radius * radiusOffset;
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
    #endregion

    private SoundObject GetSoundObject(SoundType type) {
        SoundObject so = null;

        if(soundObjectPool.Count > 0) {
            so = soundObjectPool[0];
            soundObjectPool.RemoveAt(0);

            // Pool에 오브젝트가 하나도 없으면 임의로 하나를 생성해서 대기해 놓음
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

    private string GetSoundPath(SoundType type) => Path.Combine(BASIC_PATH_OF_SFX, type.ToString());
}

public class SoundObject {
    public AudioSource Source { get; private set; }
    public SoundManager.SoundType Type { get; private set; }
    public float CurrentTime { get { return Source.time; } }
    public float Length { get { return Source.clip.length; } }
    public float NormalizedTime { get { return CurrentTime / Length; } }

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

        Source.Play();
    }

    public void Stop() {
        Source.Stop();

        Source.gameObject.SetActive(false);
    }

    public void ChangeSoundType(SoundManager.SoundType type) {
        Source.clip = SoundManager.Instance.GetAudioClip(type);

        Type = type;
    }
    #endregion
}
