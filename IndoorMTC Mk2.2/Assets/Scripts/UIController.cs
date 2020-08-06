using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject controller;
    public GameObject switchFirstPersonButton;  
    
    public GameObject PopoutPanel;
    public GameObject openFileBackground;
    public Button[] navMachineButtons = new Button[2];

    [Header("Navigation Panel")]
    public GameObject navigationBackground;
    public GameObject[] navigationStepBackground = new GameObject[2];
    public Toggle[] navigationStartEndToggles = new Toggle[4]; //[map_start; machine_start; map_end; machine_end]
    public Button[] navigationOkButtons = new Button[2]; //[OK_start; OK_end]
    public Dropdown[] navigationMachineChoice = new Dropdown[2]; //[dropdown_start; dropdown_end]

    [Header("Machine Panel")]
    public GameObject machineInfoPanel;
    public bool machinePanelVisible;
    public string machineName;
    public GameObject machineInfoText;
    public GameObject machineBackground;

    Color activeGroup = new Color(0.18f, 0.53f, 0.08f, 1);
    Color activeNavigationPanel = new Color(0.60f, 0.86f, 0.45f);
    Color passiveNavigationPanel = new Color(0.89f, 0.89f, 0.89f);
    Navigation navigation;
    CameraController cameraControl;
    IndoorGMLParser parser;
    bool navigationPanelVisible;
    bool machineInfoAvailable;


    private void Start()
    {
        parser = GameObject.FindGameObjectWithTag("Controller").GetComponent<IndoorGMLParser>();
        cameraControl = Camera.main.GetComponent<CameraController>();

        if (parser.inspection)
        {
            navigationStartEndToggles[0].onValueChanged.AddListener((bool value) => NavigationToggleControl(true, value));
            navigationStartEndToggles[2].onValueChanged.AddListener((bool value) => NavigationToggleControl(false, value));

            navigation = controller.GetComponent<Navigation>();
        }
    }
    void Update()
    {
        #region Machine Info Panel

        //Update displayed data in Textpanel containing MTConnect data 
        if (machineInfoPanel.activeInHierarchy)
        {
            machineInfoAvailable = false;

            if (!string.IsNullOrEmpty(machineName))
            {
                machineInfoText.GetComponent<Text>().text = "\n" + " Name: " + machineName + "\n";

                foreach (string[] entry in controller.GetComponent<MTConnect>().deviceInfo)
                {
                    if (entry[0] == machineName)
                    {
                        machineInfoText.GetComponent<Text>().text += " " + entry[2] + ": " + entry[3] + "\n";
                        machineInfoAvailable = true;
                    }
                }
                if (!machineInfoAvailable)
                {
                    machineInfoText.GetComponent<Text>().text += " No additional Info available" + "\n";
                }
            }
            else
            {
                machineInfoText.GetComponent<Text>().text = "";
            }
        }

        #endregion

        //disable cameracontrol when Dropdown menu is open 
        if (parser.inspection)
        {
            if (navigationMachineChoice[0].transform.childCount > 3 || navigationMachineChoice[1].transform.childCount > 3)
            {
                cameraControl.cameraActive = false;
            }
            else
            {
                cameraControl.cameraActive = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    //open/close machine info panel
    public void IOMachineInfoPanel(bool active)
    {
        machineInfoPanel.SetActive(active);
    }

    //activate First person button if position is defined in IndoorGML data
    public void IOSwitchFirstPersonButton(bool active)
    {
        switchFirstPersonButton.SetActive(active);
    }

    public void PanelControl(int status)
    {
        /*
         * status == 0 ►►► open navigation popout
         * status == 1 ►►► minimize navigation
         * status == 2 ►►► open GML File
         * status == 3 ►►► machine panel opened
        */
        if (status == 0 && !navigationPanelVisible)
        {
            cameraControl.cameraActive = true;

            navigationBackground.GetComponent<Image>().color = activeGroup;
            openFileBackground.GetComponent<Image>().color = Color.grey;
            machineBackground.GetComponent<Image>().color = Color.grey;


            navigation.mapChoiceEnabled = true;


            if (machinePanelVisible)
            {
                PopoutPanel.GetComponent<Animation>().Play("Machine to Navigation");
            }
            else
            {
                PopoutPanel.GetComponent<Animation>().Play("Navigation Popout");
            }

            navigationPanelVisible = true;
            machinePanelVisible = false;
        }

        if (status == 1 && navigationPanelVisible)
        {
            PopoutPanel.GetComponent<Animation>().Play("Navigation Popin");
            navigationPanelVisible = false;
        }

        if (status == 2)
        {
            cameraControl.cameraActive = true;

            navigationBackground.GetComponent<Image>().color = Color.grey;
            openFileBackground.GetComponent<Image>().color = activeGroup;
            machineBackground.GetComponent<Image>().color = Color.grey;

            if (navigationPanelVisible)
            {
                PopoutPanel.GetComponent<Animation>().Play("Navigation Popin");
            }
            if (machinePanelVisible)
            {
                PopoutPanel.GetComponent<Animation>().Play("Machine Fade out");
            }


            navigationPanelVisible = false;
            machinePanelVisible = false;
        }

        if (status == 3 && !machinePanelVisible)
        {
            cameraControl.cameraActive = false;

            navigationBackground.GetComponent<Image>().color = Color.grey;
            openFileBackground.GetComponent<Image>().color = Color.grey;
            machineBackground.GetComponent<Image>().color = activeGroup;

            if (navigationPanelVisible)
            {
                PopoutPanel.GetComponent<Animation>().Play("Navigation to Machine");
            }
            else
            {
                PopoutPanel.GetComponent<Animation>().Play("Machine Fade in");
            }

            navigationPanelVisible = false;
            machinePanelVisible = true;
        }
    }

    //behavior for choosing eighter choose on map or from dropdown list
    public void NavigationToggleControl(bool start, bool toggleOn)
    {
        navigation.mapChoiceEnabled = toggleOn;

        if (start)
        {
            navigationMachineChoice[0].interactable = !toggleOn;
            navigationOkButtons[0].interactable = !toggleOn;

        }
        else
        {
            navigationMachineChoice[1].interactable = !toggleOn;
            navigationOkButtons[1].interactable = !toggleOn;

        }
    }

    //coloring panels and make toggles interactable depending on current choice
    public void NavigationPanelControl(int status)
    {
        switch (status)
        {
            case 0: //default State: Choose StartPoint
                navigationStartEndToggles[0].interactable = true;
                navigationStartEndToggles[1].interactable = true;
                navigationStartEndToggles[2].interactable = false;
                navigationStartEndToggles[3].interactable = false;
                navigationStepBackground[0].GetComponent<Image>().color = activeNavigationPanel;
                navigationStepBackground[1].GetComponent<Image>().color = passiveNavigationPanel;
                navigation.mapChoiceEnabled = true;
                break;

            case 1: //choose Endpoint
                navigationStartEndToggles[0].interactable = false;
                navigationStartEndToggles[1].interactable = false;
                navigationStartEndToggles[2].interactable = true;
                navigationStartEndToggles[3].interactable = true;
                navigationOkButtons[0].interactable = false;
                navigationStepBackground[0].GetComponent<Image>().color = passiveNavigationPanel;
                navigationStepBackground[1].GetComponent<Image>().color = activeNavigationPanel;
                navigation.mapChoiceEnabled = true;
                break;

            case 2: //choices are done
                navigationStartEndToggles[0].interactable = false;
                navigationStartEndToggles[1].interactable = false;
                navigationStartEndToggles[2].interactable = false;
                navigationStartEndToggles[3].interactable = false;
                navigationStepBackground[0].GetComponent<Image>().color = passiveNavigationPanel;
                navigationStepBackground[1].GetComponent<Image>().color = passiveNavigationPanel;
                navigationOkButtons[1].interactable = false;
                navigation.mapChoiceEnabled = false;
                break;
        }
    }

    //activate menubuttons only after IndoorGML data is sucessfully loaded
    public void InspectionUIEnabler (bool gmlLoaded)
    {
        navMachineButtons[0].interactable = gmlLoaded;
        navMachineButtons[1].interactable = gmlLoaded;
    }
}
