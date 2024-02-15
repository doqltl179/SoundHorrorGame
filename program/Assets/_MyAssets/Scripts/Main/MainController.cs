using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour {
    [SerializeField] private Canvas canvas = null;

    private IEnumerator mainCameraAnimationCoroutine = null;



    private void Start() {
        canvas.worldCamera = UtilObjects.Instance.Cam;
        canvas.planeDistance = 5.0f;

        // UI 세팅
        SetUIProperties();

        // Lobby 레벨 생성
        //LevelLoader.Instance.LoadLevel(10, 10, true);
        LevelLoader.Instance.LoadLevel(25, 25);

        if(mainCameraAnimationCoroutine != null) StopCoroutine(mainCameraAnimationCoroutine);
        mainCameraAnimationCoroutine = MainCameraAnimationCoroutine();
        StartCoroutine(mainCameraAnimationCoroutine);
    }

    private void Update() {
        if(Input.GetMouseButtonDown(0)) {
            Ray ray = UtilObjects.Instance.Cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit) && hit.transform.CompareTag(MazeBlock.TagName)) {
                SoundManager.Instance.PlayOnWorld(
                    hit.point, 
                    SoundManager.SoundType.MouseClick, 
                    Mathf.Clamp01(1.0f - hit.distance / LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH));
            }
        }
    }

    private void SetUIProperties() {

    }

    private IEnumerator MainCameraAnimationCoroutine() {
        Vector3 levelCenterPos = LevelLoader.Instance.GetCenterPos();
        Vector3 levelSidePos = LevelLoader.Instance.GetBlockPos((LevelLoader.Instance.LevelWidth - 1) * 0.5f, 0.0f);
        Vector3 cameraPos = new Vector3(levelCenterPos.x, PlayerController.PlayerHeight, levelSidePos.z);
        Vector3 cameraForward = (levelCenterPos - levelSidePos).normalized;
        UtilObjects.Instance.Cam.transform.position = cameraPos;
        UtilObjects.Instance.Cam.transform.forward = cameraForward;

        yield return null;

        mainCameraAnimationCoroutine = null;
    }

    #region OnClick
    public void OnContinueClicked() {

    }

    public void OnNewGameClicked() {

    }

    public void OnSettingsClicked() {

    }

    public void OnExitClicked() {

    }
    #endregion
}
