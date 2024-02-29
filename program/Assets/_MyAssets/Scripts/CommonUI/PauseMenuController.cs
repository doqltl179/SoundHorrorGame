using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuController : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    public CanvasGroup CanvasGroup { get { return canvasGroup; } }



    #region Action
    public void OnClickContinue() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

        UtilObjects.Instance.SetActivePauseMenu(false);
    }

    public void OnClickSettings() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

        UtilObjects.Instance.SetActiveSettings(true);
    }

    public void OnClickExit() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);


    }
    #endregion
}
