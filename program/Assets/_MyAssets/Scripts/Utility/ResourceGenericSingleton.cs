using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResourceGenericSingleton<T> : MonoBehaviour where T : MonoBehaviour {
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
                                foreach(var c in childObjs) {
                                    Destroy(c.gameObject);
                                }
                            }
                        }
                    }
                }

                string componentName = typeof(T).Name;
                GameObject resourceObj = ResourceLoader.GetResource<GameObject>(componentName);

                GameObject obj = Instantiate(resourceObj);
                obj.name = componentName;

                T component = obj.GetComponent<T>();
                if(component == null) {
                    Debug.LogError(string.Format("Component not found in Prefab. name: ", componentName));

                    return null;
                }

                instance = component;
            }
            return instance;
        }
    }
}
