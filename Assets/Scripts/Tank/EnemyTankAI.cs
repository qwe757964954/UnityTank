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
        private bool audioLogged = false;  // 用于控制日志输出次数

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
            
            // 确保TankMovement的音频组件被正确设置
            if (tankMovement != null)
            {
                AudioSource[] audioSources = GetComponents<AudioSource>();
                if (audioSources.Length > 0)
                {
                    // 确保第一个音频组件设置正确
                    audioSources[0].mute = false;
                    audioSources[0].volume = 0.3f; // 稍微提高音量
                    audioSources[0].playOnAwake = true;
                    audioSources[0].loop = true;
                    
                    // 确保音频剪辑已设置
                    if (tankMovement.m_EngineIdling == null || tankMovement.m_EngineDriving == null)
                    {
                        // 查找Player坦克获取音频剪辑
                        GameObject playerTank = GameObject.FindWithTag("Player");
                        if (playerTank != null)
                        {
                            Complete.TankMovement playerMovement = playerTank.GetComponent<Complete.TankMovement>();
                            if (playerMovement != null)
                            {
                                tankMovement.m_EngineIdling = playerMovement.m_EngineIdling;
                                tankMovement.m_EngineDriving = playerMovement.m_EngineDriving;
                                Debug.Log("从Player坦克复制了音频剪辑");
                            }
                        }
                    }
                    
                    // 强制设置音频源
                    tankMovement.m_MovementAudio = audioSources[0];
                    Debug.Log("已为敌方坦克设置移动音效组件: " + 
                             "引用是否为空=" + (tankMovement.m_MovementAudio == null) + 
                             ", 静音=" + tankMovement.m_MovementAudio.mute + 
                             ", 音量=" + tankMovement.m_MovementAudio.volume);
                }
                else
                {
                    Debug.LogWarning("敌方坦克没有可用的AudioSource组件!");
                }
            }
        }

        void Update()
        {
            // 检查音频组件状态（仅在前几帧输出一次）
            if (!audioLogged && Time.frameCount < 100)
            {
                if (tankMovement != null)
                {
                    Debug.Log("EnemyTank音频状态: " + 
                        "MovementAudio=" + (tankMovement.m_MovementAudio != null) + 
                        ", EngineIdling=" + (tankMovement.m_EngineIdling != null) + 
                        ", EngineDriving=" + (tankMovement.m_EngineDriving != null));
                    
                    if (tankMovement.m_MovementAudio != null)
                    {
                        Debug.Log("音频组件详情: " + 
                            "静音=" + tankMovement.m_MovementAudio.mute + 
                            ", 音量=" + tankMovement.m_MovementAudio.volume +
                            ", 当前剪辑=" + (tankMovement.m_MovementAudio.clip != null ? tankMovement.m_MovementAudio.clip.name : "无") +
                            ", 是否播放=" + tankMovement.m_MovementAudio.isPlaying);
                    }
                    audioLogged = true;
                }
            }

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
            
            // 每5秒检查一次音频状态
            if (Time.frameCount % 300 == 0) // 假设60fps，大约每5秒检查一次
            {
                if (tankMovement.m_MovementAudio != null)
                {
                    // 强制播放引擎声音
                    if (!tankMovement.m_MovementAudio.isPlaying)
                    {
                        Debug.LogWarning("引擎声音未播放，尝试强制播放");
                        tankMovement.m_MovementAudio.Play();
                    }
                    
                    // 输出当前运动信息
                    Debug.Log("坦克移动状态: " + 
                        "前进值=" + tankMovement.m_MovementInputValue + 
                        ", 转向值=" + tankMovement.m_TurnInputValue + 
                        ", 音频正在播放=" + tankMovement.m_MovementAudio.isPlaying);
                    
                    // 手动检查是否应播放行驶声音
                    if (Mathf.Abs(tankMovement.m_MovementInputValue) > 0.1f || Mathf.Abs(tankMovement.m_TurnInputValue) > 0.1f)
                    {
                        if (tankMovement.m_MovementAudio.clip != tankMovement.m_EngineDriving)
                        {
                            Debug.Log("切换到行驶声音");
                            tankMovement.m_MovementAudio.clip = tankMovement.m_EngineDriving;
                            tankMovement.m_MovementAudio.Play();
                        }
                    }
                    else if (tankMovement.m_MovementAudio.clip != tankMovement.m_EngineIdling)
                    {
                        Debug.Log("切换到怠速声音");
                        tankMovement.m_MovementAudio.clip = tankMovement.m_EngineIdling;
                        tankMovement.m_MovementAudio.Play();
                    }
                }
            }
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
