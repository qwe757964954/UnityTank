using UnityEngine;

public class HeroCameraFollow : MonoBehaviour
{
    public string playerTag = "Player"; // player1 坦克的Tag
    public Vector3 offset = new Vector3(0, 2, 5); // 跟随的偏移
    public float followSpeed = 5f;

    private Transform player;

    void Start()
    {
        // 只找player1，如果有多个Player，建议用名字或其他方式区分
        GameObject playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null)
            player = playerObj.transform;
    }

    void LateUpdate()
    {
        if (player == null)
        {
            // 动态查找，防止坦克重生时丢失引用
            GameObject playerObj = GameObject.FindWithTag(playerTag);
            if (playerObj != null)
                player = playerObj.transform;
            else
                return;
        }

        // 跟随player1坦克
        Vector3 targetPos = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        transform.LookAt(player.position + Vector3.up * 1.0f); // 让摄像机始终看向坦克
    }
}