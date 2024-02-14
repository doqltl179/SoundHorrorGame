using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceLoader {
    /// <summary>
    /// Resource를 반환하는 것으로 instantiate가 된 게임 오브젝트를 반환하는 것이 아님.
    /// </summary>
    public static T GetResource<T>(string path) where T : UnityEngine.Object {
        T obj = Resources.Load<T>(path);
        if(obj == null) {
            Debug.LogError(string.Format("Object not found in resources folder. path: ", path));

            return null;
        }

        return obj;
    }
}
