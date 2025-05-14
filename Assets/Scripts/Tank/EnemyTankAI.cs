using UnityEngine;

namespace Complete
{
    public class EnemyTankAI : MonoBehaviour
    {
        public Transform player;
        public float patrolRadius = 10f;
        public float chaseDistance = 15f;
        public float shootDistance = 12f;
        public float patrolWaitTime = 2f;
        private float patrolTimer;
        private Vector3 patrolTarget;
        private Complete.TankMovement tankMovement;
        private Complete.TankShooting tankShooting;
        private enum State { Patrol, Chase, Attack }
        private State currentState;
        // 记录连续卡住的次数
        private int stuckCounter = 0;
        private Vector3 lastPosition;
        private float stuckCheckTimer = 0f;
        private float stuckDistance = 0.2f; // 判定为卡住的移动距离阈值
        private bool isBackingUp = false; // 是否正在后退
        private float backupTimer = 0f; // 后退计时器
        private float backupDuration = 1.5f; // 后退持续时间
        private Vector3 backupDirection; // 后退方向

        void Start()
        {
            tankMovement = GetComponent<Complete.TankMovement>();
            tankShooting = GetComponent<Complete.TankShooting>();
            patrolTimer = patrolWaitTime;
            SetNewPatrolTarget();
            currentState = State.Patrol;
            if (player == null && GameObject.FindWithTag("Player") != null)
                player = GameObject.FindWithTag("Player").transform;
            
            // 初始化上次位置
            lastPosition = transform.position;
        }

        void Update()
        {
            if (player == null) return;
            float distance = Vector3.Distance(transform.position, player.position);
            switch (currentState)
            {
                case State.Patrol:
                    Patrol();
                    if (distance < chaseDistance)
                        currentState = State.Chase;
                    break;
                case State.Chase:
                    Chase();
                    if (distance < shootDistance)
                        currentState = State.Attack;
                    else if (distance > chaseDistance)
                        currentState = State.Patrol;
                    break;
                case State.Attack:
                    Attack();
                    if (distance > shootDistance)
                        currentState = State.Chase;
                    break;
            }
        }

        void Patrol()
        {
            // 如果正在执行后退操作，优先处理
            if (isBackingUp)
            {
                HandleBackup();
                return;
            }
            
            // 检查是否卡住
            CheckIfStuck();
            
            Vector3 dir = (patrolTarget - transform.position).normalized;
            float checkDistance = 2.5f; // 增加检测距离
            
            // 多方向射线检测
            bool pathBlocked = false;
            
            // 主方向射线
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, checkDistance))
            {
                pathBlocked = true;
            }
            
            // 左右侧射线检测
            Vector3 rightDir = Quaternion.Euler(0, 30, 0) * dir;
            Vector3 leftDir = Quaternion.Euler(0, -30, 0) * dir;
            
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rightDir, checkDistance) &&
                Physics.Raycast(transform.position + Vector3.up * 0.5f, leftDir, checkDistance))
            {
                // 如果前方、左右都被挡住，情况更严重
                pathBlocked = true;
            }
            
            // 如果连续卡住次数过多或路径被阻挡
            if (stuckCounter >= 2 || pathBlocked)
            {
                // 先后退一段距离，再寻找新路径
                StartBackup();
                return;
            }
            
            MoveTowards(dir);
            if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            {
                patrolTimer -= Time.deltaTime;
                if (patrolTimer <= 0f)
                {
                    SetNewPatrolTarget();
                    patrolTimer = patrolWaitTime;
                }
            }
        }

        void Chase()
        {
            Vector3 dir = (player.position - transform.position).normalized;
            MoveTowards(dir);
        }

        void Attack()
        {
            Vector3 dir = (player.position - transform.position).normalized;
            MoveTowards(Vector3.zero); // 停止移动
            // 朝向玩家
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
            // 自动开火
            tankShooting.m_FireButton = "Fire1"; // 兼容TankShooting
            if (tankShooting != null)
                tankShooting.Invoke("Fire", 0f);
        }

        void MoveTowards(Vector3 dir)
        {
            if (tankMovement == null) return;
            // 只用z轴为前进，x轴为转向
            float forward = Vector3.Dot(dir, transform.forward);
            float turn = Vector3.SignedAngle(transform.forward, dir, Vector3.up) / 45f;
            tankMovement.m_MovementInputValue = Mathf.Clamp(forward, -1f, 1f);
            tankMovement.m_TurnInputValue = Mathf.Clamp(turn, -1f, 1f);
        }

        void SetNewPatrolTarget()
        {
            Vector2 rand = Random.insideUnitCircle * patrolRadius;
            patrolTarget = new Vector3(transform.position.x + rand.x, transform.position.y, transform.position.z + rand.y);
        }

        // 检查AI是否卡住
        void CheckIfStuck()
        {
            stuckCheckTimer += Time.deltaTime;
            
            // 每1秒检查一次是否在移动
            if (stuckCheckTimer >= 1f)
            {
                float movedDistance = Vector3.Distance(transform.position, lastPosition);
                
                // 如果几乎没动，且尝试移动（输入不为零）
                if (movedDistance < stuckDistance && tankMovement.m_MovementInputValue != 0)
                {
                    stuckCounter++;
                }
                else
                {
                    // 如果移动了，减少卡住计数
                    stuckCounter = Mathf.Max(0, stuckCounter - 1);
                }
                
                // 更新位置和计时器
                lastPosition = transform.position;
                stuckCheckTimer = 0f;
            }
        }

        // 开始后退
        void StartBackup()
        {
            isBackingUp = true;
            backupTimer = 0f;
            
            // 确定后退方向（与当前前进方向相反，并添加一些随机性）
            backupDirection = -transform.forward;
            float randomAngle = Random.Range(-45f, 45f);
            backupDirection = Quaternion.Euler(0, randomAngle, 0) * backupDirection;
        }
        
        // 处理后退过程
        void HandleBackup()
        {
            backupTimer += Time.deltaTime;
            
            if (backupTimer < backupDuration)
            {
                // 执行后退
                MoveTowards(-backupDirection); // 后退方向
            }
            else
            {
                // 后退完成后，重新选择巡逻点并继续前进
                isBackingUp = false;
                
                // 选择一个与当前位置相反的方向的新巡逻点
                Vector2 rand = Random.insideUnitCircle.normalized * patrolRadius;
                Vector3 newDir = -backupDirection + new Vector3(rand.x * 0.5f, 0, rand.y * 0.5f);
                patrolTarget = transform.position + newDir.normalized * patrolRadius * 0.7f;
                
                // 重置计时器和卡住计数
                patrolTimer = patrolWaitTime;
                stuckCounter = 0;
            }
        }
    }
}
