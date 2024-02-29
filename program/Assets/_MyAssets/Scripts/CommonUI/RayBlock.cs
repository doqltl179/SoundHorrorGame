using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RayBlock : MonoBehaviour {
    [SerializeField] private Image blockImage;
    [SerializeField] private CanvasGroup canvasGroup;
    public CanvasGroup CanvasGroup { get { return canvasGroup; } }
    public Color Color {
        get => blockImage.color;
        set => blockImage.color = value;
    }
    public float Alpha {
        get => canvasGroup.alpha;
        set => canvasGroup.alpha = value;
    }
}
