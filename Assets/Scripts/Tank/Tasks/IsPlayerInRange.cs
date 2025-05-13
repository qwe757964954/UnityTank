using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Complete
{
    [TaskCategory("Tank/Sensing")]
    public class IsPlayerInRange : Conditional
    {
        [UnityEngine.Tooltip("检测范围")]
        public SharedFloat detectRange = 15f;

        [UnityEngine.Tooltip("玩家对象")]
        public SharedGameObject player;

        [UnityEngine.Tooltip("是否需要视线检测（射线检测）")]
        public SharedBool requireLineOfSight = true;

        [UnityEngine.Tooltip("检测层级")]
        public LayerMask lineOfSightMask = -1;

        public override TaskStatus OnUpdate()
        {
            // 如果玩家引用为空，尝试查找带有"Player"标签的对象
            if (player.Value == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    player.Value = playerObj;
                }
                else
                {
                    return TaskStatus.Failure;
                }
            }

            // 计算与玩家的距离
            float distance = Vector3.Distance(transform.position, player.Value.transform.position);
            
            // 如果距离大于检测范围，返回失败
            if (distance > detectRange.Value)
                return TaskStatus.Failure;
            
            // 如果需要视线检测
            if (requireLineOfSight.Value)
            {
                Vector3 direction = player.Value.transform.position - transform.position;
                if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, detectRange.Value, lineOfSightMask))
                {
                    // 如果射线击中的是玩家，返回成功
                    if (hit.transform.gameObject == player.Value)
                        return TaskStatus.Success;
                    else
                        return TaskStatus.Failure; // 射线击中了其他物体，视线被阻挡
                }
                return TaskStatus.Failure; // 射线没有击中任何物体
            }
            
            // 不需要视线检测，只要在范围内就返回成功
            return TaskStatus.Success;
        }
    }
}