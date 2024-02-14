using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceLoader {
    /// <summary>
    /// Resource�� ��ȯ�ϴ� ������ instantiate�� �� ���� ������Ʈ�� ��ȯ�ϴ� ���� �ƴ�.
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
