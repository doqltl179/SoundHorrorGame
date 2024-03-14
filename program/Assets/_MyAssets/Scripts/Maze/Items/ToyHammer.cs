using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToyHammer : PickupItem {
    [SerializeField] private CapsuleCollider[] colliders;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    public override bool IsPickup {
        get => isPickup;
        set {
            foreach(CapsuleCollider col in colliders) {
                col.enabled = !value;
                col.isTrigger = value;
            }
            rigidbody.useGravity = !value;
            animator.enabled = value;

            if(value) {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            isPickup = value;
        }
    }



    protected override void Start() {
        // Tag 및 Layer 설정을 하지 않음
    }

    #region Utility
    public override void Play() {
        animator.SetBool("Hit", true);

        IsPlaying = true;
    }

    public override void Stop() {
        IsPlaying = false;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        animator.SetBool("Hit", false);
    }
    #endregion

    #region Animation Event
    public void PlaySound_Swing() => audioSource.Play();

    public void StopSound_Swing() => audioSource.Stop();
    #endregion
}
