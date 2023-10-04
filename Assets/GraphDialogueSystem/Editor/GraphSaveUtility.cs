using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private List<Edge> Edges => _graphView.edges.ToList();
    private List<DialogueNode> Nodes => _graphView.nodes.ToList().Cast<DialogueNode>().ToList();

    private DialogueContainer _containerCache;
    private DialogueGraphView _graphView;

    public static GraphSaveUtility GetInstance(DialogueGraphView graphView)
    {
        return new GraphSaveUtility
        {
            _graphView = graphView
        };
    }

    public void SaveGraph(string fileName)
    {
        var dialogueContainerObject = ScriptableObject.CreateInstance<DialogueContainer>();
        //保存数据
        if (!SaveNodes(fileName, dialogueContainerObject)) return;

        dialogueContainerObject.InitGraphData();

        //如果对应文件夹不存在，则创建该文件夹
        if (!AssetDatabase.IsValidFolder("Assets/Resources/DialogueData"))
            AssetDatabase.CreateFolder("Assets/Resources", "DialogueData");

        UnityEngine.Object loadedAsset = AssetDatabase.LoadAssetAtPath($"Assets/Resources/DialogueData/{fileName}.asset", typeof(DialogueContainer));

        //如果对应文件不存在，则创建文件，否则更新该文件中数据
        if (loadedAsset == null || !AssetDatabase.Contains(loadedAsset))
        {
            AssetDatabase.CreateAsset(dialogueContainerObject, $"Assets/Resources/DialogueData/{fileName}.asset");
        }
        else
        {
            DialogueContainer container = loadedAsset as DialogueContainer;
            container.NodeLinks = dialogueContainerObject.NodeLinks;
            container.DialogueNodeData = dialogueContainerObject.DialogueNodeData;
            container.DialogueGraphData = dialogueContainerObject.DialogueGraphData;
            EditorUtility.SetDirty(container);
        }

        AssetDatabase.SaveAssets();
    }

    private bool SaveNodes(string fileName, DialogueContainer dialogueContainerObject)
    {
        // 如果没有任何边则返回。
        if (!Edges.Any()) return false;

        //对于边来说，可能会公用多个输入端口，但只对应一个输出结点，所以我们使用输出结点作区分
        //并且如果一个输出端口连接着一个输入端口，则可以视为有效的链接
        var connectedSockets = Edges.Where(x => x.input.node != null).ToArray();

        //保存端口之间的连线数据
        for (var i = 0; i < connectedSockets.Count(); i++)
        {
            var outputNode = (connectedSockets[i].output.node as DialogueNode);
            var inputNode = (connectedSockets[i].input.node as DialogueNode);
            //output ---> input
            dialogueContainerObject.NodeLinks.Add(new NodeLinkData
            {
                baseNodeGuid = outputNode.GUID,
                portName = connectedSockets[i].output.portName,
                targetNodeGuid = inputNode.GUID
            });
        }

        //保存除入口节点外的所有结点的数据
        foreach (var node in Nodes.Where(node => !node.EntyPoint))
        {
            dialogueContainerObject.DialogueNodeData.Add(new DialogueNodeData
            {
                guid = node.GUID,
                dialogueText = node.DialogueText,
                position = node.GetPosition().position
            });
        }

        return true;
    }

    /// <summary>
    /// 加载 对话视图，但只初始化
    /// </summary>
    public void LoadGraph(string fileName)
    {
        _containerCache = Resources.Load<DialogueContainer>("DialogueData/" + fileName);
        if (_containerCache == null)
        {
            EditorUtility.DisplayDialog("File Not Found", "Target Narrative Data does not exist!", "OK");
            return;
        }

        ClearGraph();
        GenerateDialogueNodes();
        ConnectDialogueNodes();
    }

    /// <summary>
    /// 设置当前窗口的入口节点的 guid 为加载数据入口结点的 guid
    /// 然后删除除入口结点外所有结点
    /// </summary>
    private void ClearGraph()
    {
        Nodes.Find(x => x.EntyPoint).GUID = _containerCache.NodeLinks[0].baseNodeGuid;
        foreach (var perNode in Nodes)
        {
            if (perNode.EntyPoint) continue;
            //移除所有指向该结点的边
            Edges.Where(x => x.input.node == perNode).ToList()
                .ForEach(edge => _graphView.RemoveElement(edge));
            //移除该节点
            _graphView.RemoveElement(perNode);
        }
    }

    /// <summary>
    /// 根据加载的数据创建结点，并给他们赋值
    /// </summary>
    private void GenerateDialogueNodes()
    {
        foreach (var nodeData in _containerCache.DialogueNodeData)
        {
            var tempNode = _graphView.CreateNode(nodeData.dialogueText, Vector2.zero);
            tempNode.GUID = nodeData.guid;
            //设置节点位置
            tempNode.SetPosition(new Rect(nodeData.position, _graphView.DefaultNodeSize));
            _graphView.AddElement(tempNode);

            //根据 缓存中边的数据给创建的结点添加其对应的输出端口
            var nodePorts = _containerCache.NodeLinks.Where(x => x.baseNodeGuid == nodeData.guid).ToList();
            nodePorts.ForEach(x => _graphView.AddChoicePort(tempNode, x.portName));
        }
    }

    private void ConnectDialogueNodes()
    {
        //便利结点，找到以该结点作为输出结点的边 output->input(输出节点是唯一的，输入节点可以有好几个输入)
        for (var i = 0; i < Nodes.Count; i++)
        {
            var k = i;
            var connections = _containerCache.NodeLinks.Where(x => x.baseNodeGuid == Nodes[k].GUID).ToList();
            //对于条边，我们已经有了其输出结点，接下来我们需要找其对应的输入结点
            for (var j = 0; j < connections.Count(); j++)
            {
                var targetNodeGUID = connections[j].targetNodeGuid;
                //找到输入结点
                var targetNode = Nodes.First(x => x.GUID == targetNodeGUID);
                //将对应的输出端口和输入端口连接起来。
                LinkNodesTogether(Nodes[i].outputContainer[j].Q<Port>(), (Port)targetNode.inputContainer[0]);
            }
        }
    }

    private void LinkNodesTogether(Port outputSocket, Port inputSocket)
    {
        var tempEdge = new Edge()
        {
            output = outputSocket,
            input = inputSocket
        };
        //将该边的输入输出端口与对应的边相连接
        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);
        _graphView.Add(tempEdge);
    }
}