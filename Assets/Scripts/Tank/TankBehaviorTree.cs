using UnityEngine;
using BehaviorDesigner.Runtime;

namespace Complete
{
    public class TankBehaviorTree : MonoBehaviour
    {
        public GameObject player;
        public float detectRange = 15f;
        public float attackRange = 12f;
        public float patrolRadius = 10f;
        private BehaviorTree behaviorTree;

        void Start()
        {
            // 获取行为树组件
            behaviorTree = GetComponent<BehaviorTree>();
            
            // 如果没有找到行为树组件，添加一个
            if (behaviorTree == null)
            {
                behaviorTree = gameObject.AddComponent<BehaviorTree>();
                behaviorTree.ExternalBehavior = CreateBehaviorTree();
            }

            // 如果没有指定玩家，尝试查找
            if (player == null)
            {
                player = GameObject.FindWithTag("Player");
            }

            // 设置共享变量
            if (behaviorTree.GetVariable("Player") != null)
                behaviorTree.SetVariableValue("Player", player);
            
            if (behaviorTree.GetVariable("DetectRange") != null)
                behaviorTree.SetVariableValue("DetectRange", detectRange);
            
            if (behaviorTree.GetVariable("AttackRange") != null)
                behaviorTree.SetVariableValue("AttackRange", attackRange);
            
            if (behaviorTree.GetVariable("PatrolRadius") != null)
                behaviorTree.SetVariableValue("PatrolRadius", patrolRadius);
        }

        // 创建行为树资源的函数
        private ExternalBehavior CreateBehaviorTree()
        {
            // 实际使用时，应该通过编辑器创建行为树资源并通过Inspector引用
            Debug.LogWarning("No behavior tree asset assigned! Please create a behavior tree in the Behavior Designer editor.");
            return null;
        }
    }
}