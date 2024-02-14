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

    private Dictionary<SoundType, AudioClip> clips = new Dictionary<SoundType, AudioClip>();

    private List<SoundObject> soundObjectList = new List<SoundObject>();
    private List<SoundObject> soundObjectPool = new List<SoundObject>();

    public Action OnWorldSoundAdded;
    public Action OnWorldSoundRemoved;



    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    bool soundObjectRemoveChecker;
    private void FixedUpdate() {
        soundObjectRemoveChecker = false;
        for(int i = 0; i < soundObjectList.Count; i++) {
            if(!soundObjectList[i].Source.isPlaying) {
                soundObjectList[i].Stop();
                soundObjectPool.Add(soundObjectList[i]);

                soundObjectList.RemoveAt(i);
                i--;

                soundObjectRemoveChecker = true;
            }
        }

        if(soundObjectRemoveChecker) {
            OnWorldSoundRemoved?.Invoke();
        }
    }

    #region Utility
    public void PlaySoundOnWorld(Vector3 worldPos, SoundType type, float volumeOffset = 1.0f) {
        SoundObject so = GetSoundObject(type);
        so.Position = worldPos;

        so.Play();
        soundObjectList.Add(so);

        OnWorldSoundAdded?.Invoke();
    }

    #region Material Property Util Func
    public List<Vector4> GetSoundObjectPosList() {
        Vector4 vec3ToVec4(Vector3 v) => new Vector4(v.x, v.y, v.z);
        return soundObjectList.Select(t => vec3ToVec4(t.Source.transform.position)).ToList();
    }

    public List<float> GetSoudObjectRadiusList(float standardRadius) {
        return soundObjectList.Select(t => t.CurrentTime * standardRadius).ToList();
    }

    public List<float> GetSoundObjectAlphaList(float standardTime = 1.0f) {
        const float minRatio = 0.7f;
        const float maxRatio = 1.0f;
        return soundObjectList.Select(t => 
            t.Length > standardTime ?
            1.0f - Mathf.Clamp01(Mathf.InverseLerp(minRatio, maxRatio, t.CurrentTime / standardTime)) : 
            1.0f - Mathf.InverseLerp(minRatio, maxRatio, t.NormalizedTime)
            ).ToList();
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

        so.Source.clip = GetAudioClip(type);

        return so;
    }

    private AudioClip GetAudioClip(SoundType type) {
        AudioClip clip = null;
        if(!clips.TryGetValue(type, out clip)) {
            string path = GetSoundPath(type);
            clip = ResourceLoader.GetResource<AudioClip>(path);

            clips.Add(type, clip);
        }

        return clip;
    }

    private string GetSoundPath(SoundType type) {
        switch(type) {
            case SoundType.MouseClick: 
                return Path.Combine(BASIC_PATH_OF_SFX, type.ToString());

            default: 
                return string.Empty;
        }
    }

    private class SoundObject {
        public AudioSource Source { get; private set; }
        public float CurrentTime { get { return Source.time; } }
        public float Length { get { return Source.clip.length; } }
        public float NormalizedTime { get { return CurrentTime / Length;  } }

        private Vector3 position = Vector3.zero;
        public Vector3 Position {
            get => position;
            set {
                Source.transform.position = value;

                position = value;
            }
        }



        public SoundObject() {
            if(Source == null) {
                GameObject go = new GameObject(nameof(SoundObject));
                go.transform.SetParent(SoundManager.Instance.transform);

                AudioSource source = go.AddComponent<AudioSource>();

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
        #endregion
    }
}
