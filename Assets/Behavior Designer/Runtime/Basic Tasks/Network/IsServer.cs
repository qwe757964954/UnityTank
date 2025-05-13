using UnityEngine.Networking;
using Unity.Netcode;
namespace BehaviorDesigner.Runtime.Tasks.Basic.UnityNetwork
{
    public class IsServer : Conditional
    {
        public override TaskStatus OnUpdate()
        {
            return NetworkManager.Singleton.IsClient ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}