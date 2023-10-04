using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System;
using UnityEditor.UIElements;

[CustomEditor(typeof(DialogueSystem))]
public class DialogueSystemEditor : Editor
{
    private SerializedProperty fileName;
    private SerializedProperty fileSavePath;

    private void OnEnable()
    {
        fileName = serializedObject.FindProperty("fileName");
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        fileName.stringValue = EditorGUILayout.TextField("文件名", fileName.stringValue);
        CreateBtn_OpenDialogueWindow();
        EditorGUILayout.HelpBox("默认保存路径：Assets/Resources/DialogueData", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    private void CreateBtn_OpenDialogueWindow()
    {
        bool existFile = Resources.Load<DialogueContainer>("DialogueData/" + fileName.stringValue);
        if (GUILayout.Button(existFile ? "打开对话编辑器" : "创建文件"))
        {
            OpenDialogueWindow(existFile);
        }
    }

    private void OpenDialogueWindow(bool existFile)
    {
        if (fileName.stringValue == String.Empty)
        {
            EditorUtility.DisplayDialog("Error", "The file name can't be empty", "OK");
            return;
        }
        var window = EditorWindow.GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");
        window._fileName = fileName.stringValue;
        window.fileNameTextField.value = fileName.stringValue;
        if (existFile)
        {
            window.RequestDataOperation(false);
        }
        window.Show();
    }
}
