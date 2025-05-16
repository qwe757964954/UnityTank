using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Complete;

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
    
    // 打开背包
    public void OpenBackpack()
    {
        ScrollViewRoot.SetActive(true);
        
        // 从右侧滑入
        StartCoroutine(AnimateScrollView(showPosition + hidePosition, showPosition));
    }
    
    // 关闭背包
    public void CloseBackpack()
    {
        // 滑出到右侧
        StartCoroutine(AnimateScrollView(showPosition, showPosition + hidePosition, () => {
            ScrollViewRoot.SetActive(false);
        }));
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
    
    // 滑动动画协程
    private IEnumerator AnimateScrollView(Vector2 startPos, Vector2 endPos, System.Action onComplete = null)
    {
        float elapsedTime = 0;
        
        while (elapsedTime < animationDuration)
        {
            scrollViewRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终位置精确
        scrollViewRectTransform.anchoredPosition = endPos;
        
        // 执行完成回调
        if (onComplete != null)
            onComplete();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
