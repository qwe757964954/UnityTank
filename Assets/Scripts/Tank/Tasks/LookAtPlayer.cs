using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Complete
{
    [TaskCategory("Tank/Movement")]
    public class LookAtPlayer : Action
    {
        [UnityEngine.Tooltip("目标玩家")]
        public SharedGameObject player;

        [UnityEngine.Tooltip("旋转速度")]
        public SharedFloat rotationSpeed = 5f;

        [UnityEngine.Tooltip("是否只在水平面上旋转")]
        public SharedBool horizontalOnly = true;

        // 坦克的旋转控制引用
        private TankMovement tankMovement;

        public override void OnAwake()
        {
            tankMovement = GetComponent<TankMovement>();
        }

        public override TaskStatus OnUpdate()
        {
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

            // 计算朝向玩家的方向
            Vector3 direction = player.Value.transform.position - transform.position;
            
            // 如果只需要水平旋转，忽略Y轴
            if (horizontalOnly.Value)
                direction.y = 0;

            // 如果方向为零向量，返回成功
            if (direction.sqrMagnitude < 0.001f)
                return TaskStatus.Success;

            // 如果使用TankMovement控制旋转
            if (tankMovement != null)
            {
                // 计算需要旋转的角度
                float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
                
                // 通过TankMovement的m_TurnInputValue控制旋转
                // 将角度归一化到-1到1之间作为输入值
                tankMovement.m_TurnInputValue = Mathf.Clamp(angle / 90f, -1f, 1f);
                
                // 返回正在进行
                return TaskStatus.Running;
            }
            else
            {
                // 如果没有TankMovement组件，直接使用Transform旋转
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    Time.deltaTime * rotationSpeed.Value);
                
                // 如果朝向足够接近目标方向，返回成功
                if (Quaternion.Angle(transform.rotation, targetRotation) < 5f)
                    return TaskStatus.Success;
                
                return TaskStatus.Running;
            }
        }

        public override void OnEnd()
        {
            // 确保在任务结束时重置转向输入
            if (tankMovement != null)
                tankMovement.m_TurnInputValue = 0f;
        }
    }
}