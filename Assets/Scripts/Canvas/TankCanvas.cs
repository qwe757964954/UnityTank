using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class TankCanvas : MonoBehaviour
{
    [SerializeField] private InputField inputField; // Reference to the InputField
    [SerializeField] private Button submitButton;   // Reference to the Button
    [SerializeField] private RawImage tweenImage;   // Reference to the Button

    void Start()
    {
        // Add listener to button click
        if (submitButton != null && inputField != null)
        {
            submitButton.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("InputField or Button is not assigned in the Inspector!");
        }
        
        if (tweenImage != null)
        {
            // 将tweenImage移动到(0, 0, 0)
              PlayTween(new Vector3(500, 500, 0), 1f, 2f);
        }
        else
        {
            Debug.LogError("TweenImage未在Inspector中赋值！");
        }
    }
    // 调用此方法实现先移动再无限旋转
        public void PlayTween(Vector3 targetPos, float moveDuration = 1f, float rotateDuration = 2f)
        {
            // 先移动
            tweenImage.rectTransform
                .DOMove(targetPos, moveDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 移动完成后无限旋转
                    tweenImage.rectTransform
                        .DORotate(new Vector3(0, 0, 360), rotateDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1, LoopType.Restart);
                });
        }
    void OnButtonClick()
    {
        // Get and print the text from the InputField
        if (inputField != null)
        {
            string inputText = inputField.text;
            Debug.Log("Input Text: " + inputText);
        }
    }
}