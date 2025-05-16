using UnityEngine;

namespace Complete
{
    [System.Serializable]
    public class PropItem
    {
        public string id;          // 道具唯一ID
        public string propName;    // 道具名称
        public string spriteName;  // 对应的Sprite资源名称
        public string description; // 道具描述
        
        public PropItem(string id, string propName, string spriteName, string description = "")
        {
            this.id = id;
            this.propName = propName;
            this.spriteName = spriteName;
            this.description = description;
        }
    }
}