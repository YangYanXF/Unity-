# Unity-

一个简单的对话编辑器，实现了图形化编辑界面，以及对应的数据的保存与加载

# [Unity 插件] 图形化的对话编辑器

基于 Unity GraphView 的简单图形化对话编辑器

## 如何使用该编辑器？

（1）打开编辑器

* 将该插件导入 Unity 后，你可以选择在菜单栏 Graph-> DialogueSystem 直接打开该编辑器

![Image](https://pic4.zhimg.com/80/v2-3f0d7c83a2f0d43941d1e49cb5087726.png)

* 自定义 DialogueSystem Inspector 面板：输入文件名，如果文件存在则打开文件，如果不存在则直接打开编辑窗口

![Image](https://pic4.zhimg.com/80/v2-8f14a637482e8a1b9eb7babe55059d6e.png)

（2）创建结点

* 点击标题栏的 CreateNode 按钮
* 右键单击，点击弹出菜单栏的 CreateNode。

![Image](https://pic4.zhimg.com/80/v2-a82a13c098ce28bf551fc341bd8e8c4e.png)

（3） 结点

* 点击 Add Choice 按钮添加选项，用于连接其他结点（该结点成为输出结点，而左边 Input结点为输入节点，用过 ShaderGraph 应该一下子就能明白。（注意一个输出结点只能连接一条线段，而输入结点可以有多条线段与其相连

![Image](https://pic4.zhimg.com/80/v2-11764788e55bbcb90053bd810127fc1b.png)

（4） 保存与载入数据

* 点击顶部的 Save Data 和 Load Data 即可，如果文件名为空或是文件不存在，会弹出提示框

![Image](https://pic4.zhimg.com/80/v2-5b5d839b70330360db32259685002202.png)

（5）如何使用导出的数据
我们首先使用下面的 DialogueSystem 脚本的静态函数，获取 DialogueContainer 数据

```C#
public static DialogueContainer GetGraphData(string fileName)
```

对于 DialogueContainer 数据，我们主要使用其中的 DialogueGraphData（其他两个数据是用于加载和存储可视化窗口的结点和连线数据），考虑到对话的性质，该数据使用 图 存储对话，在使用时我们无需调用 InitGraphData，这是保存数据时就做好的。我们只需使用 DialogueContainer 中的其他即可函数，以及 GraphDataNode 中定义的几个属性 即可满足我们的需求。

定义如下：

```C#
[Serializable]
public class DialogueContainer : ScriptableObject
{
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
    public List<DialogueNodeData> DialogueNodeData = new List<DialogueNodeData>();
    public List<GraphDataNode> DialogueGraphData = new List<GraphDataNode>();

    //根据 NodeLinks 和 DialogueNodeData 初始化 DialogueGraphData
    public void InitGraphData(){}

    //获取该对话数据的第一个结点
    public GraphDataNode GetHead(){}

    //用于获取 node 结点的上一个结点，index 是对应第几个选项
    public GraphDataNode GetNext(GraphDataNode node, int index = 0){}

    //用于获取 node 结点的上一个结点，index 是对应第几个选项
    public GraphDataNode GetPre(GraphDataNode node, int index = 0){}
}

[Serializable]
public class GraphDataNode
{
    public string text;
    public List<int> next = new List<int>();
    public List<int> pre = new List<int>();
    public List<string> choices = new List<string>();

    public int preLength => pre.Count;
    public int nextLength => next.Count;
    public bool isChoiceNode => nextLength > 1 ? true : false;
    public bool isEnd => nextLength == 0 ? true : false;
}
```

## 实现原理

### 主体文件结构

* Editor(Folder)
  - DialogueGraph.cs : 主要用于创建与窗口外观有关的内容，如顶部标题栏的按钮，或是缩略图
  - DialogueGraphView.cs ：用于处理该编辑器的主要逻辑，如创建结点、为结点创建端口等...
  - NodeSearchWindow.cs
  - GraphSaveUtility.cs ： 用于保存与加载数据
  - DialogueNode.cs ：对话结点
  - DialogueSystemEditor.cs ：用于给 DialogueSystem 自定义 Inspector 窗口
  - Resources(Folder)
    * NarrativeGraph.uss ： 编辑器背景网格
    * Node.uss ： 编辑器结点颜色
* runtime(Folder)
  - DialogueContainer.cs ： 最终实例化出来的数据（ScriptableObject）
  - DialougeNodeData.cs ：用于保存结点数据
  - NodeLinkData.cs ： 用于保存连线数据
  - GraphDataNode.cs ： 图（数据结构）的结点
  - DialogueSystem.cs : 提供了一些简单的功能，比如加载文件数据

### DialogueGraph.cs

```C#
public class DialogueGraph : EditorWindow
{
    //用于创建标题栏打开编辑器的选项
    [MenuItem("Graph/Dialogue System")]
    public static void CreateGraphViewWindow()
    {
        //...
    }

    private void OnEnable()
    {
        //初始化 DialogueGraphView.cs
        ConstructGraphView();
        //创建标题栏 按钮与文本输入框
        GenerateToolbar();
        //创建缩略图
        GenerateMiniMap();
    }
}
```

### DialougeGraphView.cs

```C#
public class DialogueGraphView : GraphView
{
    public DialogueGraphView(DialogueGraph editorWindow)
    {
        //...

        //将开始节点添加到该视图中
        AddElement(GenerateEntryPointNode());

        //添加搜索窗口
        AddSearchWindow(editorWindow);
    }
    /// <summary>
    /// 该函数用于确定哪些端口之间可以相连，这里不涉及数据传输，所以仅仅是让端口不能连接自身所在结点
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {

    }

    /// <summary>
    /// 调用 CreateNode 函数创建结点，并将结点添加进该视图中;
    /// </summary>
    public void CreateNewDialogueNode(string nodeName, Vector2 position)
    {
        AddElement(CreateNode(nodeName, position));
    }

    /// <summary>
    /// 给 nodeCache 添加名为 overridePortName 的端口
    /// </summary>
    public void AddChoicePort(DialogueNode nodeCache, string overriddenPortName = "")
    {
        //...
    }

    /// <summary>
    /// 移除 node 的 socket 端口，并将该端口
    /// </summary>
    private void RemovePort(Node node, Port socket)
    {
        //...
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

    /// <summary>
    /// 创建入口结点
    /// </summary>
    private DialogueNode GenerateEntryPointNode()
    {

    }
}
```

### GraphSaveUtility

```C#
public class GraphSaveUtility
{

    /// <summary>
    /// 保存对话视图
    /// </summary>
    public void SaveGraph(string fileName)
    {

    }


    /// <summary>
    /// 加载对话视图
    /// </summary>
    public void LoadGraph(string fileName)
    {

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

    }

    /// <summary>
    /// 根据加载的数据创建结点，并给他们赋值
    /// </summary>
    private void GenerateDialogueNodes()
    {

    }

    //将结点链接起来
    private void ConnectDialogueNodes()
    {

    }
}
```

参考资料：https://www.youtube.com/watch?v=7KHGH0fPL84