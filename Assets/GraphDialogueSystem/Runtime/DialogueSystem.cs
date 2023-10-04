using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    public string fileName;

    private const string _filePath = "DialogueData";

    public static DialogueContainer GetGraphData(string fileName)
    {
        var dialogueContainer = Resources.Load<DialogueContainer>(_filePath + "/" + fileName);
        if (dialogueContainer != null)
        {
            return dialogueContainer;
        }
        else
        {
            Debug.LogWarning($"DialogueSystem: {fileName} don't exist!");
            return null;
        }
    }
}