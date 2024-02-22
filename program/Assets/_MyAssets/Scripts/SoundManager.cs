using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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

        // Item
        Crystal, 

        // Player
        PlayerWalk,

        // Etc
        MouseClick, //'Main'에서만 사용됨
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

    public Action<SoundObject, SoundFrom> OnWorldSoundAdded;
    public Action<SoundFrom> OnWorldSoundRemoved;



    protected override void Awake() {
        base.Awake();
    }

    private void FixedUpdate() {
        if(noneFromSoundObjectList.Count > 0) {
            bool listChanged = false;
            for(int i = 0; i < noneFromSoundObjectList.Count; i++) {
                if(!noneFromSoundObjectList[i].Source.isPlaying) {
                    SoundObject so = noneFromSoundObjectList[i];
                    noneFromSoundObjectList.RemoveAt(i);

                    //so.Stop();
                    //soundObjectPool.Add(so);
                    StartCoroutine(StopAsync(so));

                    i--;
                    listChanged = true;
                }
            }
            if(listChanged) OnWorldSoundRemoved?.Invoke(SoundFrom.None);
        }
        if(playerSoundObjectList.Count > 0) {
            bool listChanged = false;
            for(int i = 0; i < playerSoundObjectList.Count; i++) {
                if(!playerSoundObjectList[i].Source.isPlaying) {
                    SoundObject so = playerSoundObjectList[i];
                    playerSoundObjectList.RemoveAt(i);

                    //so.Stop();
                    //soundObjectPool.Add(so);
                    StartCoroutine(StopAsync(so));

                    i--;
                    listChanged = true;
                }
            }
            if(listChanged) OnWorldSoundRemoved?.Invoke(SoundFrom.Player);
        }
        if(monsterSoundObjectList.Count > 0) {
            bool listChanged = false;
            for(int i = 0; i < monsterSoundObjectList.Count; i++) {
                if(!monsterSoundObjectList[i].Source.isPlaying) {
                    SoundObject so = monsterSoundObjectList[i];
                    monsterSoundObjectList.RemoveAt(i);

                    //so.Stop();
                    //soundObjectPool.Add(so);
                    StartCoroutine(StopAsync(so));

                    i--;
                    listChanged = true;
                }
            }
            if(listChanged) OnWorldSoundRemoved?.Invoke(SoundFrom.Monster);
        }
        if(itemSoundObjectList.Count > 0) {
            bool listChanged = false;
            for(int i = 0; i < itemSoundObjectList.Count; i++) {
                if(!itemSoundObjectList[i].Source.isPlaying) {
                    SoundObject so = itemSoundObjectList[i];
                    itemSoundObjectList.RemoveAt(i);

                    //so.Stop();
                    //soundObjectPool.Add(so);
                    StartCoroutine(StopAsync(so));

                    i--;
                    listChanged = true;
                }
            }
            if(listChanged) OnWorldSoundRemoved?.Invoke(SoundFrom.Item);
        }
    }

    private IEnumerator StopAsync(SoundObject so) {
        so.Stop();
        yield return null;

        soundObjectPool.Add(so);
    }

    #region Utility
    public void PlayOnWorld(Vector3 worldPos, SoundType type, SoundFrom from, float volumeOffset = 1.0f) {
        SoundObject so = GetSoundObject(type);
        so.Position = worldPos;
        so.Volume = 1.0f * volumeOffset;

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
