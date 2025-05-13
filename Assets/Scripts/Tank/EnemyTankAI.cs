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

        void Start()
        {
            tankMovement = GetComponent<Complete.TankMovement>();
            tankShooting = GetComponent<Complete.TankShooting>();
            patrolTimer = patrolWaitTime;
            SetNewPatrolTarget();
            currentState = State.Patrol;
            if (player == null && GameObject.FindWithTag("Player") != null)
                player = GameObject.FindWithTag("Player").transform;
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
            Vector3 dir = (patrolTarget - transform.position).normalized;
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
    }
}
