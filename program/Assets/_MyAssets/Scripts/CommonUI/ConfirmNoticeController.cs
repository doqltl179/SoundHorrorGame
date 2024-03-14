using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class ConfirmNoticeController : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    public CanvasGroup CanvasGroup { get { return canvasGroup; } }
    public float Alpha {
        get => canvasGroup.alpha;
        set => canvasGroup.alpha = value;
    }

    [Header("Message")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private LocalizeStringEvent messageStringEvent;

    [Header("Buttons")]
    [SerializeField] private Button[] buttons;
    [SerializeField] private TextMeshProUGUI[] buttonTexts;
    [SerializeField] private LocalizeStringEvent[] buttonStringEvents;



    #region Utility
    public void Init(string messageKey, string key1, Action action1, string key2 = null, Action action2 = null) {
        // Clear Action
        buttons[0].onClick.RemoveAllListeners();
        buttons[1].onClick.RemoveAllListeners();

        // Set Message
        messageStringEvent.StringReference.TableEntryReference = messageKey;

        // Set Action1
        buttonStringEvents[0].StringReference.TableEntryReference = key1;
        buttons[0].onClick.AddListener(() => {
            SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

            action1?.Invoke();
        });

        // Set Action2
        if(!string.IsNullOrEmpty(key2)) {
            buttons[1].gameObject.SetActive(true);

            buttonStringEvents[1].StringReference.TableEntryReference = key2;
            buttons[1].onClick.AddListener(() => {
                SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

                action2?.Invoke();
            });
        }
        else {
            buttons[1].gameObject.SetActive(false);
        }
    }
    #endregion
}
