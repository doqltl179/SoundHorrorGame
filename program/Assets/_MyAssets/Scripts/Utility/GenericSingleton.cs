using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour {
    private static Dictionary<string, T> instances = new Dictionary<string, T>();
    public static T Instance {
        get {
            T inst = null;
            string componentName = typeof(T).Name;
            if(!instances.TryGetValue(componentName, out inst)) {
                T[] temps = FindObjectsOfType<T>();
                foreach(T temp in temps) {
                    Destroy(temp.gameObject);
                }

                GameObject go = new GameObject(componentName);
                inst = go.AddComponent<T>();

                instances.Add(componentName, inst);

                DontDestroyOnLoad(go);
            }

            return inst;
        }
    }
}
