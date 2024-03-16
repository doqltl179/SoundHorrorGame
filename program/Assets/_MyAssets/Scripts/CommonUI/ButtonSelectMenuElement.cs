using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class ButtonSelectMenuElement : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private LocalizeStringEvent stringEvent;



    #region Utility
    public void Init(ButtonSelectMenuStruct elementStruct) {
        button.onClick.RemoveAllListeners();

        if(string.IsNullOrEmpty(elementStruct.key)) stringEvent.gameObject.SetActive(false);
        else {
            stringEvent.StringReference.TableEntryReference = elementStruct.key;
            stringEvent.gameObject.SetActive(true);
        }
        button.onClick.AddListener(() => {
            SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

            elementStruct.action?.Invoke();
        });
    }
    #endregion
}
