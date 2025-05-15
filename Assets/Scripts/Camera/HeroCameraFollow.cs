using UnityEngine;

namespace Complete
{
    public class HeroCameraFollow : MonoBehaviour
{
    public string playerTag = "Player"; // player1 坦克的Tag
    public Vector3 offset = new Vector3(0, 2, 5); // 跟随的偏移
    public float followSpeed = 5f;
    public LayerMask targetLayers; // 添加可以追踪的层级掩码
    
    private Transform player;
    private bool debugLogShown = false; // 防止日志刷屏

    void Start()
    {
        FindPlayer();
        Debug.Log("HeroCameraFollow 启动，当前查找Tag: " + playerTag);
    }
    
    void FindPlayer()
    {
        // 优先查找同时满足Tag和名称条件的对象
        GameObject playerObj = null;
        
        // 获取所有带Player标签的对象
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(playerTag);
        Debug.Log("找到 " + taggedObjects.Length + " 个带Player标签的对象");
        
        // 过滤掉SpawnPoint，优先选择包含"Tank"的对象
        foreach (GameObject obj in taggedObjects)
        {
            Debug.Log("检查对象: " + obj.name + ", Layer: " + obj.layer);
            
            // 排除SpawnPoint
            if (obj.name.Contains("SpawnPoint"))
            {
                Debug.Log("忽略生成点: " + obj.name);
                continue;
            }
            
            // 优先选择坦克对象
            if (obj.name.Contains("Tank"))
            {
                playerObj = obj;
                Debug.Log("找到坦克对象: " + obj.name);
                break;
            }
            
            // 如果没有找到明确的坦克，记住第一个非SpawnPoint对象
            if (playerObj == null)
            {
                playerObj = obj;
            }
        }
        
        // 如果通过标签无法找到合适对象，尝试通过层级查找
        if (playerObj == null)
        {
            Debug.Log("通过标签未找到合适对象，尝试通过层级查找");
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            
            foreach (GameObject obj in allObjects)
            {
                // 检查对象是否在第9层(你设置的坦克层级)
                if (obj.layer == 9 && obj.name.Contains("Tank"))
                {
                    playerObj = obj;
                    Debug.Log("通过层级找到坦克: " + obj.name + ", Layer: " + obj.layer);
                    break;
                }
            }
        }
        
        // 最后尝试：根据TankMovement组件查找
        if (playerObj == null)
        {
            Debug.Log("尝试通过TankMovement组件查找坦克");
            TankMovement[] tanks = FindObjectsOfType<TankMovement>();
            if (tanks.Length > 0)
            {
                playerObj = tanks[0].gameObject;
                Debug.Log("通过TankMovement组件找到坦克: " + playerObj.name);
            }
        }
        
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("最终选定的玩家对象: " + playerObj.name + ", Tag: " + playerObj.tag + ", Layer: " + playerObj.layer);
            
            // 确保玩家对象在第9层
            if (playerObj.layer != 9)
            {
                Debug.LogWarning("玩家对象不在正确的层级上！当前层级: " + playerObj.layer + "，应为9");
            }
        }
        else
        {
            Debug.LogError("无法找到任何坦克对象！请检查场景中是否有坦克实例。");
        }
    }

    void LateUpdate()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        // 跟随player1坦克
        Vector3 targetPos = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        transform.LookAt(player.position + Vector3.up * 1.0f); // 让摄像机始终看向坦克
        
        // 每5秒重置debug标志以允许偶尔输出日志
        if (Time.frameCount % 300 == 0)
        {
            debugLogShown = false;
        }
    }
}

}
