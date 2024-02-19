using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour {
    [SerializeField] private Canvas canvas = null;

    [Header("Camera Animation Properties")]
    [SerializeField, Range(0.1f, 10.0f)] private float cameraRotateSpeed = 1.0f;
    [SerializeField, Range(0.0f, 180.0f)] private float cameraRotateAngle = 120.0f;


    private IEnumerator mainCameraAnimationCoroutine = null;




    private void OnDestroy() {
        if(mainCameraAnimationCoroutine != null) {
            StopCoroutine(mainCameraAnimationCoroutine);
            mainCameraAnimationCoroutine = null;
        }
    }

    private void Start() {
        canvas.worldCamera = UtilObjects.Instance.Cam;
        canvas.planeDistance = 5.0f;

        // UI 세팅
        SetUIProperties();

        // Lobby 레벨 생성
        LevelLoader.Instance.ResetLevel();
        LevelLoader.Instance.LoadLevel(5, 5, true);

        // Lobby 카메라 애니메이션
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
        UtilObjects.Instance.CamPos = cameraPos;
        UtilObjects.Instance.CamForward = cameraForward;

        Vector3 forwardMin = Quaternion.AngleAxis(cameraRotateAngle * 0.5f, Vector3.up) * cameraForward;
        Vector3 forwardMax = Quaternion.AngleAxis(cameraRotateAngle * -0.5f, Vector3.up) * cameraForward;
        float angleRatio = 0.5f; //cameraForward를 angle ratio 0.5로 설정
        while(true) {
            angleRatio += Time.deltaTime * cameraRotateSpeed;
            UtilObjects.Instance.CamForward = Vector3.Lerp(forwardMin, forwardMax, (Mathf.Sin(angleRatio) + 1) * 0.5f);

            yield return null;
        }

        mainCameraAnimationCoroutine = null;
    }

    #region OnClick
    public void OnContinueClicked() {

    }

    public void OnNewGameClicked() {
        SceneLoader.Instance.LoadScene(SceneLoader.SceneType.Game);
    }

    public void OnSettingsClicked() {

    }

    public void OnExitClicked() {

    }
    #endregion
}
