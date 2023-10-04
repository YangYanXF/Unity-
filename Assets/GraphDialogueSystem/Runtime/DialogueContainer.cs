using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class DialogueContainer : ScriptableObject
{
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
    public List<DialogueNodeData> DialogueNodeData = new List<DialogueNodeData>();
    public List<GraphDataNode> DialogueGraphData = new List<GraphDataNode>();

    /// <summary>
    /// 根据 NodeLinks 和 DialogueNodeData 创建图数据结构
    /// </summary>
    public void InitGraphData()
    {
        NodeLinkData firstEdge = NodeLinks.First();
        //获取第一个结点
        DialogueNodeData firstNodeData = DialogueNodeData.Where(x => x.guid == firstEdge.targetNodeGuid).First();
        GraphDataNode graphNode = new GraphDataNode();
        DialogueGraphData.Add(graphNode);
        InitGraphNode(graphNode, firstNodeData);
    }

    /// <summary>
    /// 获取第一个对话结点的对话数据
    /// </summary>
    public GraphDataNode GetHead()
    {
        if (DialogueGraphData != null)
        {
            return DialogueGraphData[0];
        }

        Debug.LogWarning("DialogueGraphData is null");
        return null;
    }

    /// <summary>
    /// 获得 node 结点的下一个结点，如果有分支，则 index 代表对应的分支结点
    /// </summary>
    public GraphDataNode GetNext(GraphDataNode node, int index = 0)
    {
        if (index < 0 || index > node.nextLength)
        {
            Debug.LogWarning($"DialogueContainer: The index of next Dialogue : {index} is out of range!");
            return null;
        }

        return DialogueGraphData[node.next[index]];
    }

    /// <summary>
    /// 获取 node 结点的上一个结点，如果有分支，则 index 代表对应的分支结点
    /// </summary>
    /// <param name="node"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public GraphDataNode GetPre(GraphDataNode node, int index = 0)
    {
        if (index < 0 || index > node.preLength)
        {
            Debug.LogWarning($"DialogueContainer: The index of pre Dialogue : {index} is out of range!");
            return null;
        }

        return DialogueGraphData[node.pre[index]];
    }

    /// <summary>
    /// 递归初始化数据
    /// </summary>
    private void InitGraphNode(GraphDataNode currentGraphNode, DialogueNodeData currentNodeData)
    {
        //初始化当前节点
        currentGraphNode.text = currentNodeData.dialogueText;
        currentGraphNode.GUID = currentNodeData.guid;

        //找到以当前结点为输出结点的边
        var edges = NodeLinks.Where(x => x.baseNodeGuid == currentNodeData.guid).ToList();

        if (edges.Count > 1)
        {
            foreach (var edge in edges)
            {
                currentGraphNode.choices.Add(edge.portName);
            }
        }

        foreach (var edge in edges)
        {
            //查找当前边对应的输入结点
            DialogueNodeData nodeData = DialogueNodeData.Where(x => x.guid == edge.targetNodeGuid).First();
            GraphDataNode graphNode;

            //查找该条边对应的下一个结点是否已经在 List 中，如果不在则创建一个对象，并加入表中，否则直接返回该结点
            int index = DialogueGraphData.FindIndex(x => x.GUID == nodeData.guid);
            if (index == -1)
            {
                graphNode = new GraphDataNode();
                DialogueGraphData.Add(graphNode);
            }
            else
                graphNode = DialogueGraphData[index];

            //将两个结点连起来
            int nextIndex = DialogueGraphData.IndexOf(graphNode);
            int currentIndex = DialogueGraphData.IndexOf(currentGraphNode);
            currentGraphNode.next.Add(nextIndex);
            graphNode.pre.Add(currentIndex);

            //递归初始化结点
            InitGraphNode(graphNode, nodeData);
        }
    }
}