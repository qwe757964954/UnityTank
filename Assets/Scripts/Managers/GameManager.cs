using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control.
        public TankManager[] m_Tanks;               // A collection of managers for enabling and disabling different aspects of the tanks.

        
        private int m_RoundNumber;                  // Which round the game is currently on.
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
        private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
        private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.
        private CancellationTokenSource m_CTS;      // 用于取消异步任务


        private void Start()
        {
            // 初始化取消令牌源
            m_CTS = new CancellationTokenSource();
            
            // Create the delays so they only have to be made once.
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            SpawnAllTanks();
            SetCameraTargets();

            // Once the tanks have been created and the camera is using them as targets, start the game.
            // 使用 CancellationToken 防止场景销毁后异步任务继续执行
            var token = this.GetCancellationTokenOnDestroy();
            // 使用 Forget() 替代 UniTask.Void 传递 token
            GameLoop(token).Forget();
        }

        private void OnDestroy()
        {
            // 确保取消所有异步任务
            if (m_CTS != null && !m_CTS.IsCancellationRequested)
            {
                m_CTS.Cancel();
                m_CTS.Dispose();
                m_CTS = null;
            }
        }
        
        private void SpawnAllTanks()
        {
            if (m_TankPrefab == null || m_Tanks == null) return;
            
            // For all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i] == null || m_Tanks[i].m_SpawnPoint == null) continue;
                
                // Create them, set their player number and references needed for control.
                m_Tanks[i].m_Instance =
                    Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
                m_Tanks[i].m_PlayerNumber = i + 1;
                m_Tanks[i].Setup();
                
                // Assign tags to the tanks
                if (i == 0 && m_Tanks[i].m_Instance != null)
                {
                    m_Tanks[i].m_Instance.tag = "Player"; // Tag for Tank 1 (assuming this is the player)
                    SetLayerRecursively(m_Tanks[i].m_Instance, 9); // 设置为 Players 层（第8层）以便相机能渲染
                    var tankMovement = m_Tanks[i].m_Instance.GetComponent<TankMovement>();
                    var tankShooting = m_Tanks[i].m_Instance.GetComponent<TankShooting>();

                    if (tankMovement != null)
                    {
                        tankMovement.joystick = GameObject.FindObjectOfType<VirtualJoystick>();
                    }
                    if (tankShooting != null)
                    {
                        tankShooting.attackButton = GameObject.FindObjectOfType<AttackButton>();
                    }
                }
                else if (i == 1 && m_Tanks[i].m_Instance != null)
                {
                    // m_Tanks[i].m_Instance.tag = "Player"; // Tag for Tank 2
                }
                // Add more conditions if there are additional tanks
            }
        }

        private void SetCameraTargets()
        {
            if (m_CameraControl == null || m_Tanks == null) return;
            
            // Create a collection of transforms the same size as the number of tanks.
            Transform[] targets = new Transform[m_Tanks.Length];

            // For each of these transforms...
            for (int i = 0; i < targets.Length; i++)
            {
                // ... set it to the appropriate tank transform.
                if (m_Tanks[i] != null && m_Tanks[i].m_Instance != null)
                {
                    targets[i] = m_Tanks[i].m_Instance.transform;
                }
            }

            // These are the targets the camera should follow.
            m_CameraControl.m_Targets = targets;
        }


        // This is called from start and will run each phase of the game one after another.
        private async UniTask GameLoop(CancellationToken cancellationToken = default)
        {
            if (this == null) return;
            
            try
            {
                // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
                await RoundStarting(cancellationToken);
                
                if (cancellationToken.IsCancellationRequested) return;

                // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
                await RoundPlaying(cancellationToken);
                
                if (cancellationToken.IsCancellationRequested) return;

                // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
                await RoundEnding(cancellationToken);
                
                if (cancellationToken.IsCancellationRequested) return;

                // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found.
                if (m_GameWinner != null)
                {
                    // If there is a game winner, restart the level.
                    SceneManager.LoadScene(0);
                }
                else
                {
                    // If there isn't a winner yet, restart this coroutine so the loop continues.
                    // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
                    // 使用 Forget() 替代 UniTask.Void 传递 token
                    GameLoop(cancellationToken).Forget();
                }
            }
            catch (System.OperationCanceledException)
            {
                // 任务被取消，正常退出
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameLoop error: {ex.Message}");
            }
        }


        private async UniTask RoundStarting(CancellationToken cancellationToken = default)
        {
            if (this == null) return;
            
            try
            {
                // As soon as the round starts reset the tanks and make sure they can't move.
                ResetAllTanks();
                DisableTankControl();

                // Snap the camera's zoom and position to something appropriate for the reset tanks.
                if (m_CameraControl != null)
                {
                    m_CameraControl.SetStartPositionAndSize();
                }

                // Increment the round number and display text showing the players what round it is.
                m_RoundNumber++;
                if (m_MessageText != null)
                {
                    m_MessageText.text = "ROUND " + m_RoundNumber;
                }

                // Wait for the specified length of time until yielding control back to the game loop.
                await UniTask.Delay(System.TimeSpan.FromSeconds(m_StartDelay), cancellationToken: cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                throw; // 重新抛出取消异常
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"RoundStarting error: {ex.Message}");
            }
        }


        private async UniTask RoundPlaying(CancellationToken cancellationToken = default)
        {
            if (this == null) return;
            
            try
            {
                // As soon as the round begins playing let the players control the tanks.
                EnableTankControl();

                // Clear the text from the screen.
                if (m_MessageText != null)
                {
                    m_MessageText.text = string.Empty;
                }

                // While there is not one tank left...
                while (!OneTankLeft())
                {
                    // 检查是否被取消或组件已销毁
                    if (cancellationToken.IsCancellationRequested || this == null)
                    {
                        return;
                    }
                    
                    // ... return on the next frame.
                    await UniTask.Yield(cancellationToken);
                }
            }
            catch (System.OperationCanceledException)
            {
                throw; // 重新抛出取消异常
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"RoundPlaying error: {ex.Message}");
            }
        }


        private async UniTask RoundEnding(CancellationToken cancellationToken = default)
        {
            if (this == null) return;
            
            try
            {
                // Stop tanks from moving.
                DisableTankControl();

                // Clear the winner from the previous round.
                m_RoundWinner = null;

                // See if there is a winner now the round is over.
                m_RoundWinner = GetRoundWinner();

                // If there is a winner, increment their score.
                if (m_RoundWinner != null)
                    m_RoundWinner.m_Wins++;

                // Now the winner's score has been incremented, see if someone has one the game.
                m_GameWinner = GetGameWinner();

                // Get a message based on the scores and whether or not there is a game winner and display it.
                string message = EndMessage();
                if (m_MessageText != null)
                {
                    m_MessageText.text = message;
                }

                // Wait for the specified length of time until yielding control back to the game loop.
                await UniTask.Delay(System.TimeSpan.FromSeconds(m_EndDelay), cancellationToken: cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                throw; // 重新抛出取消异常
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"RoundEnding error: {ex.Message}");
            }
        }


        // This is used to check if there is one or fewer tanks remaining and thus the round should end.
        private bool OneTankLeft()
        {
            if (m_Tanks == null) return true;
            
            int numTanksLeft = 0;
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i] == null || m_Tanks[i].m_Instance == null)
                    continue;
                if (m_Tanks[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }
            return numTanksLeft <= 1;
        }
        
        
        // This function is to find out if there is a winner of the round.
        // This function is called with the assumption that 1 or fewer tanks are currently active.
        private TankManager GetRoundWinner()
        {
            if (m_Tanks == null) return null;
            
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                // 确保 m_Tanks[i] 和 m_Instance 不为空
                if (m_Tanks[i] == null || m_Tanks[i].m_Instance == null)
                    continue;
                    
                // ... and if one of them is active, it is the winner so return it.
                if (m_Tanks[i].m_Instance.activeSelf)
                    return m_Tanks[i];
            }

            // If none of the tanks are active it is a draw so return null.
            return null;
        }


        // This function is to find out if there is a winner of the game.
        private TankManager GetGameWinner()
        {
            if (m_Tanks == null) return null;
            
            // Go through all the tanks...
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i] == null) continue;
                
                // ... and if one of them has enough rounds to win the game, return it.
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                    return m_Tanks[i];
            }

            // If no tanks have enough rounds to win, return null.
            return null;
        }


        // Returns a string message to display at the end of each round.
        private string EndMessage()
        {
            if (m_Tanks == null) return "DRAW!";
            
            // By default when a round ends there are no winners so the default end message is a draw.
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that.
            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            // Add some line breaks after the initial message.
            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message.
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i] == null) continue;
                message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
            }

            // If there is a game winner, change the entire message to reflect that.
            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }


        // This function is used to turn all the tanks back on and reset their positions and properties.
        private void ResetAllTanks()
        {
            if (m_Tanks == null) return;
            
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i] == null) continue;
                m_Tanks[i].Reset();
            }
        }


        private void EnableTankControl()
        {
            if (m_Tanks == null) return;
            
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i] == null) continue;
                m_Tanks[i].EnableControl();
            }
        }


        private void DisableTankControl()
        {
            if (m_Tanks == null) return;
            
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i] == null) continue;
                m_Tanks[i].DisableControl();
            }
        }
        
        public void LoadMainMenu()
        {
            // 在加载新场景前取消所有异步任务
            if (m_CTS != null)
            {
                m_CTS.Cancel();
            }
            SceneManager.LoadScene("Scenes/Tank"); // 替换为你的主菜单场景名称
        }
        
        public void LoadGameScene()
        {
            // 在加载新场景前取消所有异步任务
            if (m_CTS != null)
            {
                m_CTS.Cancel();
            }
            SceneManager.LoadScene("Scenes/Tank2"); // 替换为你的主菜单场景名称
        }
        
        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child == null) continue;
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}