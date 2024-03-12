using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToyHammer : PickupItem {
    [SerializeField] private MeshCollider[] colliders;
    [SerializeField] private Animator animator;

    public override bool IsPickup {
        get => isPickup;
        set {
            foreach(MeshCollider col in colliders) {
                col.enabled = !value;
            }
            rigidbody.useGravity = !value;

            isPickup = value;
        }
    }



    private void OnCollisionEnter(Collision collision) {
        if(!IsPlaying || !isPickup) return;


    }

    #region Utility
    public override void Play() {
        IsPlaying = true;
    }

    public override void Stop() {
        IsPlaying = false;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }
    #endregion
}
