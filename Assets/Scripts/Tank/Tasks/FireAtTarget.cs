using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;

namespace Complete
{
    [TaskCategory("Tank/Combat")]
    public class FireAtTarget : Action
    {
        [UnityEngine.Tooltip("射击的目标")]
        public SharedGameObject target;
        
        [UnityEngine.Tooltip("射击的冷却时间(秒)")]
        public SharedFloat cooldownTime = 2f;
        
        [UnityEngine.Tooltip("射击的概率(0-1)")]
        public SharedFloat firingProbability = 0.8f;
        
        [UnityEngine.Tooltip("射击距离")]
        public SharedFloat firingDistance = 12f;

        // 坦克的射击控制引用
        private TankShooting tankShooting;
        // 记录上次射击的时间
        private float lastFireTime = -10f;

        public override void OnAwake()
        {
            tankShooting = GetComponent<TankShooting>();
        }

        public override TaskStatus OnUpdate()
        {
            // 如果没有射击组件，返回失败
            if (tankShooting == null)
                return TaskStatus.Failure;
            
            // 如果目标为空，尝试查找带有"Player"标签的对象
            if (target.Value == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target.Value = player;
                }
                else
                {
                    return TaskStatus.Failure;
                }
            }
            
            // 计算与目标的距离
            float distance = Vector3.Distance(transform.position, target.Value.transform.position);
            
            // 检查是否在射击距离内
            if (distance > firingDistance.Value)
                return TaskStatus.Failure;
            
            // 检查冷却时间
            if (Time.time - lastFireTime < cooldownTime.Value)
                return TaskStatus.Running;
            
            // 根据概率决定是否射击
            if (Random.value > firingProbability.Value)
                return TaskStatus.Running;
            
            // 执行射击
            Fire();
            
            // 更新上次射击时间
            lastFireTime = Time.time;
            
            return TaskStatus.Success;
        }
        
        private void Fire()
        {
            // 确保可以访问Fire方法
            if (tankShooting != null)
            {
                // 这里我们使用反射调用私有的Fire方法
                // 为避免错误，我们使用Invoke方式调用
                tankShooting.Invoke("Fire", 0);
            }
        }
    }
}