using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour {
    private static T instance = null;
    public static T Instance {
        get {
            if(instance == null) {
                T component = FindObjectOfType<T>();
                if(component == null) {
                    string componentName = typeof(T).Name;

                    GameObject go = new GameObject(componentName);
                    component = go.AddComponent<T>();
                }

                instance = component;
            }
            return instance;
        }
    }

    protected virtual void Awake() {
        if(Instance != null) DontDestroyOnLoad(gameObject);
        else Destroy(gameObject);
    }
}
