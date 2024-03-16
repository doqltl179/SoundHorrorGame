using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSelectMenuController : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    public CanvasGroup CanvasGroup { get { return canvasGroup; } }
    public float Alpha {
        get => canvasGroup.alpha;
        set => canvasGroup.alpha = value;
    }

    [Header("Element")]
    [SerializeField] private ButtonSelectMenuElement elementResource;
    [SerializeField] private Transform elementParent;

    private List<ButtonSelectMenuElement> elements = new List<ButtonSelectMenuElement>();



    private void Start() {
        elementResource.gameObject.SetActive(false);
    }

    #region Utility
    public void InitMenu(ButtonSelectMenuStruct[] structs) {
        if(structs == null || structs.Length <= 0) return;

        ButtonSelectMenuElement currentElement;
        int i = 0;
        for(; i < structs.Length; i++) {
            if(elements.Count >= i + 1) {
                currentElement = elements[i];
            }
            else {
                ButtonSelectMenuElement e = Instantiate(elementResource, elementParent);

                elements.Add(e);
                currentElement = e;
            }
            currentElement.gameObject.SetActive(true);

            currentElement.Init(structs[i]);
        }

        if(elements.Count > i) {
            for(; i < elements.Count; i++) {
                elements[i].gameObject.SetActive(false);
            }
        }
    }
    #endregion

    #region OnClick
    public void OnClickBack() {
        SoundManager.Instance.PlayOneShot(SoundManager.SoundType.ButtonClick);

        UtilObjects.Instance.SetActiveButtonSelectMenu(false);
    }
    #endregion
}

public class ButtonSelectMenuStruct {
    public string key;
    public Action action;
}