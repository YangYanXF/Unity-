using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class GraphDataNode
{
    public string text;
    public List<int> next = new List<int>();
    public List<int> pre = new List<int>();
    public List<string> choices = new List<string>();
    private string guid;

    public string GUID
    {
        get { return guid; }
        set { guid = value; }
    }

    public int preLength => pre.Count;
    public int nextLength => next.Count;
    public bool isChoiceNode => nextLength > 1 ? true : false;
    public bool isEnd => nextLength == 0 ? true : false;
}
