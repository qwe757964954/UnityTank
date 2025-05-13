using Unity.Netcode;
using BehaviorDesigner.Runtime.Tasks;

namespace BehaviorDesigner.Runtime.Tasks.Basic.UnityNetwork
{
    [TaskCategory("Basic/Network")]
    public class IsClient : Conditional
    {
        public override TaskStatus OnUpdate()
        {
            return NetworkManager.Singleton.IsClient ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}