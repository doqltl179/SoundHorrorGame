using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KeyGuideController : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    public CanvasGroup CanvasGroup { get { return canvasGroup; } }
    public float Alpha {
        get => canvasGroup.alpha;
        set => canvasGroup.alpha = value;
    }

    [Header("Key Text")]
    [SerializeField] private TextMeshProUGUI keyTextForward;
    [SerializeField] private TextMeshProUGUI keyTextLeft;
    [SerializeField] private TextMeshProUGUI keyTextBack;
    [SerializeField] private TextMeshProUGUI keyTextRight;

    [SerializeField] private TextMeshProUGUI keyTextRun;
    [SerializeField] private TextMeshProUGUI keyTextCrouch;

    [SerializeField] private TextMeshProUGUI keyTextItemInteract;



    private void OnEnable() {
        keyTextForward.text = PlayerController.key_moveF.ToString();
        keyTextLeft.text = PlayerController.key_moveL.ToString();
        keyTextBack.text = PlayerController.key_moveB.ToString();
        keyTextRight.text = PlayerController.key_moveR.ToString();

        keyTextRun.text = PlayerController.key_run.ToString().Replace("Left", "L ");
        keyTextCrouch.text = PlayerController.key_crouch.ToString().Replace("Left", "L ").Replace("Control", "Ctrl");

        keyTextItemInteract.text = ItemController.key_interact.ToString();
    }

    #region Button OnClicked
    public void OnClickedBack() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

        UtilObjects.Instance.SetActiveKeyGuide(false);
    }
    #endregion
}
