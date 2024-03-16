using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Components;

public class MainController : MonoBehaviour {
    [SerializeField] private Canvas canvas = null;

    [Header("Menu")]
    [SerializeField] private LocalizeStringEvent firstMenuStringEvent;

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
        Application.targetFrameRate = 60;

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

        // 음성 감지 시작
        MicrophoneRecorder.Instance.gameObject.SetActive(UserSettings.UseMicBoolean);

        // BGM 재생
        SoundManager.Instance.PlayBGM(SoundManager.SoundType.Main, 3.0f, 0.65f);

        // Scene 명명
        SceneLoader.Instance.ChangeCurrentLoadedSceneImmediately(SceneLoader.SceneType.Main);

        // Cursor 세팅
        UtilObjects.Instance.SetActiveCursorImage(false);
    }

    //private void Update() {
    //    if(Input.GetMouseButtonDown(0)) {
    //        Ray ray = UtilObjects.Instance.Cam.ScreenPointToRay(Input.mousePosition);
    //        RaycastHit hit;
    //        if(Physics.Raycast(ray, out hit) && hit.transform.CompareTag(MazeBlock.TagName)) {
    //            SoundManager.Instance.PlayOnWorld(
    //                hit.point,
    //                SoundManager.SoundType.MouseClick,
    //                SoundManager.SoundFrom.None,
    //                Mathf.Clamp01(1.0f - hit.distance / LevelLoader.STANDARD_RIM_RADIUS_SPREAD_LENGTH));
    //        }
    //    }
    //}

    private void SetUIProperties() {
        int clearLevelMax = UserSettings.GetClearLevelMax();
        if(clearLevelMax <= 0) {
            firstMenuStringEvent.StringReference.TableEntryReference = "Start";
        }
        else if(UserSettings.GetEndingClearBoolean(true) || UserSettings.GetEndingClearBoolean(false)) {
            firstMenuStringEvent.StringReference.TableEntryReference = "SelectChapter";
        }
        else {
            firstMenuStringEvent.StringReference.TableEntryReference = "Continue";
        }
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
    public void OnKeyGuideClicked() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

        UtilObjects.Instance.SetActiveKeyGuide(true);
    }

    public void OnNewGameClicked() {
        if(UserSettings.GetEndingClearBoolean(true) || UserSettings.GetEndingClearBoolean(false)) {
            SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

            ButtonSelectMenuStruct[] structs = new ButtonSelectMenuStruct[GameLevelStruct.GameLevels.Length];
            for(int i = 0; i < structs.Length; i++) {
                int gameLevel = i;
                structs[i] = new ButtonSelectMenuStruct() {
                    key = "Chapter" + (i + 1).ToString().PadLeft(2, '0'),
                    action = () => {
                        UserSettings.GameLevel = gameLevel;

                        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.GameEnter);
                        SoundManager.Instance.StopBGM(SoundManager.SoundType.Main, 0.5f);

                        SceneLoader.Instance.LoadScene(SceneLoader.SceneType.Game);
                    }
                };
            }

            UtilObjects.Instance.SetActiveButtonSelectMenu(true, structs);
        }
        else {
            UserSettings.GameLevel = UserSettings.GetClearLevelMax() + 1;

            SoundManager.Instance.PlayOneShot(SoundManager.SoundType.GameEnter);
            SoundManager.Instance.StopBGM(SoundManager.SoundType.Main, 0.5f);

            SceneLoader.Instance.LoadScene(SceneLoader.SceneType.Game);
        }
    }

    public void OnSettingsClicked() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

        UtilObjects.Instance.SetActiveSettings(true);
    }

    public void OnExitClicked() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

        UtilObjects.Instance.InitConfirmNotice(
            "GoToMain",
            "No",
            () => {
                UtilObjects.Instance.SetActiveConfirmNotice(false);
                StartCoroutine(UtilObjects.Instance.SetActiveRayBlockAction(false));
            },
            "Yes",
            () => {
                Application.Quit();
            });
        UtilObjects.Instance.SetActiveConfirmNotice(true);
    }
    #endregion
}
