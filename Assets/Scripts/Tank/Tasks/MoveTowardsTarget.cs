using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Complete
{
    [TaskCategory("Tank/Movement")]
    public class MoveTowardsTarget : Action
    {
        [UnityEngine.Tooltip("移动目标")]
        public SharedGameObject target;
        
        [UnityEngine.Tooltip("目标位置")]
        public SharedVector3 targetPosition;
        
        [UnityEngine.Tooltip("到达目标的停止距离")]
        public SharedFloat stoppingDistance = 52f; // Increased to 2f
        
        [UnityEngine.Tooltip("移动速度")]
        public SharedFloat speed = 0.01f;

        private TankMovement tankMovement;
        private bool useTargetPosition = false;

        public override void OnAwake()
        {
            tankMovement = GetComponent<TankMovement>();
        }

        public override void OnStart()
        {
            useTargetPosition = (target.Value == null);
            if (!useTargetPosition && target.Value == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target.Value = player;
                }
            }
        }

        public override TaskStatus OnUpdate()
        {
            // Re-check for target if null
            if (target.Value == null && !useTargetPosition)
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

            Vector3 position = useTargetPosition ? targetPosition.Value : target.Value.transform.position;
            Vector3 direction = position - transform.position;
            direction.y = 0; // Ignore height differences
            float distance = direction.magnitude;

            if (distance < stoppingDistance.Value)
            {
                if (tankMovement != null)
                    tankMovement.m_MovementInputValue = 0f;
                return TaskStatus.Success;
            }

            if (tankMovement != null)
            {
                float forwardAmount = Vector3.Dot(transform.forward, direction.normalized);
                float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
                tankMovement.m_MovementInputValue = Mathf.Clamp(forwardAmount * speed.Value, -1f, 1f);
                tankMovement.m_TurnInputValue = Mathf.Clamp(angle / 90f, -1f, 1f); // Add turning
                return TaskStatus.Running;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            if (tankMovement != null)
            {
                tankMovement.m_MovementInputValue = 0f;
                tankMovement.m_TurnInputValue = 0f; // Reset turn input
            }
        }
    }
}