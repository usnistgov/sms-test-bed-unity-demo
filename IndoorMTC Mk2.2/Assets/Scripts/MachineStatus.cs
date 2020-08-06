using UnityEngine;

public class MachineStatus : MonoBehaviour
{
    MTConnect mtConnect;
    UIController uIController;
    IndoorGMLParser parser;
    MachinePanelControl machinePanelControl;
    bool statusKnown;

    Navigation navigation;

    void Start()
    {
        mtConnect = GameObject.FindGameObjectWithTag("Controller").GetComponent<MTConnect>();
        uIController = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();
        parser = GameObject.FindGameObjectWithTag("Controller").GetComponent<IndoorGMLParser>();
        if (parser.inspection) { navigation = GameObject.FindGameObjectWithTag("Controller").GetComponent<Navigation>(); }
        machinePanelControl = GameObject.FindGameObjectWithTag("UIController").GetComponent<MachinePanelControl>();

    }

    void Update()
    {
        //Update color of machine based on status: available, unavailable and unknown
        //do only in planning mode
        if (!parser.inspection)
        {
            statusKnown = false;
            foreach (string[] dataset in mtConnect.deviceInfo) //search for corresponding data in MTConnect stream
            {
                if (dataset[0] == gameObject.name)
                {
                    if (dataset[2] == "Availability" && dataset[3] == "UNAVAILABLE")
                    {
                        GetComponent<Renderer>().material = Resources.Load("Materials/MachineStat_UNAVILABLE", typeof(Material)) as Material;
                        statusKnown = true;
                        break;
                    }

                    if (dataset[2] == "Availability" && dataset[3] == "AVAILABLE")
                    {
                        GetComponent<Renderer>().material = Resources.Load("Materials/MachineStat_AVILABLE", typeof(Material)) as Material;
                        statusKnown = true;
                        break;
                    }
                }
            }
            if (statusKnown == false)
            {
                GetComponent<Renderer>().material = Resources.Load("Materials/MachineStat_UNKNOWN", typeof(Material)) as Material;
            }
        }
    }

    private void OnMouseDown()
    {
        //in inspection scene set machine for navigation as start or target
        if (parser.inspection && navigation.mapChoiceEnabled)
        {
            for (int i = 0; i < parser.machineStates.Count; i++)
            {
                if (gameObject.name == parser.machineStates[i][0])
                {
                    for (int j = 0; j < parser.states.Count; j++)
                    {
                        if (parser.machineStates[i][1] == parser.states[j].name)
                        {
                            if (!navigation.startDefined)
                            {
                                parser.states[j].GetComponent<Renderer>().material = Resources.Load("Materials/State_START", typeof(Material)) as Material;
                                gameObject.GetComponent<Renderer>().material = Resources.Load("Materials/State_START", typeof(Material)) as Material;
                                uIController.navigationMachineChoice[0].value = i;
                            }
                            else
                            {
                                parser.states[j].GetComponent<Renderer>().material = Resources.Load("Materials/State_TARGET", typeof(Material)) as Material;
                                gameObject.GetComponent<Renderer>().material = Resources.Load("Materials/State_TARGET", typeof(Material)) as Material;
                                machinePanelControl.machineChoice.value = i;
                                uIController.navigationMachineChoice[1].value = i;
                                machinePanelControl.SetMachine();
                            }

                            navigation.SetStartEnd(parser.states[j]);
                            parser.states[j].transform.localScale = Vector3.one * parser.stateSize * 1.3f;

                        }
                    }
                }
            }
        }

        //in planning-scene show MTConnect data when clicking on machine
        else
        {
            uIController.machineName = gameObject.transform.name;
            uIController.IOMachineInfoPanel(true);
        }
    }
}
