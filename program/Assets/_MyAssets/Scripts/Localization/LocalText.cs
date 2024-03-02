using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalText : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI text;

    [SerializeField] private Localization.Key key = Localization.Key.code;
    public Localization.Key Key {
        get => key;
        set {
            if(key != value) {
                key = value;

                text.text = Localization.GetText(Localization.CurrentLocal, value);
            }
        }
    }



    private void Reset() {
        if(text == null) text = GetComponent<TextMeshProUGUI>();
    }

    private void Awake() {
        Localization.OnLocalChanged += OnLocalChanged;
    }

    private void OnDestroy() {
        Localization.OnLocalChanged -= OnLocalChanged;
    }

    private void Start() {
        text.text = Localization.GetText(Localization.CurrentLocal, key);
    }

    #region Action
    private void OnLocalChanged(Localization.Local local) {
        text.text = Localization.GetText(local, key);
    }
    #endregion
}
