using UnityEngine;

namespace Complete
{
    public class CollectibleProp : MonoBehaviour
    {
        public string propId;           // 道具ID
        public string propName;         // 道具名称
        public string spriteName;       // 对应的Sprite资源名
        public string description;      // 道具描述
        
        private void OnCollisionEnter(Collision collision)
        {
            // 检查是否被坦克碰撞
            if (collision.gameObject.CompareTag("Player"))
            {
                // 创建道具数据
                PropItem prop = new PropItem(
                    propId,
                    propName,
                    spriteName,
                    description
                );
                
                // 添加到背包
                PropManager.Instance.AddProp(prop);
                
                // 播放收集效果
                PlayCollectEffect();
                
                // 销毁道具对象
                Destroy(gameObject);
            }
        }
        
        private void PlayCollectEffect()
        {
            // 可以在这里添加收集效果，如音效、粒子等
            // 例如：
            // AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
    }
}