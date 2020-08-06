using SFB;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Ignition : MonoBehaviour
{
    public GameObject cameraMovementPanel;

    UIController uiController;
    IndoorGMLParser parser;
    CameraController cameraController;

    void Start()
    {
        parser = GetComponent<IndoorGMLParser>();
        uiController = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();
        cameraController = Camera.main.GetComponent<CameraController>();
    }


    public void OpenFileDialog()
    {
        if (parser.inspection)
        {
            uiController.PanelControl(1);
        }

        var gmlPath = StandaloneFileBrowser.OpenFilePanel("Locate IndoorGML Data", "", "gml", false);

        if (gmlPath.Length > 0)
        {
            if (cameraController.firstPersonView)
            { cameraController.SwitchCamera(); }

            parser.startText.GetComponent<Text>().text = "Loading... Please wait!";
            StartCoroutine(ShortLoad(new System.Uri(gmlPath[0]).AbsoluteUri));
        }
    }

    IEnumerator ShortLoad(string fileURL)
    {
        GameObject.Destroy(CommonObjs.gmlRoot); //delete all IndoorGML Objects
        CommonObjs.Init(); //Recreate structure

        yield return null; //let 1 Frame pass to show "Loading" Text

        parser.Load(fileURL); //load new file

        if (!parser.inspection) //Set Camera Position and show panels
        {
            cameraController.DoMoveViewPoint(1);
            cameraMovementPanel.SetActive(true);
        }
        else
        {
            cameraController.DoMoveViewPoint(5);
        }
    }
}