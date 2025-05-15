using UnityEngine;
using System.Collections;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        public float m_Speed = 12f;                 // How fast the tank moves forward and back.
        public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
        public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
		public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

        public float m_MovementInputValue;         // The current value of the movement input.
        public float m_TurnInputValue;             // The current value of the turn input.
        private string m_MovementAxisName;         // The name of the input axis for moving forward and back.
        private string m_TurnAxisName;             // The name of the input axis for turning.
        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
        private ParticleSystem[] m_particleSystems; // References to all the particles systems used by the Tanks
        public VirtualJoystick joystick; // 拖到Inspector
        private AudioSource m_PermanentIdleAudio; // 永久怠速音效
        private AudioSource m_PermanentDrivingAudio; // 永久行驶音效
        private bool permanentAudioSetup = false; // 是否已设置永久音效
        
        private void Awake ()
        {
            m_Rigidbody = GetComponent<Rigidbody> ();
        }


        private void OnEnable ()
        {
            // When the tank is turned on, make sure it's not kinematic.
            m_Rigidbody.isKinematic = false;

            // Also reset the input values.
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;

            // We grab all the Particle systems child of that Tank to be able to Stop/Play them on Deactivate/Activate
            // It is needed because we move the Tank when spawning it, and if the Particle System is playing while we do that
            // it "think" it move from (0,0,0) to the spawn point, creating a huge trail of smoke
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Play();
            }
        }


        private void OnDisable ()
        {
            // When the tank is turned off, set it to kinematic so it stops moving.
            m_Rigidbody.isKinematic = true;

            // Stop all particle system so it "reset" it's position to the actual one instead of thinking we moved when spawning
            for(int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }
        }


        private void Start ()
        {
            // The axes names are based on player number.
            m_MovementAxisName = "Vertical" + m_PlayerNumber;
            m_TurnAxisName = "Horizontal" + m_PlayerNumber;

            // 确保音频组件正确设置
            SetupAudio();
        }

        // 确保音频组件被正确设置
        private void SetupAudio()
        {
            // 创建永久音频源
            SetupPermanentAudioSources();
            
            // 如果没有音频源，尝试从对象获取
            if (m_MovementAudio == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                if (sources.Length > 0)
                {
                    m_MovementAudio = sources[0];
                }
            }

            // 确保有音频剪辑
            if (m_EngineIdling == null || m_EngineDriving == null)
            {
                // 尝试从AudioClips目录加载音频文件
                AudioClip idleClip = Resources.Load<AudioClip>("AudioClips/EngineIdle");
                AudioClip drivingClip = Resources.Load<AudioClip>("AudioClips/EngineDriving");
                
                if (idleClip != null && drivingClip != null)
                {
                    m_EngineIdling = idleClip;
                    m_EngineDriving = drivingClip;
                }
                else
                {
                    // 如果仍然加载失败，尝试用AssetDatabase加载
                    #if UNITY_EDITOR
                    m_EngineIdling = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AudioClips/EngineIdle.aif");
                    m_EngineDriving = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AudioClips/EngineDriving.aif");
                    #endif
                    
                    // 如果资源加载失败，尝试从玩家坦克复制
                    if (m_EngineIdling == null || m_EngineDriving == null)
                    {
                        GameObject playerTank = GameObject.FindWithTag("Player");
                        if (playerTank != null && playerTank != gameObject)
                        {
                            TankMovement playerMovement = playerTank.GetComponent<TankMovement>();
                            if (playerMovement != null && playerMovement.m_EngineIdling != null && playerMovement.m_EngineDriving != null)
                            {
                                m_EngineIdling = playerMovement.m_EngineIdling;
                                m_EngineDriving = playerMovement.m_EngineDriving;
                            }
                        }
                    }
                }
            }

            // 确保音频源已正确设置
            if (m_MovementAudio != null)
            {
                m_MovementAudio.volume = 0.5f;
                m_MovementAudio.loop = true;
                m_MovementAudio.playOnAwake = true;
                m_MovementAudio.spatialBlend = 1.0f;
                m_MovementAudio.mute = false;
                
                // 存储原始音高
                m_OriginalPitch = m_MovementAudio.pitch;
            }
        }

        // 设置永久音频源
        private void SetupPermanentAudioSources()
        {
            if (permanentAudioSetup)
                return;
                
            // 创建两个永久音频源，专门用于播放引擎声音
            GameObject idleAudioObj = new GameObject("PermanentIdleAudio_" + gameObject.name);
            idleAudioObj.transform.parent = transform;
            idleAudioObj.transform.localPosition = Vector3.zero;
            m_PermanentIdleAudio = idleAudioObj.AddComponent<AudioSource>();
            
            GameObject drivingAudioObj = new GameObject("PermanentDrivingAudio_" + gameObject.name);
            drivingAudioObj.transform.parent = transform;
            drivingAudioObj.transform.localPosition = Vector3.zero;
            m_PermanentDrivingAudio = drivingAudioObj.AddComponent<AudioSource>();
            
            // 确保有音频剪辑
            if (m_EngineIdling == null || m_EngineDriving == null)
            {
                // 尝试从其他地方获取音频剪辑
                GameObject playerTank = GameObject.FindWithTag("Player");
                if (playerTank != null && playerTank != gameObject)
                {
                    TankMovement playerMovement = playerTank.GetComponent<TankMovement>();
                    if (playerMovement != null)
                    {
                        m_EngineIdling = playerMovement.m_EngineIdling;
                        m_EngineDriving = playerMovement.m_EngineDriving;
                    }
                }
                
                #if UNITY_EDITOR
                if (m_EngineIdling == null)
                    m_EngineIdling = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AudioClips/EngineIdle.aif");
                if (m_EngineDriving == null)
                    m_EngineDriving = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AudioClips/EngineDriving.aif");
                #endif
            }
            
            // 设置永久怠速音效
            if (m_EngineIdling != null)
            {
                m_PermanentIdleAudio.clip = m_EngineIdling;
                m_PermanentIdleAudio.loop = true;
                m_PermanentIdleAudio.volume = 0.7f; // 增加基础音量
                m_PermanentIdleAudio.spatialBlend = 1.0f; // 3D音效
                m_PermanentIdleAudio.playOnAwake = true;
                m_PermanentIdleAudio.mute = false;
                
                // 配置3D音效衰减参数
                m_PermanentIdleAudio.rolloffMode = AudioRolloffMode.Custom; // 使用自定义衰减曲线
                m_PermanentIdleAudio.SetCustomCurve(
                    AudioSourceCurveType.CustomRolloff,
                    CreateAudioRolloffCurve()
                );
                m_PermanentIdleAudio.minDistance = 2f; // 2米内保持全音量
                m_PermanentIdleAudio.maxDistance = 80f; // 40米外听不到
                m_PermanentIdleAudio.dopplerLevel = 0.2f; // 降低多普勒效应
                m_PermanentIdleAudio.spread = 30f; // 增加声音分散角度
                
                m_PermanentIdleAudio.Play();
            }
            
            // 设置永久行驶音效
            if (m_EngineDriving != null)
            {
                m_PermanentDrivingAudio.clip = m_EngineDriving;
                m_PermanentDrivingAudio.loop = true;
                m_PermanentDrivingAudio.volume = 0.0f; // 初始音量为0
                m_PermanentDrivingAudio.spatialBlend = 1.0f; // 3D音效
                m_PermanentDrivingAudio.playOnAwake = true;
                m_PermanentDrivingAudio.mute = false;
                
                // 配置3D音效衰减参数
                m_PermanentDrivingAudio.rolloffMode = AudioRolloffMode.Custom; // 使用自定义衰减曲线
                m_PermanentDrivingAudio.SetCustomCurve(
                    AudioSourceCurveType.CustomRolloff,
                    CreateAudioRolloffCurve()
                );
                m_PermanentDrivingAudio.minDistance = 2f; // 2米内保持全音量
                m_PermanentDrivingAudio.maxDistance = 80f; // 40米外听不到
                m_PermanentDrivingAudio.dopplerLevel = 0.3f; // 适当的多普勒效应
                m_PermanentDrivingAudio.spread = 40f; // 增加声音分散角度
                
                m_PermanentDrivingAudio.Play();
            }
            
            // 确保原始音频源的设置也是正确的
            if (m_MovementAudio != null)
            {
                m_MovementAudio.spatialBlend = 1.0f; // 3D音效
                m_MovementAudio.rolloffMode = AudioRolloffMode.Custom;
                m_MovementAudio.SetCustomCurve(
                    AudioSourceCurveType.CustomRolloff,
                    CreateAudioRolloffCurve()
                );
                m_MovementAudio.minDistance = 2f;
                m_MovementAudio.maxDistance = 80f;
                m_MovementAudio.dopplerLevel = 0.2f;
                m_MovementAudio.spread = 30f;
            }
            
            permanentAudioSetup = true;
            
            // 启动音量控制协程
            StartCoroutine(ManagePermanentAudioVolume());
        }
        
        // 创建音量衰减曲线 - 平滑的距离衰减
        private AnimationCurve CreateAudioRolloffCurve()
        {
            // 创建一个自定义的音量衰减曲线
            AnimationCurve curve = new AnimationCurve();
            
            // 音量在最小距离处保持为1
            curve.AddKey(0f, 1f);
            // 5米处仍然有80%音量
            curve.AddKey(0.2f, 0.8f);
            // 10米处还有50%音量
            curve.AddKey(0.4f, 0.5f);
            // 15米处有25%音量
            curve.AddKey(0.6f, 0.25f);
            // 20米处只有10%音量
            curve.AddKey(0.8f, 0.1f);
            // 最大距离时音量为0
            curve.AddKey(1f, 0f);
            
            // 不需要手动设置切线模式，Unity会自动处理曲线平滑
            
            return curve;
        }
        
        // 持续控制永久音频的音量
        private IEnumerator ManagePermanentAudioVolume()
        {
            while (true)
            {
                if (m_PermanentIdleAudio != null && m_PermanentDrivingAudio != null)
                {
                    // 计算移动强度
                    float movementIntensity = Mathf.Max(
                        Mathf.Abs(m_MovementInputValue),
                        Mathf.Abs(m_TurnInputValue)
                    );
                    
                    // 平滑过渡
                    float idleTargetVolume = Mathf.Lerp(0.6f, 0.0f, movementIntensity); // 增加怠速音量
                    float drivingTargetVolume = Mathf.Lerp(0.0f, 1.0f, movementIntensity); // 增加行驶音量到最大
                    
                    // 应用音量
                    m_PermanentIdleAudio.volume = Mathf.Lerp(m_PermanentIdleAudio.volume, idleTargetVolume, Time.deltaTime * 3.0f);
                    m_PermanentDrivingAudio.volume = Mathf.Lerp(m_PermanentDrivingAudio.volume, drivingTargetVolume, Time.deltaTime * 3.0f);
                    
                    // 确保声音总是在播放
                    if (!m_PermanentIdleAudio.isPlaying)
                    {
                        m_PermanentIdleAudio.Play();
                    }
                    
                    if (!m_PermanentDrivingAudio.isPlaying)
                    {
                        m_PermanentDrivingAudio.Play();
                    }
                }
                
                yield return null;
            }
        }

        private void Update()
        {
            Vector2 input;
            if (joystick != null)
                input = joystick.InputDirection;
            else
                input = new Vector2(Input.GetAxis(m_TurnAxisName), Input.GetAxis(m_MovementAxisName));

            // 方向盘式控制
            if (input.magnitude > 0.1f)
            {
                // 计算目标世界方向
                Vector3 targetDir = new Vector3(input.x, 0, input.y).normalized;
                // 计算当前朝向与目标方向的夹角
                float angle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
                // 平滑转向，只有前进时才能转向
                float turn = Mathf.Clamp(angle / 45f, -1f, 1f) * input.magnitude; // 45度内全速转向
                m_TurnInputValue = turn;
                // 前进速度与输入强度相关
                m_MovementInputValue = input.magnitude;
            }
            else
            {
                m_MovementInputValue = 0f;
                m_TurnInputValue = 0f;
            }
            EngineAudio();
        }


        private void EngineAudio ()
        {
            // 如果音频组件未初始化，不执行后续操作
            if (m_MovementAudio == null || m_EngineIdling == null || m_EngineDriving == null)
            {
                return;
            }
            
            // 判断坦克是否在移动
            bool isMoving = Mathf.Abs(m_MovementInputValue) > 0.1f || Mathf.Abs(m_TurnInputValue) > 0.1f;
            
            // 选择要播放的声音
            AudioClip targetClip = isMoving ? m_EngineDriving : m_EngineIdling;
            
            // 如果当前没有播放任何声音，或者需要切换声音
            if (!m_MovementAudio.isPlaying || m_MovementAudio.clip != targetClip)
            {
                m_MovementAudio.Stop(); // 先停止当前声音
                m_MovementAudio.clip = targetClip; // 设置新声音
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play(); // 播放新声音
            }
        }

        private void FixedUpdate ()
        {
            // Adjust the rigidbodies position and orientation in FixedUpdate.
            Move ();
            Turn ();
        }


        private void Move ()
        {
            // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
            Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;

            // Apply this movement to the rigidbody's position.
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }


        private void Turn ()
        {
            // Determine the number of degrees to be turned based on the input, speed and time between frames.
            float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

            // Make this into a rotation in the y axis.
            Quaternion turnRotation = Quaternion.Euler (0f, turn, 0f);

            // Apply this rotation to the rigidbody's rotation.
            m_Rigidbody.MoveRotation (m_Rigidbody.rotation * turnRotation);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 只处理Player标签的Tank
            if (gameObject.tag != "Player")
                return;

            // 检查碰撞对象是否为transmit
            if (collision.gameObject.tag == "transmit")
            {
                // 获取当前场景名
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene == "Tank")
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Tank2");
                }
                else if (currentScene == "Tank2")
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Tank");
                }
            }
        }
    }
}