using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SuperScrollView;

namespace Complete
{
    public class PropManager : MonoBehaviour
    {
        private static PropManager _instance;
        private static bool _shuttingDown = false;
        
        public static PropManager Instance
        {
            get
            {
                // 如果正在关闭应用，或者在场景切换过程中，不创建新实例
                if (_shuttingDown)
                {
                    Debug.LogWarning("[PropManager] Instance被请求，但应用正在关闭");
                    return null;
                }
                
                if (_instance == null)
                {
                    // 先查找场景中已有的实例
                    _instance = FindObjectOfType<PropManager>();
                    
                    // 如果场景中没有实例，创建一个新的
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PropManager");
                        _instance = go.AddComponent<PropManager>();
                        DontDestroyOnLoad(go);
                        Debug.Log("[PropManager] 创建新实例");
                    }
                }
                return _instance;
            }
        }

        // 所有收集的道具
        private List<PropItem> collectedProps = new List<PropItem>();
        
        // 转换后的 DataSourceMgr 实例
        private DataSourceMgr<ItemData> _dataSourceMgr;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log("[PropManager] 销毁重复实例");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _shuttingDown = false;
            
            // 从PlayerPrefs加载已保存的道具
            LoadPropsFromLocal();
        }
        
        private void OnApplicationQuit()
        {
            _shuttingDown = true;
        }
        
        private void OnDestroy()
        {
            // 检查如果销毁的是当前的单例实例
            if (_instance == this)
            {
                _shuttingDown = true;
                Debug.Log("[PropManager] 单例实例被销毁");
            }
        }
        
        // 添加道具到背包
        public void AddProp(PropItem prop)
        {
            Debug.Log($"[PropManager] 添加道具: {prop.propName}, ID: {prop.id}");
            
            // 检查是否已经存在相同ID的道具，避免重复添加
            bool isDuplicate = collectedProps.Any(p => p.id == prop.id);
            
            if (isDuplicate)
            {
                Debug.Log($"[PropManager] 道具已存在，忽略重复添加: {prop.propName}, ID: {prop.id}");
                // return;
            }
            
            // 添加新道具
            collectedProps.Add(prop);
            SavePropsToLocal(); // 保存到本地
            
            // 重置数据源
            _dataSourceMgr = null;
            
            // 通知UI更新
            if (OnPropCollected != null)
                OnPropCollected.Invoke(prop);
        }
        
        // 获取所有收集的道具
        public List<PropItem> GetAllProps()
        {
            return collectedProps;
        }
        
        // 获取道具列表作为 DataSource
        public DataSourceMgr<ItemData> GetDataSource()
        {
            if (_dataSourceMgr == null)
            {
                _dataSourceMgr = new DataSourceMgr<ItemData>(0);
                
                foreach (PropItem prop in collectedProps)
                {
                    ItemData itemData = new ItemData();
                    itemData.mName = prop.propName;
                    itemData.mIcon = prop.spriteName;
                    itemData.mDesc = prop.description;
                    _dataSourceMgr.AppendData(itemData);
                }
            }
            return _dataSourceMgr;
        }
        
        // 重新加载道具数据（用于清空背包）
        public void ReloadProps()
        {
            // 清空当前数据
            collectedProps.Clear();
            _dataSourceMgr = null;
            
            // 从本地重新加载（如果已被清空，将加载空数据）
            LoadPropsFromLocal();
            
            // 触发道具更新事件，让UI刷新
            if (OnPropsReloaded != null)
                OnPropsReloaded.Invoke();
        }
        
        // 道具收集事件
        public delegate void PropCollectedHandler(PropItem prop);
        public event PropCollectedHandler OnPropCollected;
        
        // 道具重新加载事件（用于通知UI刷新）
        public delegate void PropsReloadedHandler();
        public event PropsReloadedHandler OnPropsReloaded;
        
        // 保存道具到本地
        private void SavePropsToLocal()
        {
            // 将道具列表转换为JSON
            string propsJson = JsonUtility.ToJson(new SerializableProps { props = collectedProps.ToArray() });
            PlayerPrefs.SetString("CollectedProps", propsJson);
            PlayerPrefs.Save();
        }
        
        // 从本地加载道具
        private void LoadPropsFromLocal()
        {
            if (PlayerPrefs.HasKey("CollectedProps"))
            {
                string propsJson = PlayerPrefs.GetString("CollectedProps");
                SerializableProps loadedProps = JsonUtility.FromJson<SerializableProps>(propsJson);
                if (loadedProps != null && loadedProps.props != null)
                {
                    collectedProps = new List<PropItem>(loadedProps.props);
                }
            }
        }
        
        // 用于序列化的辅助类
        [System.Serializable]
        private class SerializableProps
        {
            public PropItem[] props;
        }
    }
}