using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SoundManager : GenericSingleton<SoundManager> {
    public enum SoundType {
        MouseClick, //'Main'에서만 사용됨


    }

    private readonly string BASIC_PATH_OF_SFX = "Audios/SFX";
    private readonly string BASIC_PATH_OF_BGM = "Audios/BGM";

    private Dictionary<SoundType, AudioClip> clipResources = new Dictionary<SoundType, AudioClip>();

    private List<SoundObject> soundObjectList = new List<SoundObject>();
    private List<SoundObject> soundObjectPool = new List<SoundObject>();

    public Action<SoundObject> OnWorldSoundAdded;
    public Action OnWorldSoundRemoved;



    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    bool soundObjectRemovedChecker;
    private void FixedUpdate() {
        soundObjectRemovedChecker = false;
        for(int i = 0; i < soundObjectList.Count; i++) {
            if(!soundObjectList[i].Source.isPlaying) {
                StartCoroutine(StopSoundObject(i)); 
                i--;

                soundObjectRemovedChecker = true;
            }
        }

        if(soundObjectRemovedChecker) {
            OnWorldSoundRemoved?.Invoke();
        }
    }

    /// <summary>
    /// <br/> 사운드가 끝나자마자 Stop을 했을 때에 '지지직'하는 사운드가 끊기는 듯한 소리가 들리게 되므로
    /// <br/> 한 프레임 텀을 줘서 Stop을 실행
    /// </summary>
    private IEnumerator StopSoundObject(int index) {
        SoundObject so = soundObjectList[index];
        soundObjectList.RemoveAt(index);

        yield return null;

        so.Stop();
        soundObjectPool.Add(so);
    }

    #region Utility
    public void PlayOnWorld(Vector3 worldPos, SoundType type, float volumeOffset = 1.0f) {
        SoundObject so = GetSoundObject(type);
        so.Position = worldPos;
        so.Volume = 1.0f * volumeOffset;

        so.Play();
        soundObjectList.Add(so);

        OnWorldSoundAdded?.Invoke(so);
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
    public Vector4[] GetSoundObjectPosArray() {
        Vector4 vec3ToVec4(Vector3 v) => new Vector4(v.x, v.y, v.z);
        return soundObjectList.Select(t => vec3ToVec4(t.Source.transform.position)).ToArray();
    }

    public float[] GetSoundObjectRadiusArray(float spreadTime, float spreadLength) {
        float calculateFunc(SoundObject so) {
            float radius = so.CurrentTime / spreadTime * spreadLength;

            float dist = Vector3.Distance(so.Position, UtilObjects.Instance.CamPos);
            float radiusOffset = Mathf.Clamp01(1.0f - dist / spreadLength);

            return radius * radiusOffset;
        }
        return soundObjectList.Select(t => calculateFunc(t)).ToArray();
    }

    public float[] GetSoundObjectAlphaArray(float spreadTime, float spreadLength) {
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

        return soundObjectList.Select(t => calculateFunc(t)).ToArray();
    }
    #endregion
    #endregion

    private SoundObject GetSoundObject(SoundType type) {
        SoundObject so = null;

        if(soundObjectPool.Count > 0) {
            so = soundObjectPool[0];
            soundObjectPool.RemoveAt(0);
        }
        else {
            so = new SoundObject();
        }

        so.ChangeSoundType(type);

        return so;
    }

    private string GetSoundPath(SoundType type) {
        switch(type) {
            case SoundType.MouseClick: 
                return Path.Combine(BASIC_PATH_OF_SFX, type.ToString());

            default: 
                return string.Empty;
        }
    }
}

public class SoundObject {
    public AudioSource Source { get; private set; }
    public SoundManager.SoundType Type { get; private set; }
    public float CurrentTime { get { return Source.time; } }
    public float Length { get { return Source.clip.length; } }
    public float NormalizedTime { get { return CurrentTime / Length; } }

    private Vector3 position = Vector3.zero;
    public Vector3 Position {
        get => position;
        set {
            Source.transform.position = value;

            position = value;
        }
    }

    private float volume = 1.0f;
    public float Volume {
        get => volume;
        set {
            Source.volume = value;

            volume = value;
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
