using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemController : MonoBehaviour {
    [SerializeField] private SphereCollider collider;

    [Header("Pickaxe")]
    [SerializeField] private GameObject pickaxe;
    [SerializeField] private int collectingCount = 5;
    [SerializeField] private float collectingAnimationTime = 3.0f;
    private const float PickaxeHeight = 0.5f;
    public int CollectingCount { get; private set; } = 0;

    public bool PlayerEnter { get; private set; } = false;

    public Vector3 Pos { get { return transform.position; } }
    public float Radius { get { return collider.radius * transform.localScale.x; } }

    public static KeyCode key_interact = KeyCode.E;

    private const float ItemSoundPlayTimeInterval = 10.0f;
    private float itemSoundPlayTimeChecker = 0.0f;

    private IEnumerator pickaxeAnimationCoroutine = null;



    private void Start() {
        itemSoundPlayTimeChecker = Random.Range(0.0f, ItemSoundPlayTimeInterval);
    }

    private void Update() {
        if(PlayerEnter) return;

        itemSoundPlayTimeChecker += Time.deltaTime;
        if(itemSoundPlayTimeChecker >= ItemSoundPlayTimeInterval) {
            if(Vector3.Distance(Pos, UtilObjects.Instance.CamPos) < LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH) {
                List<Vector3> tempPath = LevelLoader.Instance.GetPath(Pos, UtilObjects.Instance.CamPos, Radius);
                float dist = LevelLoader.Instance.GetPathDistance(tempPath);
                SoundManager.Instance.PlayOnWorld(
                    Pos,
                    SoundManager.SoundType.Crystal, 
                    SoundManager.SoundFrom.Item,
                    1.0f - dist / LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH);
            }

            itemSoundPlayTimeChecker -= ItemSoundPlayTimeInterval;
        }
    }

    private IEnumerator PickaxeAnimationCoroutine() {
        Vector3 itemToPlayer = (PlayerController.Instance.Pos - Pos).normalized;
        Vector3 itemLeft = Quaternion.LookRotation(itemToPlayer, Vector3.up) * Vector3.left;
        Vector3 pickaxePos = Pos + itemLeft * PickaxeHeight;
        float pickaxePosY = PickaxeHeight * 0.65f;
        pickaxe.transform.position = new Vector3(pickaxePos.x, pickaxePosY, pickaxePos.z);
        pickaxe.transform.LookAt(new Vector3(Pos.x, pickaxePosY, Pos.z), Vector3.up);

        pickaxe.SetActive(false);

        Quaternion animationStartAngle = pickaxe.transform.localRotation * Quaternion.Euler(-20, 0, 0);
        Quaternion animationEndAngle = pickaxe.transform.localRotation * Quaternion.Euler(40, 0, 0);
        itemSoundPlayTimeChecker = 0.0f;
        float timeRatio = 0.0f;
        float animationRatio = 0.0f;
        WaitForSeconds delay = new WaitForSeconds(0.2f);
        while(CollectingCount < collectingCount) {
            if(Input.GetKey(KeyCode.E)) {
                if(!pickaxe.activeSelf) pickaxe.SetActive(true);

                itemSoundPlayTimeChecker += Time.deltaTime;

                timeRatio = itemSoundPlayTimeChecker / collectingAnimationTime;
                animationRatio = Mathf.Pow(timeRatio, 8);
                pickaxe.transform.localRotation = Quaternion.Lerp(animationStartAngle, animationEndAngle, animationRatio);

                if(timeRatio >= 1.0f) {
                    CollectingCount++;

                    SoundManager.SoundType playType = SoundManager.SoundType.None;
                    switch(CollectingCount) {
                        case 1: playType = SoundManager.SoundType.Mining01; break;
                        case 2: playType = SoundManager.SoundType.Mining02; break;
                        case 3: playType = SoundManager.SoundType.Mining03; break;
                        case 4: playType = SoundManager.SoundType.Mining04; break;
                        case 5: playType = SoundManager.SoundType.MiningEnd; break;
                    }
                    SoundManager.Instance.PlayOnWorld(Pos, playType, SoundManager.SoundFrom.Player);

                    yield return delay;

                    itemSoundPlayTimeChecker = 0.0f;
                }
            }

            yield return null;
        }

        pickaxe.SetActive(false);

        LevelLoader.Instance.CollectItem(this);

        pickaxeAnimationCoroutine = null;
    }

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag(PlayerController.TagName)) {
            //LevelLoader.Instance.OnItemCollected?.Invoke(this);
            PlayerEnter = true;

            if(pickaxeAnimationCoroutine != null) StopCoroutine(pickaxeAnimationCoroutine);
            pickaxeAnimationCoroutine = PickaxeAnimationCoroutine();
            StartCoroutine(pickaxeAnimationCoroutine);
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.CompareTag(PlayerController.TagName)) {
            PlayerEnter = false;

            if(pickaxeAnimationCoroutine != null) {
                StopCoroutine(pickaxeAnimationCoroutine);
                pickaxeAnimationCoroutine = null;

                pickaxe.SetActive(false);
            }
        }
    }

    #region Utility
    public void SetMaterial(Material mat) {
        MeshRenderer[] renderers = transform.GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer mr in renderers) {
            mr.material = mat;
        }
    }
    #endregion
}
