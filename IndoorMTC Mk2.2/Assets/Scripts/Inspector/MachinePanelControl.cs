using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MachinePanelControl : MonoBehaviour
{
    public Dropdown machineChoice;
    public Image image;
    public Transform entry;
    public Transform refList;
    public GameObject noDocText;

    string machineName;
    bool pictureSet;
    bool docSet;
    UIController uiController;
    IndoorGMLParser parser;

    void Start()
    {
        uiController = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();
        parser = GameObject.FindGameObjectWithTag("Controller").GetComponent<IndoorGMLParser>();
    }

    public void SetMachine()
    {
        machineName = machineChoice.options[machineChoice.value].text; 
        uiController.machineName = machineName; //Set Machine for Machine-Data window
        pictureSet = false;
        docSet = false;
        image.gameObject.SetActive(true);

        foreach (Transform child in refList.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        

        foreach (string[] localMachineRefs in parser.machineRefs)
        {
            if (machineName == localMachineRefs[0])
            {
                if (localMachineRefs[1] == "picture")
                {
                    float pixelsPerUnit = 100.0f;
                    Texture2D SpriteTexture;
                    byte[] FileData;

                    if(File.Exists(localMachineRefs[3]))
                    {
                        FileData = File.ReadAllBytes(localMachineRefs[3]);
                        SpriteTexture = new Texture2D(2, 2);
                        SpriteTexture.LoadImage(FileData);

                        image.GetComponent<Image>().sprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), pixelsPerUnit);
                        image.GetComponent<Image>().preserveAspect = true;
                        pictureSet = true;
                    }
                }
                if (localMachineRefs[1] == "document")
                {
                    var newEntry = Instantiate(entry);
                    newEntry.SetParent(refList, false);
                    newEntry.gameObject.transform.GetChild(0).GetComponent<Text>().text = localMachineRefs[2];
                    newEntry.gameObject.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate { OpenRefDocument(localMachineRefs[3]); });
                    docSet = true;
                }
            }
        }

        if (!pictureSet)
        {
            image.sprite = Resources.Load("NoImage", typeof(Sprite)) as Sprite;
        }

        if (docSet)
        {
            noDocText.SetActive(false);
        }
        else
        {
            noDocText.SetActive(true);
        }
    }

    public void OpenRefDocument(string path)
    {
        Application.OpenURL(path);
    }
}
