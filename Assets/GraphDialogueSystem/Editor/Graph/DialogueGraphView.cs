using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

public class DialogueGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(200, 150);
    public DialogueNode EntryPointNode;
    private NodeSearchWindow _searchWindow;

    public DialogueGraphView(DialogueGraph editorWindow)
    {
        //添加网格
        styleSheets.Add(Resources.Load<StyleSheet>("NarrativeGraph"));
        //允许缩放
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        //添加内容拖拽器, 允许鼠标拖动一个或多个元素
        this.AddManipulator(new ContentDragger());
        //添加一个选择拖拽器
        this.AddManipulator(new SelectionDragger());
        //添加一个矩形选择框
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new FreehandSelector());

        //添加网格背景
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        //将开始节点添加到该视图中
        AddElement(GenerateEntryPointNode());

        //添加搜索窗口
        AddSearchWindow(editorWindow);
    }


    private void AddSearchWindow(DialogueGraph editorWindow)
    {
        _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        _searchWindow.Init(editorWindow, this);
        nodeCreationRequest = context =>
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
    }

    /// <summary>
    /// 该函数用于确定哪些端口之间可以相连，这里不涉及数据传输，所以仅仅是让端口不能连接自身所在结点
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        var startPortView = startPort;

        ports.ForEach((port) =>
        {
            var portView = port;
            if (startPortView != portView && startPortView.node != portView.node)
                compatiblePorts.Add(port);
        });

        return compatiblePorts;
    }

    /// <summary>
    /// 调用 CreateDialogueNode 函数，并将结点添加进该视图中;
    /// </summary>
    public void CreateNewDialogueNode(string nodeName, Vector2 position)
    {
        AddElement(CreateNode(nodeName, position));
    }

    public DialogueNode CreateNode(string nodeName, Vector2 position)
    {
        var tempDialogueNode = new DialogueNode()
        {
            title = nodeName,
            DialogueText = nodeName,
            GUID = Guid.NewGuid().ToString()
        };
        //给结点添加 uss 文件
        tempDialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));
        //创建输入端口
        var inputPort = GeneratePort(tempDialogueNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        tempDialogueNode.inputContainer.Add(inputPort);
        tempDialogueNode.RefreshExpandedState();
        tempDialogueNode.RefreshPorts();
        tempDialogueNode.SetPosition(new Rect(position,
            DefaultNodeSize)); //To-Do: implement screen center instantiation positioning

        //创建对话文本输入框
        var textField = new TextField("");
        textField.RegisterValueChangedCallback(evt =>
        {
            tempDialogueNode.DialogueText = evt.newValue;
            tempDialogueNode.title = evt.newValue;
        });
        textField.SetValueWithoutNotify(tempDialogueNode.title);
        tempDialogueNode.mainContainer.Add(textField);

        //添加一个用于创建输出端口的按钮
        var button = new Button(() => { AddChoicePort(tempDialogueNode); })
        {
            text = "Add Choice"
        };
        tempDialogueNode.titleButtonContainer.Add(button);

        return tempDialogueNode;
    }


    public void AddChoicePort(DialogueNode nodeCache, string overriddenPortName = "")
    {
        var generatedPort = GeneratePort(nodeCache, Direction.Output);

        //为了美观，隐藏端口名字，而只留下输入框
        var portLabel = generatedPort.contentContainer.Q<Label>("type");
        generatedPort.contentContainer.Remove(portLabel);

        var outputPortCount = nodeCache.outputContainer.Query("connector").ToList().Count();
        var outputPortName = string.IsNullOrEmpty(overriddenPortName)
            ? $"Option {outputPortCount + 1}"
            : overriddenPortName;

        //该 textField 用于输入选项的名字
        var textField = new TextField()
        {
            name = string.Empty,
            value = outputPortName
        };
        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
        generatedPort.contentContainer.Add(new Label("  "));
        generatedPort.contentContainer.Add(textField);

        //添加一个删除端口的按钮
        var deleteButton = new Button(() => RemovePort(nodeCache, generatedPort))
        {
            text = "X"
        };
        generatedPort.contentContainer.Add(deleteButton);
        generatedPort.portName = outputPortName;

        nodeCache.outputContainer.Add(generatedPort);
        nodeCache.RefreshPorts();
        nodeCache.RefreshExpandedState();
    }

    private void RemovePort(Node node, Port socket)
    {
        //找到和该端口相连的边
        var targetEdge = edges.ToList()
            .Where(x => x.output.portName == socket.portName && x.output.node == socket.node);
        if (targetEdge.Any())
        {
            //断开连接
            var edge = targetEdge.First();
            edge.input.Disconnect(edge);
            RemoveElement(targetEdge.First());
        }
        //移除端口
        node.outputContainer.Remove(socket);
        //刷新结点
        node.RefreshPorts();
        node.RefreshExpandedState();
    }

    /// <summary>
    /// 创建一个用于连接其他节点的端口
    /// 其中 portDirection 是标记是输入端口还是输出端口的，
    /// </summary>
    private Port GeneratePort(DialogueNode node, Direction portDirection,
        Port.Capacity capacity = Port.Capacity.Single)
    {
        //对于最后一个参数适用于传输数据的, 对于 ShaderGraph 这种需要传数据的就很有用，这里不需要传数据，就随便填个 float 类型
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }

    private DialogueNode GenerateEntryPointNode()
    {
        var nodeCache = new DialogueNode()
        {
            title = "START",
            GUID = Guid.NewGuid().ToString(),
            DialogueText = "ENTRYPOINT",
            EntyPoint = true
        };

        //Direction.Output 意味着该将数据输出，所以该端口用于连接其他节点
        var generatedPort = GeneratePort(nodeCache, Direction.Output);
        generatedPort.portName = "Next";
        //将该端口添加进输出容器中,否者该端口不会显示
        nodeCache.outputContainer.Add(generatedPort);

        //设置入口结点不可删除并且无法拖动
        nodeCache.capabilities &= ~Capabilities.Movable;
        nodeCache.capabilities &= ~Capabilities.Deletable;

        //当我们对其他容器进行操作时，使用以下函数进行刷新，否者可能会有一些奇怪的结果
        nodeCache.RefreshExpandedState();
        nodeCache.RefreshPorts();

        nodeCache.SetPosition(new Rect(100, 200, 100, 150));
        return nodeCache;
    }
}