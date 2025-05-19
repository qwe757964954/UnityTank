using UnityEngine;

namespace Complete
{
    public class CameraControl : MonoBehaviour
    {
        public float m_DampTime = 0.2f;                 
        public float m_ScreenEdgeBuffer = 4f;           
        public float m_MinSize = 6.5f;                  
        [HideInInspector] public Transform[] m_Targets;

        // 摄像机设置
        public float m_CameraHeight = 25f;              // 摄像机高度
        public float m_FOV = 60f;                       // 视野范围
        public float m_MinZoom = 5f;                    // 最小缩放
        public float m_MaxZoom = 15f;                   // 最大缩放
        public LayerMask m_CollisionLayers;             // 碰撞层

        // 角度设置
        public float m_CameraPitch = 0f;               // 相机俯仰角度（60度俯视）
        public float m_CameraYaw = 0f;                  // 相机水平旋转角度
    
        private Camera m_Camera;                        
        private float m_ZoomSpeed;                      
        private Vector3 m_MoveVelocity;                 
        private Vector3 m_DesiredPosition;              

        private void Awake()
        {
            m_Camera = GetComponentInChildren<Camera>();
            if (m_Camera == null)
                m_Camera = Camera.main;
                
            // 确保摄像机设置正确
            m_Camera.fieldOfView = m_FOV;
        }

        private void Start()
        {
            // 立即设置初始位置和朝向
            SetStartPositionAndSize();
        }

        private void FixedUpdate()
        {
            Move();
            Zoom();
        }

        private void Move()
        {
            // 找到所有目标的平均位置
            FindAveragePosition();
            
            // 计算摄像机的目标位置
            Vector3 targetPosition = CalculateCameraPosition();
            
            // 平滑移动到目标位置
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref m_MoveVelocity, m_DampTime);
            
            // 设置摄像机旋转 - 斜视角
            transform.rotation = Quaternion.Euler(m_CameraPitch, m_CameraYaw, 0f);
        }

        private Vector3 CalculateCameraPosition()
        {
            // 根据角度计算位置偏移
            float pitch = m_CameraPitch * Mathf.Deg2Rad;
            float yaw = m_CameraYaw * Mathf.Deg2Rad;
            
            // 计算相对位置（考虑旋转角度）
            float xOffset = -Mathf.Sin(yaw) * m_CameraHeight * Mathf.Cos(pitch);
            float zOffset = -Mathf.Cos(yaw) * m_CameraHeight * Mathf.Cos(pitch);
            float yOffset = m_CameraHeight * Mathf.Sin(pitch);
            
            // 计算最终相机位置
            return new Vector3(
                m_DesiredPosition.x + xOffset,
                m_DesiredPosition.y + yOffset,
                m_DesiredPosition.z + zOffset
            );
        }

        private void FindAveragePosition()
        {
            Vector3 averagePos = new Vector3();
            int numTargets = 0;
            
            // 计算所有活动目标的平均位置
            for (int i = 0; i < m_Targets.Length; i++)
            {
                if (!m_Targets[i].gameObject.activeSelf)
                    continue;
                    
                averagePos += m_Targets[i].position;
                numTargets++;
            }
            
            if (numTargets > 0)
                averagePos /= numTargets;
                
            // 保存平均位置
            m_DesiredPosition = averagePos;
        }

        private void Zoom()
        {
            // 计算所需的缩放大小
            float requiredSize = FindRequiredSize();
            
            // 调整摄像机高度作为缩放方式
            float targetHeight = Mathf.Clamp(requiredSize * 2f, m_MinZoom, m_MaxZoom);
            m_CameraHeight = Mathf.SmoothDamp(m_CameraHeight, targetHeight, ref m_ZoomSpeed, m_DampTime);
        }

        private float FindRequiredSize()
        {
            // 找到需要包含所有目标的视野大小
            float size = 0f;
            
            for (int i = 0; i < m_Targets.Length; i++)
            {
                if (!m_Targets[i].gameObject.activeSelf)
                    continue;
                    
                // 计算从目标中心到当前目标的向量
                Vector3 targetVector = m_Targets[i].position - m_DesiredPosition;
                
                // 计算水平距离
                float distance = new Vector2(targetVector.x, targetVector.z).magnitude;
                
                // 找到最大所需视野大小
                size = Mathf.Max(size, distance);
            }
            
            // 添加边缘缓冲
            size += m_ScreenEdgeBuffer;
            size = Mathf.Max(size, m_MinSize);
            
            return size;
        }

        public void SetStartPositionAndSize()
        {
            // 找到目标平均位置
            FindAveragePosition();
            
            // 设置摄像机位置
            transform.position = CalculateCameraPosition();
            
            // 设置摄像机旋转
            transform.rotation = Quaternion.Euler(m_CameraPitch, m_CameraYaw, 0f);
            
            // 设置摄像机FOV
            m_Camera.fieldOfView = m_FOV;
        }
    }
}