using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour {
    private static T instance = null;
    public static T Instance {
        get {
            if(instance == null || instance.gameObject == null) {
                for(int i = 0; i < SceneManager.sceneCount; i++) {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if(scene.isLoaded) {
                        GameObject[] rootObjects = scene.GetRootGameObjects();
                        foreach(var rootObj in rootObjects) {
                            T[] childObjs = rootObj.GetComponentsInChildren<T>(true);
                            if(childObjs.Length > 0) {
                                foreach(var obj in childObjs) {
                                    Destroy(obj.gameObject);
                                }
                            }
                        }
                    }
                }

                string componentName = typeof(T).Name;

                GameObject go = new GameObject(componentName);
                T component = go.AddComponent<T>();

                instance = component;
            }
            return instance;
        }
    }
}
