using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemController : MonoBehaviour {
    [SerializeField] private SphereCollider collider;

    public Vector3 Pos { get { return transform.position; } }
    public float Radius { get { return collider.radius; } }

    private const float ItemSoundPlayTimeInterval = 10.0f;
    private float itemSoundPlayTimeChecker = 0.0f;



    private void Start() {
        itemSoundPlayTimeChecker = Random.Range(0.0f, ItemSoundPlayTimeInterval);
    }

    private void FixedUpdate() {
        itemSoundPlayTimeChecker += Time.deltaTime;
        if(itemSoundPlayTimeChecker >= ItemSoundPlayTimeInterval) {
            if(Vector3.Distance(Pos, UtilObjects.Instance.CamPos) < LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH) {
                List<Vector3> tempPath = LevelLoader.Instance.GetPath(
                    Pos,
                    UtilObjects.Instance.CamPos,
                    Radius,
                    1 << LayerMask.NameToLayer(MazeBlock.WallLayerName));
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

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag(PlayerController.TagName)) {
            LevelLoader.Instance.OnItemCollected?.Invoke(this);
        }
    }
}
