using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Teleport : MonoBehaviour {
    [SerializeField] private ParticleSystem potal;
    [SerializeField] private CapsuleCollider collider;

    public Vector3 Pos {
        get => transform.position;
        set => transform.position = value;
    }

    private IEnumerator teleportAnimationCoroutine = null;



    private void OnDestroy() {
        if(teleportAnimationCoroutine != null) {
            StopCoroutine(teleportAnimationCoroutine);
            teleportAnimationCoroutine = null;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if(other.CompareTag(PlayerController.TagName)) {
            if(teleportAnimationCoroutine == null) {
                teleportAnimationCoroutine = TeleportAnimationCoroutine();
                StartCoroutine(teleportAnimationCoroutine);
            }
        }
    }

    private IEnumerator TeleportAnimationCoroutine() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.Teleport, 0.85f);

        const float fadeTime = 0.2f;

        // 플레이어 정지
        PlayerController.Instance.IsPlaying = false;

        // Fade Out
        yield return StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(true, fadeTime));
        yield return null;

        // Set player to random pos
        const int zoom = 2;
        List<Vector2Int> emptyCoords = LevelLoader.Instance.GetAllCoordsNotExistMonsters(zoom);
        Vector2Int[] teleportCoords = LevelLoader.Instance.GetAllCoordsOfTeleports();
        if(emptyCoords.Count > 0) {
            Vector2Int randomZoomInCoord = emptyCoords[Random.Range(0, emptyCoords.Count)];
            Vector2Int randomCoord = LevelLoader.Instance.GetRandomCoordOnZoomInCoordArea(randomZoomInCoord, zoom);
            while(true) {
                if(teleportCoords.Where(t => t.x == randomCoord.x && t.y == randomCoord.y).Any()) {
                    randomCoord = LevelLoader.Instance.GetRandomCoordOnZoomInCoordArea(randomZoomInCoord, zoom);
                }
                else {
                    break;
                }
            }
            PlayerController.Instance.Pos = LevelLoader.Instance.GetBlockPos(randomCoord);
            if(PlayerController.Instance.PickupItem != null) {
                PlayerController.Instance.SetPickupItemTransformImmediately();
            }

            UtilObjects.Instance.CamPos = PlayerController.Instance.CamPos;
            UtilObjects.Instance.CamRotation = PlayerController.Instance.CamRotation;
        }

        yield return null;
        // 플레이어 시작
        PlayerController.Instance.IsPlaying = true;

        // Teleport Off
        collider.enabled = false;
        potal.gameObject.SetActive(false);

        // Fade In
        yield return StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(false, fadeTime));

        // Delay
        yield return new WaitForSeconds(30.0f);

        // Move Pos
        emptyCoords = LevelLoader.Instance.GetAllCoordsNotExistMonsters(zoom);
        if(emptyCoords.Count > 0) {
            Vector2Int randomZoomInCoord = emptyCoords[Random.Range(0, emptyCoords.Count)];
            Vector2Int randomCoord = LevelLoader.Instance.GetRandomCoordOnZoomInCoordArea(randomZoomInCoord, zoom);
            transform.position = LevelLoader.Instance.GetBlockPos(randomCoord);
        }

        // Teleport On
        potal.gameObject.SetActive(true);
        var renderer = potal.GetComponent<Renderer>();
        const string rendererColorPropertyName = "_Color";

        const float potalFadeTime = 2.0f;
        float timeChecker = 0.0f;
        while(timeChecker < potalFadeTime) {
            timeChecker += Time.deltaTime;
            renderer.material.SetColor(rendererColorPropertyName, Color.white * timeChecker / potalFadeTime * 1.6f);

            yield return null;
        }

        collider.enabled = true;

        teleportAnimationCoroutine = null;
    }
}
