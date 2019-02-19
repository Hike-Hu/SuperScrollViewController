using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 纵向列表
/// </summary>
public class UIVertical : MonoBehaviour
{
    // 外部基础参数设置  
    public int itemWidth;            //单元格宽
    public int itemHeight;           //单元格高
    public int columnCount;      // 显示列数
    public int rowCount;             // 显示行数
    public int offsetX = 0; //列间隔
    public int offsetY = 0; //行间隔 
    public int Paddind_x = 0;
    public int Paddind_y = 0;

    //基础设置
    private ScrollRect scrollRect;
    private RectTransform itemParent;
    private GameObject itemObj;          //克隆体
    private Vector2 m_maskSize;     //遮罩大小

    //根据参数来计算
    private int m_createCount;            //创建的数量
    private int m_rectWidth;           //列表宽度
    private int m_rectHeigh;           //列表高度
    private int m_listCount;              //列表总的需要显示的数量，外部给
    private int rowSum;                 //总共多少行
    //private int columnSum;                 //总共多少列
    private int m_showCount;              //当前实际显示的数量(小于或等于createCount)
    private int lastStartIndex = 0;     //记录上次的初始序号
    private int m_startIndex = 0;     //显示开始序号
    private int m_endIndex = 0;           //显示结束序号
    private Dictionary<int, Transform> dic_itemIndex = new Dictionary<int, Transform>();      //item对应的序号
    private Vector3 curItemParentPos = Vector3.zero;
    private Transform m_item;

    public delegate void UpdateListItemEvent(Transform item, int index);
    private UpdateListItemEvent m_updateItem = null;


    /// <summary>
    /// 初始化数据 item长宽，列数和行
    /// </summary>
    /// <param item宽="width"></param>
    /// <param item长="heigh"></param>
    /// <param 1="column"></param>
    /// <param 一个列表最多能显示多少行（元素）="row"></param>
    public void Init(int width, int heigh, int column, int row, GameObject perfab)
    {
        itemWidth = width;
        itemHeight = heigh;
        columnCount = column;
        rowCount = row;
        //rowCount = row + 2;
        InitData(perfab);
    }

    /// <summary>
    /// 初始化列表 item长宽，列数和行 
    /// </summary>
    public void InitData(GameObject _perfab)
    {
        m_createCount = columnCount * rowCount;
        if (m_createCount <= 0)
        {
            Debug.LogError("横纵不能为0！");
            return;
        }

        scrollRect = transform.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnValueChange);
        }
        m_rectWidth = columnCount * itemWidth;

        itemParent = transform.Find("Mask/List").GetComponent<RectTransform>();
        itemParent.pivot = new Vector2(0, 1);
        itemObj = _perfab;
        RectTransform itemRec = itemObj.GetComponent<RectTransform>();
        itemRec.anchorMin = new Vector2(0, 1);
        itemRec.anchorMax = new Vector2(0, 1);
        itemRec.pivot = new Vector2(0, 1);

        m_maskSize = GetComponent<RectTransform>().sizeDelta;
    }

    ////设置元素之间的间距 spacing ：SetOffset()
    public void SetOffset(int x, int y)
    {
        offsetX = x;
        offsetY = y;
        m_rectWidth = (columnCount - 1) * (itemWidth + offsetX);
    }

    /// <summary>
    /// 刷新赋值列表 回滚到顶部
    /// </summary>
    /// <param 列表的元素的最大个数="count"></param>
    /// <param 委托:进行 单个元素的赋值 = "updateItem"></param>
    public void InitList(int count, UpdateListItemEvent updateItem)
    {
        m_listCount = count;                  //记录有多少个item
        m_updateItem = updateItem;
        itemParent.transform.localPosition = new Vector2(Paddind_x, Paddind_y);
        rowSum = count / columnCount + (count % columnCount > 0 ? 1 : 0);      //计算有多少行，用于计算出总高度
        m_rectHeigh = Mathf.Max(0, rowSum * itemHeight + (rowSum - 1) * offsetY);

        itemParent.sizeDelta = new Vector2(m_rectWidth, m_rectHeigh + 50 );
        m_showCount = Mathf.Min(count, m_createCount);     //显示item的数量
        m_startIndex = 0;
        dic_itemIndex.Clear();

        for (int i = 0; i < m_showCount; i++)
        {
            Transform item = GetItem(i);
            SetItem(item, i);
        }
        ShowListCount(itemParent, m_showCount);         //显示多少个
    }

    /// <summary>
    /// 生成列表 不回滚,继续往下浏览
    /// </summary>
    /// <param 列表的元素的最大个数="count"></param>
    /// <param 委托:进行 单个元素的赋值 = "updateItem"></param>
    public void Refresh(int count, UpdateListItemEvent updateItem)
    {
        m_updateItem = updateItem;
        rowSum = count / columnCount + (count % columnCount > 0 ? 1 : 0);
        m_rectHeigh = Mathf.Max(0, rowSum * itemHeight + (rowSum - 1) * offsetY);
        itemParent.sizeDelta = new Vector2(m_rectWidth, m_rectHeigh + 50);
        m_listCount = count;
        m_showCount = Mathf.Min(count, m_createCount);     //显示item的数量
        dic_itemIndex.Clear();
        if (count == 0)
        {
            ShowListCount(itemParent, m_showCount);
            return;
        }
        //计算起始的终止序号
        //--如果数量小于遮罩正常状态下能显示的总量
        if (count <= m_createCount)
        {
            m_startIndex = 0;
            m_endIndex = count - 1;
        }
        else
        {
            m_startIndex = GetStartIndex(itemParent.localPosition.y);
            if (m_startIndex + m_createCount >= count)
            {

                m_startIndex = count - m_createCount;
                m_endIndex = count - 1;
            }
            else
            {
                m_endIndex = m_startIndex + m_createCount - 1;
            }
        }
        lastStartIndex = m_startIndex;
        if (m_endIndex < m_startIndex)
        {
            Debug.LogError("列表有问题！");
            return;
        }
        for (int i = m_startIndex; i <= m_endIndex; i++)
        {
            Transform item = GetItem(i - m_startIndex);
            SetItem(item, i);
        }
        ShowListCount(itemParent, m_showCount);
    }

    /// <summary>
    /// 创建item 有就拿来用，没有就创建
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private Transform GetItem(int index)
    {
        Transform item = null;
        if (index < itemParent.childCount)
            item = itemParent.GetChild(index);
        else
            item = ((GameObject)GameObject.Instantiate(itemObj.gameObject)).transform;
        item.name = index.ToString();
        item.SetParent(itemParent);
        item.localScale = Vector3.one;
        return item;
    }

    /// <summary>
    /// 刷新item对应数据信息
    /// </summary>
    /// <param name="item"></param>
    /// <param name="index"></param>
    private void SetItem(Transform item, int index)
    {
        dic_itemIndex[index] = item;
        item.localPosition = GetPos(index);
        if (m_updateItem != null)
            m_updateItem(item, index);
    }

    /// <summary>
    /// item对应位置
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private Vector2 GetPos(int index)
    {
        int spread = 0;
        return new Vector2(index % columnCount * (itemWidth + offsetX), -index / columnCount * (itemHeight + offsetY) - spread);
    }

    // 获取起始序列号
    private int GetStartIndex(float y)
    {
        int _spreadHeigh =  0;       
        if (y <= (itemHeight + _spreadHeigh))
            return 0;
        float scrollHeigh = gameObject.GetComponent<RectTransform>().sizeDelta.y;
        if (y >= (itemParent.sizeDelta.y - scrollHeigh - _spreadHeigh))        //拉到底部了
        {
            if (m_listCount <= m_createCount)
                return 0;
            else
                return m_listCount - m_createCount;
        }
        return ((int)((y - _spreadHeigh) / (itemHeight + offsetY)) + ((y - _spreadHeigh) % (itemHeight + offsetY) > 0 ? 1 : 0) - 1) * columnCount;
    }


    //显示子物体的数量
    private void ShowListCount(Transform trans, int num)
    {
        if (trans.childCount < num)
            return;
        for (int i = 0; i < num; i++)
        {
            trans.GetChild(i).gameObject.SetActive(true);
        }
        for (int i = num; i < trans.childCount; i++)
        {
            trans.GetChild(i).gameObject.SetActive(false);
        }
    }

    List<int> newIndexList = new List<int>();
    List<int> changeIndexList = new List<int>();
    //列表位置刷新
    private void OnValueChange(Vector2 pos)
    {
        curItemParentPos = itemParent.localPosition;
        if (m_listCount <= m_createCount)
            return;
        m_startIndex = GetStartIndex(itemParent.localPosition.y);
        if (m_startIndex + m_createCount >= m_listCount)
        {
            m_startIndex = m_listCount - m_createCount;
            m_endIndex = m_listCount - 1;
        }
        else
        {
            m_endIndex = m_startIndex + m_createCount - 1;
        }
        if (m_startIndex == lastStartIndex)
            return;
        lastStartIndex = m_startIndex;
        newIndexList.Clear();
        changeIndexList.Clear();
        for (int i = m_startIndex; i <= m_endIndex; i++)
        {
            newIndexList.Add(i);
        }

        var e = dic_itemIndex.GetEnumerator();
        while (e.MoveNext())
        {
            int index = e.Current.Key;
            if (index >= m_startIndex && index <= m_endIndex)
            {
                if (newIndexList.Contains(index))
                    newIndexList.Remove(index);
                continue;
            }
            else
            {
                changeIndexList.Add(e.Current.Key);
            }
        }

        for (int i = 0; i < newIndexList.Count && i < changeIndexList.Count; i++)
        {
            int oldIndex = changeIndexList[i];
            int newIndex = newIndexList[i];
            if (newIndex >= 0 && newIndex < m_listCount)
            {
                m_item = dic_itemIndex[oldIndex];
                dic_itemIndex.Remove(oldIndex);
                SetItem(m_item, newIndex);
            }
        }

    }
}
