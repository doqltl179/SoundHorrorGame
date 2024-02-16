using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour {
    [SerializeField] private Canvas canvas = null;

    private IEnumerator mainCameraAnimationCoroutine = null;



    List<Vector3> testPath;
    private void OnDrawGizmos() {
        if(testPath == null || testPath.Count < 2)
            return;

        // Gizmos 색상 설정
        Gizmos.color = Color.green;

        // points 배열에 있는 모든 좌표들을 순서대로 잇는 선을 그립니다.
        for(int i = 0; i < testPath.Count - 1; i++) {
            Gizmos.DrawLine(testPath[i], testPath[i + 1]);
        }
    }

    private IEnumerator Start() {
        canvas.worldCamera = UtilObjects.Instance.Cam;
        canvas.planeDistance = 5.0f;

        // UI 세팅
        SetUIProperties();

        // Lobby 레벨 생성
        LevelLoader.Instance.LoadLevel(25, 25, false);
        yield return null;
        Vector3 testStartPos = LevelLoader.Instance.GetBlockPos(0, 0);
        Vector3 testEndPos = LevelLoader.Instance.GetBlockPos(24, 24);
        testPath = LevelLoader.Instance.GetPath(testStartPos, testEndPos, PlayerController.Radius);
        Debug.Log(LevelLoader.Instance.GetPathDistance(testPath));

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
