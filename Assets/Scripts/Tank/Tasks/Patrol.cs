using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections.Generic;

namespace Complete
{
    [TaskCategory("Tank/Movement")]
    public class Patrol : Action
    {
        [UnityEngine.Tooltip("巡逻范围")]
        public SharedFloat patrolRadius = 150f;

        [UnityEngine.Tooltip("是否在范围内随机选择点")]
        public SharedBool randomPositions = true;

        [UnityEngine.Tooltip("到达巡逻点的停止距离")]
        public SharedFloat stoppingDistance = 1f;

        [UnityEngine.Tooltip("在每个巡逻点等待的时间")]
        public SharedFloat waitTime = 5f;

        [UnityEngine.Tooltip("检测障碍物的距离")]
        public SharedFloat obstacleDetectionDistance = 2f;

        [UnityEngine.Tooltip("障碍物层级")]
        public LayerMask obstacleLayer = -1;

        [UnityEngine.Tooltip("撞到障碍物后后退的距离")]
        public SharedFloat retreatDistance = 5f; // Distance to move backward when hitting an obstacle

        private TankMovement tankMovement;
        private Vector3 currentTarget;
        private float waitTimer = 0f;
        private int state = 2;
        private List<Vector3> patrolPoints;

        public override void OnAwake()
        {
            tankMovement = GetComponent<TankMovement>();
        }

        public override void OnStart()
        {
            patrolPoints = new List<Vector3>();
            patrolPoints.Add(transform.position);
            for (int i = 0; i < 4; i++)
            {
                Vector2 randomPoint = Random.insideUnitCircle * patrolRadius.Value;
                Vector3 worldPoint = transform.position + new Vector3(randomPoint.x, 0, randomPoint.y);
                patrolPoints.Add(worldPoint);
            }
            state = 2;
            waitTimer = 0f;
        }

        public override TaskStatus OnUpdate()
        {
            if (patrolPoints == null || patrolPoints.Count == 0)
                return TaskStatus.Failure;

            switch (state)
            {
                case 0: return MoveToTarget();
                case 1: return WaitAtTarget();
                case 2:
                    SelectNextTarget();
                    state = 0;
                    return TaskStatus.Running;
                default: return TaskStatus.Failure;
            }
        }

        private TaskStatus MoveToTarget()
        {
            Vector3 direction = currentTarget - transform.position;
            direction.y = 0;
            float distance = direction.magnitude;

            // Check for obstacles
            if (tankMovement != null && Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, obstacleDetectionDistance.Value, obstacleLayer))
            {
                // Move in the opposite direction
                RetreatFromObstacle();
                return TaskStatus.Running;
            }

            if (distance < stoppingDistance.Value)
            {
                if (tankMovement != null)
                {
                    tankMovement.m_MovementInputValue = 0f;
                    tankMovement.m_TurnInputValue = 0f;
                }
                state = 1;
                waitTimer = waitTime.Value;
                return TaskStatus.Running;
            }

            if (tankMovement != null)
            {
                float forwardAmount = Vector3.Dot(transform.forward, direction.normalized);
                float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
                tankMovement.m_MovementInputValue = Mathf.Clamp(forwardAmount, -1f, 1f);
                tankMovement.m_TurnInputValue = Mathf.Clamp(angle / 90f, -1f, 1f);
            }
            return TaskStatus.Running;
        }

        private TaskStatus WaitAtTarget()
        {
            waitTimer -= Time.deltaTime;
            if (tankMovement != null)
            {
                tankMovement.m_MovementInputValue = 0f;
                tankMovement.m_TurnInputValue = 0f;
            }
            if (waitTimer <= 0)
            {
                state = 2;
            }
            return TaskStatus.Running;
        }

        private void SelectNextTarget()
        {
            int index = Random.Range(0, patrolPoints.Count);
            if (randomPositions.Value && Random.value > 0.5f)
            {
                Vector2 randomPoint = Random.insideUnitCircle * patrolRadius.Value;
                currentTarget = transform.position + new Vector3(randomPoint.x, 0, randomPoint.y);
                Debug.Log($"New random patrol point: {currentTarget}");
            }
            else
            {
                currentTarget = patrolPoints[index];
                Debug.Log($"Selected predefined patrol point: {currentTarget}");
            }
        }

        private void RetreatFromObstacle()
        {
            // Calculate a new target point in the opposite direction of the current forward vector
            Vector3 retreatDirection = -transform.forward; // Opposite direction
            retreatDirection.y = 0; // Keep it on the same plane
            currentTarget = transform.position + retreatDirection.normalized * retreatDistance.Value;
            Debug.Log($"Obstacle detected, retreating to: {currentTarget}");
        }

        public override void OnEnd()
        {
            if (tankMovement != null)
            {
                tankMovement.m_MovementInputValue = 0f;
                tankMovement.m_TurnInputValue = 0f;
            }
        }
    }
}