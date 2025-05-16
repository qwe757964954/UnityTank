using UnityEngine;

namespace Complete
{
    public class CollectibleProp : MonoBehaviour
    {
        public string propId;           // 道具ID
        public string propName;         // 道具名称
        public string spriteName;       // 对应的Sprite资源名
        public string description;      // 道具描述
        
        // OnCollisionEnter方法已移除，因为在TankMovement中已经处理了碰撞逻辑
        // 这样避免收集一个道具却添加两次到背包的问题
        
        private void PlayCollectEffect()
        {
            // 可以在这里添加收集效果，如音效、粒子等
            // 例如：
            // AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
    }
}