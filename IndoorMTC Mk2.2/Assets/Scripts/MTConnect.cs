using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MTConnect : MonoBehaviour
{
    public bool isSimulatedMT;

    UnityWebRequest request;
    public string mtcURL;

    public List<string[]> deviceInfo = new List<string[]>();
    string attributeName;
    string eventSampleName;
    bool resetname = true;
    bool connect;
    public GameObject errorWindow;


    void Update()
    {
        if (mtcURL.Length != 0)
        {
            if (request.isDone)
            {
                if (connect)
                {
                    GetMachine();
                    errorWindow.SetActive(false);
                }
                else
                {
                    errorWindow.GetComponent<Text>().text = "Error: " + request.error;
                    errorWindow.SetActive(true);
                }

                StartCoroutine(GetText());
            }
        }
    }

    public IEnumerator GetText() //Get MTConnect data
    {
        request = UnityWebRequest.Get(mtcURL);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.Log(request.error);
            connect = false;
        }
        else
        {
            connect = true;
        }
    }


    public void GetMachine() //Save all MTConnect Data to List
    {
        deviceInfo.Clear();
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(request.downloadHandler.text);
        XmlNodeList deviceList = xmlDoc.GetElementsByTagName("DeviceStream"); //List of all devices

        foreach (XmlNode device in deviceList)      //level-DeviceStream
        {
            XmlNodeList machineInfo = device.ChildNodes;


            foreach (XmlNode componentStream in machineInfo) //level-ComponentStream
            {

                if (resetname)
                {
                    attributeName = componentStream.Attributes["componentId"].Value;
                    resetname = false;
                }

                XmlNodeList samplesEvents = componentStream.ChildNodes;

                foreach (XmlNode eventSampleList in samplesEvents) //level-Event or Samples
                {
                    if (eventSampleList.Name == "Events")
                    {
                        eventSampleName = "Events";
                        XmlNodeList specialEvent = eventSampleList.ChildNodes;

                        int i = 0;
                        foreach (XmlNode eventItem in specialEvent) //Event Node
                        {
                            string[] specialDeviceInfo = new string[4] { attributeName, eventSampleName, eventItem.Name, eventItem.InnerText };

                            deviceInfo.Add(specialDeviceInfo);
                            i++;
                        }
                    }

                    if (eventSampleList.Name == "Samples")
                    {
                        eventSampleName = "Samples";
                        XmlNodeList specialSample = eventSampleList.ChildNodes;

                        string[] lastSample = new string[2];

                        int i = 0;
                        foreach (XmlNode eventItem in specialSample) //Sample Node
                        {
                            lastSample = new string[2] { eventItem.Attributes["name"].Value, eventItem.InnerText };
                            i++;
                        }
                        string[] specialDeviceInfo = new string[4] { attributeName, eventSampleName, lastSample[0], lastSample[1] };
                        deviceInfo.Add(specialDeviceInfo);

                    }
                }
            }

            resetname = true;

        }
    }
}
