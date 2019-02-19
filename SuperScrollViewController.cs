using LuaInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SuperScrollViewController : MonoBehaviour {

    private UIVertical mList;
    private UIHorizontal mList2;
    List<int> intlist = new List<int>();

    LuaFunction _refesh;

    public void SetRefeshDelegate(LuaFunction _refesh)
    {
        this._refesh = _refesh;
    }

    public void NewScroll(GameObject _perfab, int _count , LuaFunction _lunc) {
        this.SetRefeshDelegate(_lunc);
        if (transform.GetComponent<UIVertical>() != null)
        {
            mList = transform.GetComponent<UIVertical>();
            mList.InitData(_perfab);
            mList.InitList(_count, (item, index) =>
            {
                //子物体属性赋值
                if (this._refesh != null)
                {
                    this._refesh.Call(item, index);
                }
            });
        }

        if (transform.GetComponent<UIHorizontal>() != null)
        {
            mList2 = transform.GetComponent<UIHorizontal>();
            mList2.InitData(_perfab);
            mList2.InitList(_count, (item, index) =>
            {   
                //子物体属性赋值
                if (this._refesh != null)
                {
                    this._refesh.Call(item, index);
                }
            });
        }
    }

    public void ContinueScroll(int _count)
    {
        if (transform.GetComponent<UIVertical>() != null)
        {
            mList = transform.GetComponent<UIVertical>();
            mList.Refresh(_count, (item, index) =>
            {   //子物体属性赋值
                if (this._refesh != null)
                {
                    this._refesh.Call(item, index);
                }
            });
        }

        if (transform.GetComponent<UIHorizontal>() != null)
        {
            mList2 = transform.GetComponent<UIHorizontal>();
            mList2.Refresh(_count, (item, index) =>
            {   //子物体属性赋值
                if (this._refesh != null)
                {
                    this._refesh.Call(item, index);
                }
            });
        }
    }

    private void OnDestroy()
    {
        //清理事件
        if (this._refesh != null)
        {
            this._refesh.Dispose();
        }
    }
}
