using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToyHammerTrigger : MonoBehaviour {



    private void OnTriggerEnter(Collider other) {
        if(other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(MonsterController.TagName)) {
            Debug.Log($"Trigger Hammer Hit! tag: {other.attachedRigidbody.tag}");

            MonsterController mc = other.attachedRigidbody.GetComponent<MonsterController>();
            Vector3 dir = ((mc.Pos + Vector3.up * PlayerController.PlayerHeight * 2) - PlayerController.Instance.Pos).normalized;
            mc.ThrowArray(dir);
        }
    }
}
