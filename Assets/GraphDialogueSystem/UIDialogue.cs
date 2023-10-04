using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIDialogue : MonoBehaviour
{
    DialogueContainer dialogueData;
    GraphDataNode current;

    public Button btnPrefabs;
    public Transform choices;
    public GameObject dialoguePanel;

    public string fileName = "123";

    private bool isChoice = false;

    void Start()
    {
        dialogueData = DialogueSystem.GetGraphData(fileName);
        current = dialogueData.GetHead();
        ProcessNode();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ProcessNode();
        }
    }

    public void ProcessNode()
    {
        if (current != null)
        {
            choices.gameObject.SetActive(false);
            dialoguePanel.SetActive(true);
            dialoguePanel.GetComponentInChildren<Text>().text = current.text;

            if (current.isEnd) return;

            if (isChoice)
            {
                choices.gameObject.SetActive(true);
                dialoguePanel.SetActive(false);
                var btns = choices.GetComponentsInChildren<Button>();
                foreach (var button in btns)
                {
                    Destroy(button.gameObject);
                }

                for (int i = 0; i < current.nextLength; ++i)
                {
                    var btn = Instantiate(btnPrefabs, choices);
                    btn.GetComponentInChildren<Text>().text = current.choices[i];
                    GraphDataNode next = dialogueData.GetNext(current, i);
                    btn.onClick.AddListener(() =>
                    {
                        current = next;
                        Debug.Log("执行了");
                        ProcessNode();

                    });
                }
                isChoice = false;
            }
            else if (current.isChoiceNode)
            {
                isChoice = true;
            }
            else
            {
                current = dialogueData.GetNext(current);
            }
        }
    }
}
