using System.Collections;
using System.Collections.Generic;
using SuperScrollView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Complete
{
    public class PackageView : MonoBehaviour
    {
        class DotElem
        {
            public GameObject mDotElemRoot;
            public GameObject mDotNormal;
            public GameObject mDotSelect;
        }

        public LoopListView2 mLoopListView;
        public int mTotalDataCount = 100;

        public RectTransform mParentView;
        public RectTransform mDotsRoot;
        public RectTransform mDotTemplate;

        private DataSourceMgr<ItemData> mDataSourceMgr;

        private Button mSetCountButton;
        private InputField mSetCountInput;
        private Button mScrollToButton;
        private InputField mScrollToInput;
        private Button mAddButton;
        private Button mBackButton;

        private int mPageCount = 10;
        private int mMaxPageCount = 10;
        private int mCountEachPage = 16;
        private int mMaxItemCount = 0;

        private List<DotElem> mDotElemList = new List<DotElem>();
        private List<RectTransform> mDotRectList = new List<RectTransform>();

        void Start()
        {
            mMaxItemCount = mMaxPageCount * mCountEachPage;
            if (mTotalDataCount > mMaxItemCount)
            {
                mTotalDataCount = mMaxItemCount;
            }

            // 使用 PropManager 作为数据源
            InitPropDataSource();
            
            UpdatePageCount(mDataSourceMgr.TotalItemCount);
            InitAllDots();
            
            LoopListViewInitParam initParam = LoopListViewInitParam.CopyDefaultInitParam();
            initParam.mSnapVecThreshold = 99999;
            mLoopListView.mOnBeginDragAction = OnBeginDrag;
            mLoopListView.mOnDragingAction = OnDraging;
            mLoopListView.mOnEndDragAction = OnEndDrag;
            mLoopListView.mOnSnapNearestChanged = OnSnapNearestChanged;
            mLoopListView.InitListView(mPageCount,onGetItemByIndex,initParam);
            
            // 订阅 PropManager 道具收集事件
            PropManager.Instance.OnPropCollected += OnPropCollected;
            PropManager.Instance.OnPropsReloaded += OnPropsReloaded;
        }
        
        // 初始化道具数据源
        private void InitPropDataSource()
        {
            // 从 PropManager 获取数据源
            if (PropManager.Instance != null)
            {
                mDataSourceMgr = PropManager.Instance.GetDataSource();
            }
            // 如果没有道具，创建一个空数据源
            if (mDataSourceMgr == null || mDataSourceMgr.TotalItemCount == 0)
            {
                mDataSourceMgr = new DataSourceMgr<ItemData>(0);
            }
        }
        
        // 当新道具被收集时，更新UI
        private void OnPropCollected(PropItem prop)
        {
            // 如果组件已被销毁或场景正在关闭，不处理事件
            if (this == null || !gameObject.activeInHierarchy)
                return;
                
            // 重新获取数据源
            InitPropDataSource();
            
            // 更新UI
            UpdatePageCount(mDataSourceMgr.TotalItemCount);
            mLoopListView.SetListItemCount(mPageCount, false);
            mLoopListView.RefreshAllShownItem();
            ResetDots();
        }
        
        private void OnDestroy()
        {
            // 移除事件订阅
            if (PropManager.Instance != null)
            {
                PropManager.Instance.OnPropCollected -= OnPropCollected;
                PropManager.Instance.OnPropsReloaded -= OnPropsReloaded;
            }
        }

        // 当道具重新加载时（清空背包）更新UI
        private void OnPropsReloaded()
        {
            // 如果组件已被销毁或场景正在关闭，不处理事件
            if (this == null || !gameObject.activeInHierarchy)
                return;
                
            // 重新获取数据源
            InitPropDataSource();
            
            // 更新UI
            UpdatePageCount(mDataSourceMgr.TotalItemCount);
            mLoopListView.SetListItemCount(mPageCount, false);
            mLoopListView.RefreshAllShownItem();
            ResetDots();
        }

        LoopListViewItem2 onGetItemByIndex(LoopListView2 listView, int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= mPageCount)
            {
                return null;
            }

            LoopListViewItem2 item = listView.NewListViewItem("ItemPrefab");
            PageViewItem itemScript = item.GetComponent<PageViewItem>();
            if (item.IsInitHandlerCalled == false)
            {
                item.IsInitHandlerCalled = true;
                itemScript.Init();
            }
            List<PageViewItemElem> elemList = itemScript.mElemItemList;
            int count = elemList.Count;
            int picBeginIndex = pageIndex * count;
            int i = 0;
            for (;  i< count; ++i)
            {
                ItemData itemData = mDataSourceMgr.GetItemDataByIndex(picBeginIndex + i);
                if (itemData == null)
                {
                    break;
                }
                PageViewItemElem elem = elemList[i];
                elem.mRootObj.SetActive(true);
                elem.mIcon.sprite = PropResManager.Get.GetSpriteByName(itemData.mIcon);
                elem.mName.text = itemData.mName;
            }

            if (i < count)
            {
                for (; i < count; ++i)
                {
                    elemList[i].mRootObj.SetActive(false);
                }
            }

            return item;
        }

        void UpdatePageCount(int itemCount)
        {
            int tmpPageCount = itemCount/mCountEachPage;
            if (tmpPageCount > mMaxPageCount)
            {
                tmpPageCount = mMaxPageCount;
            }
            else
            {
                if (itemCount % mCountEachPage > 0)
                {
                    tmpPageCount++;
                }
            }

            mPageCount = tmpPageCount;
        }

        void InitAllDots()
        {
            mDotTemplate.gameObject.SetActive(false);
            CreateDots(mPageCount);
        }

        void CreateDots(int count)
        {
            // 先清空现有的点
            foreach (var dotElem in mDotElemList)
            {
                if (dotElem.mDotElemRoot != null)
                {
                    GameObject.Destroy(dotElem.mDotElemRoot);
                }
            }
            mDotElemList.Clear();
            
            // 创建新的点，确保顺序正确
            for (int i = 0; i < count; i++)
            {
                CreateOneDot(mDotsRoot, mDotTemplate);
            }
            
            // 确保所有的点显示正确
            if (mLoopListView != null && mLoopListView.CurSnapNearestItemIndex >= 0)
            {
                RefreshAllDots(mLoopListView.CurSnapNearestItemIndex);
            }
            else if (mDotElemList.Count > 0)
            {
                // 默认第一页选中
                RefreshAllDots(0);
            }
        }

        void CreateOneDot(RectTransform rectParent, RectTransform rectTemplate)
        {
            int dotIndex = mDotElemList.Count;
            GameObject obj = GameObject.Instantiate(rectTemplate.gameObject, rectParent);
            obj.gameObject.name = "Dot" + dotIndex;
            obj.gameObject.SetActive(true);
            RectTransform rectTrans = obj.GetComponent<RectTransform>();
            rectTrans.localScale = Vector3.one;
            rectTrans.localEulerAngles = Vector3.zero;
            rectTrans.anchoredPosition3D = Vector3.zero;
            rectTrans.SetAsLastSibling();

            DotElem elem = new DotElem();
            elem.mDotElemRoot = obj;
            elem.mDotNormal = obj.transform.Find("DotNormal").gameObject;
            elem.mDotSelect = obj.transform.Find("DotSelect").gameObject;
            mDotElemList.Add(elem);
            
            ClickEventListener listener = ClickEventListener.Get(elem.mDotElemRoot);
            listener.SetClickEventHandler(delegate {OnDotClicked(dotIndex);});
        }

        void OnDotClicked(int index)
        {
            int curNearestItemIndex = mLoopListView.CurSnapNearestItemIndex;
            if (curNearestItemIndex < 0 || curNearestItemIndex >= mPageCount)
            {
                return;
            }

            if (index == curNearestItemIndex)
            {
                return;
            }
            mLoopListView.SetSnapTargetItemIndex(index);
        }

        void UpdateAllDots()
        {
            int curNearestItemIndex = mLoopListView.CurSnapNearestItemIndex;
            if (curNearestItemIndex < 0 || curNearestItemIndex >= mPageCount)
            {
                return;
            }

            int count = mDotElemList.Count;
            if (curNearestItemIndex >= count)
            {
                return;
            }
            RefreshAllDots(curNearestItemIndex);
        }

        void RefreshAllDots(int selectedIndex)
        {
            for (int i = 0; i < mDotElemList.Count; ++i)
            {
                DotElem elem = mDotElemList[i];
                if (i != selectedIndex)
                {
                    elem.mDotNormal.SetActive(true);
                    elem.mDotSelect.SetActive(false);
                }
                else
                {
                    elem.mDotNormal.SetActive(false);
                    elem.mDotSelect.SetActive(true);
                }
            }
        }

        void ResetDots()
        {
            // 直接重新创建所有点，而不是添加或移除
            // 这样可以确保点的顺序正确
            
            // 先清除现有的所有点
            foreach (var dotElem in mDotElemList)
            {
                if (dotElem.mDotElemRoot != null)
                {
                    GameObject.Destroy(dotElem.mDotElemRoot);
                }
            }
            mDotElemList.Clear();
            
            // 重新创建所有点
            CreateDots(mPageCount);
            
            // 更新选中状态
            int curNearestItemIndex = mLoopListView.CurSnapNearestItemIndex;
            if (curNearestItemIndex >= 0 && curNearestItemIndex < mPageCount)
            {
                RefreshAllDots(curNearestItemIndex);
            }
            else if (mDotElemList.Count > 0)
            {
                // 默认选中第一页
                RefreshAllDots(0);
            }
        }

        void OnSnapNearestChanged(LoopListView2 listView, LoopListViewItem2 item)
        {
            UpdateAllDots();
        }

        void OnBeginDrag()
        {
            
        }
        void OnDraging()
        {
        }

        void OnEndDrag()
        {
            float vec = mLoopListView.ScrollRect.velocity.x;
            int curNearestItemIndex = mLoopListView.CurSnapNearestItemIndex;

            LoopListViewItem2 item = mLoopListView.GetShownItemByItemIndex(curNearestItemIndex);
            if (item == null)
            {
                mLoopListView.ClearSnapData();
                return;
            }

            if (Mathf.Abs(vec) < 50f)
            {
                mLoopListView.SetSnapTargetItemIndex(curNearestItemIndex);
                return;
            }

            Vector3 pos = mLoopListView.GetItemCornerPosInViewPort(item, ItemCornerEnum.LeftTop);
            if (pos.x > 0)
            {
                if (vec > 0)
                {
                    mLoopListView.SetSnapTargetItemIndex(curNearestItemIndex - 1);
                }
                else
                {
                    mLoopListView.SetSnapTargetItemIndex(curNearestItemIndex);
                }
            }
            else if(pos.x < 0)
            {
                if (vec > 0)
                {
                    mLoopListView.SetSnapTargetItemIndex(curNearestItemIndex);
                }
                else
                {
                    mLoopListView.SetSnapTargetItemIndex(curNearestItemIndex + 1);
                }
            }
            else
            {
                if (vec > 0)
                {
                    mLoopListView.SetSnapTargetItemIndex(curNearestItemIndex - 1);
                }
                else
                {
                    mLoopListView.SetSnapTargetItemIndex(curNearestItemIndex + 1);
                }
            }
        }

        void OnSetCountButtonClicked()
        {
            int count = 0;
            if (int.TryParse(mSetCountInput.text, out count) == false)
            {
                return;
            }

            if (count < 0)
            {
                return;
            }

            if (count > mMaxItemCount)
            {
                return;
            }

            if (count == mDataSourceMgr.TotalItemCount)
            {
                return;
            }
            mDataSourceMgr.SetDataTotalCount(count);
            UpdatePageCount(mDataSourceMgr.TotalItemCount);
            mLoopListView.SetListItemCount(mPageCount,false);
            mLoopListView.RefreshAllShownItem();
            ResetDots();
        }

        void OnScrollToButtonClicked()
        {
            int itemIndex = 0;
            if (int.TryParse(mScrollToInput.text, out itemIndex) == false)
            {
                return;
            }

            if ((itemIndex < 0) || (itemIndex >= mDataSourceMgr.TotalItemCount))
            {
                return;
            }

            int tmpItemIndex = itemIndex + 1;
            int tmpPageIndex = tmpItemIndex / mCountEachPage;
            if (tmpItemIndex % mCountEachPage > 0)
            {
                tmpPageIndex++;
            }

            if (tmpPageIndex > 0)
            {
                tmpPageIndex--;
            }
            mLoopListView.MovePanelToItemIndex(tmpPageIndex,0);
            mLoopListView.FinishSnapImmediately();
        }

        void OnAddButtonClicked()
        {
            if (mDataSourceMgr.TotalItemCount >= mMaxItemCount)
            {
                return;
            }

            ItemData newData = mDataSourceMgr.InsertData(mDataSourceMgr.TotalItemCount);
            newData.mDesc = newData.mDesc + " [New]";
            UpdatePageCount(mDataSourceMgr.TotalItemCount);
            mLoopListView.SetListItemCount(mPageCount,false);
            mLoopListView.RefreshAllShownItem();
            ResetDots();
        }

        void OnBackButtonClicked()
        {
            ButtonPanelMenuList.BackToMainMenu();
        }

    }
}