using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Complete;
using DG.Tweening; // 添加 DOTween 命名空间

public class SceneCanvas : MonoBehaviour
{
    [SerializeField] private Button openPkgBtn;   // 打开背包按钮
    [SerializeField] private Button closePkgBtn;   // 关闭背包按钮
    [SerializeField] private Button clearPkg;     // 清空背包按钮

    [SerializeField] private GameObject ScrollViewRoot;  // 背包面板根物体
    
    [SerializeField] private float animationDuration = 0.5f;  // 动画持续时间
    [SerializeField] private Vector2 hidePosition = new Vector2(300, 0);  // 隐藏时的位置偏移
    
    private RectTransform scrollViewRectTransform;
    private Vector2 showPosition;  // 显示时的位置
    
    void Start()
    {
        // 初始化引用和事件
        scrollViewRectTransform = ScrollViewRoot.GetComponent<RectTransform>();
        showPosition = scrollViewRectTransform.anchoredPosition;
        
        // 初始状态为隐藏
        scrollViewRectTransform.anchoredPosition = showPosition + hidePosition;
        ScrollViewRoot.SetActive(false);
        
        // 添加按钮事件监听
        if (openPkgBtn != null)
            openPkgBtn.onClick.AddListener(OpenBackpack);
            
        if (closePkgBtn != null)
            closePkgBtn.onClick.AddListener(CloseBackpack);
            
        if (clearPkg != null)
            clearPkg.onClick.AddListener(ClearBackpack);
    }

    private void OnDestroy()
    {
        // 当组件被销毁时，停止相关的所有 DOTween 动画
        if (scrollViewRectTransform != null)
        {
            scrollViewRectTransform.DOKill();
        }
    }
    
    // 打开背包
    public void OpenBackpack()
    {
        // 确保先停止之前的动画
        scrollViewRectTransform.DOKill();
        
        ScrollViewRoot.SetActive(true);
        
        // 从右侧滑入
        scrollViewRectTransform.anchoredPosition = showPosition + hidePosition;
        scrollViewRectTransform.DOAnchorPos(showPosition, animationDuration)
            .SetEase(Ease.OutBack) // 添加一点回弹效果
            .OnComplete(() => {
                // 动画完成后的回调
            });
    }
    
    // 关闭背包
    public void CloseBackpack()
    {
        // 确保先停止之前的动画
        scrollViewRectTransform.DOKill();
        
        // 滑出到右侧
        scrollViewRectTransform.DOAnchorPos(showPosition + hidePosition, animationDuration)
            .SetEase(Ease.InBack) // 添加一点回弹效果
            .OnComplete(() => {
                ScrollViewRoot.SetActive(false);
            });
    }
    
    // 清空背包数据
    public void ClearBackpack()
    {
        // 使用PlayerPrefs删除背包数据
        PlayerPrefs.DeleteKey("CollectedProps");
        PlayerPrefs.Save();
        
        // 通知PropManager刷新数据（如果存在）
        if (PropManager.Instance != null)
        {
            // 调用PropManager重新加载数据
            PropManager.Instance.ReloadProps();
            
            // 显示清空成功提示
            Debug.Log("背包数据已清空");
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
