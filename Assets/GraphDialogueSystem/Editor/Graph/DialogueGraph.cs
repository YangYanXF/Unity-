using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraph : EditorWindow
{
    public string _fileName = "New Narrative";

    private DialogueGraphView _graphView;

    public TextField fileNameTextField;

    [MenuItem("Graph/Dialogue System")]
    public static void CreateGraphViewWindow()
    {
        var window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue System");
    }
    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
        GenerateMiniMap();
    }

    private void ConstructGraphView()
    {
        _graphView = new DialogueGraphView(this)
        {
            name = "Dialogue System",
        };
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();
        //创建 一个 TextField 用于记录输入的文件名
        fileNameTextField = new TextField("File Name:");
        fileNameTextField.SetValueWithoutNotify(_fileName);
        //标记该文本，然后 unity 会在下一帧更新该字段的值
        fileNameTextField.MarkDirtyRepaint();
        //添加回调函数以更改 UI 中的文件名
        fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
        //将其添加到标题栏
        toolbar.Add(fileNameTextField);

        //创建用于 存储和加载数据 的按钮
        toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save Data" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load Data" });

        //创建用于 添加创建节点 的按钮
        toolbar.Add(new Button(() => _graphView.CreateNewDialogueNode("Dialogue Node", new Vector2(500, 250))) { text = "Create Node", });

        //将其添加到该窗口中
        rootVisualElement.Add(toolbar);
    }

    public void RequestDataOperation(bool save)
    {
        if (!string.IsNullOrEmpty(_fileName))
        {
            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            if (save)
                saveUtility.SaveGraph(_fileName);
            else
                saveUtility.LoadGraph(_fileName);
        }
        else
        {
            EditorUtility.DisplayDialog("Invalid File name", "Please Enter a valid filename", "OK");
        }
    }


    private void GenerateMiniMap()
    {
        var miniMap = new MiniMap { anchored = true };
        miniMap.SetPosition(new Rect(10, 30, 200, 140));
        _graphView.Add(miniMap);
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }
}