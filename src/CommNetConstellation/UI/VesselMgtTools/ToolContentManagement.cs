using CommNet;
using CommNetConstellation.CommNetLayer;
using CommNetConstellation.UI.DialogGUI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;

namespace CommNetConstellation.UI.VesselMgtTools
{
    public abstract class AbstractMgtTool
    {
        protected List<short> initialFrequencies;
        protected CNCCommNetVessel cncVessel;
        protected List<CNCAntennaPartInfo> antennas;
        protected List<Callback> actionCallbacks;

        public ToolContentManagement toolManagement { get; set; }

        public string codename;
        public string toolName;

        protected AbstractMgtTool(CommNetVessel thisVessel, string uniqueCodename, string inputToolName, List<Callback> inputActionCallbacks = null)
        {
            if (!(thisVessel is CNCCommNetVessel))
            {
                CNCLog.Error($"Vessel {thisVessel.Vessel.vesselName} connection is not of type CNCCommNetVessel!");
                return;
            }

            cncVessel = (CNCCommNetVessel)thisVessel;
            codename = uniqueCodename + "_mgttool";
            toolName = inputToolName;
            actionCallbacks = inputActionCallbacks;
        }

        protected void selfRefresh()
        {
            toolManagement.selectTool(codename);
        }

        public abstract List<DialogGUIBase> getContentComponents();
        public virtual void cleanup()
        {
            antennas = null;
            initialFrequencies = null;
        }

        public virtual void precompute()
        {
            antennas = cncVessel.getAllAntennaInfo(true);
            initialFrequencies = cncVessel.getFrequencyList();
        }
    }

    public class ToolContentManagement(float inputWidth, float inputHeight)
    {
        protected DialogGUIVerticalLayout toolContentLayout;
        protected List<AbstractMgtTool> tools = new List<AbstractMgtTool>();
        private AbstractMgtTool currentTool = null;
        protected CustomDialogGUIScrollList scrollArea;

        public void add(AbstractMgtTool newTool)
        {
            tools.Add(newTool);
            newTool.toolManagement = this;
        }

        public void clear()
        {
            tools.Clear();
        }

        public List<DialogGUIBase> getLayoutContents()
        {
            List<DialogGUIBase> layout = new List<DialogGUIBase>();

            layout.Add(new DialogGUILabel("<b>" + Localizer.Format("#CNC_getContentCompon_ManagementTools") + "</b>"));//Management tools
            DialogGUIBase[] buttons = new DialogGUIBase[tools.Count];
            for (int i = 0; i < tools.Count; i++)
            {
                AbstractMgtTool thisTool = tools[i];
                buttons[i] = new DialogGUIButton(thisTool.toolName, delegate { selectTool(thisTool.codename); }, 50, 32, false);
            }
            layout.Add(new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.MiddleLeft, buttons));

            //Tool content
            toolContentLayout = new DialogGUIVerticalLayout(10, 100, 4, new RectOffset(5, 25, 5, 5), TextAnchor.UpperLeft, new DialogGUIBase[] { new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true) });
            scrollArea = new CustomDialogGUIScrollList(new Vector2(inputWidth, inputHeight), false, true, toolContentLayout);
            layout.Add(scrollArea);

            return layout;
        }

        public void selectTool(string toolCodename)
        {
            if(currentTool != null)
                currentTool.cleanup();

            AbstractDialog.deregisterLayoutComponents(toolContentLayout);

            if ((currentTool = tools.Find(x => x.codename.Equals(toolCodename))) == null)
            {
                toolContentLayout.AddChildren(new DialogGUIBase[] { }); // empty
            }
            else
            {
                currentTool.precompute();
                toolContentLayout.AddChildren(currentTool.getContentComponents().ToArray());
            }

            AbstractDialog.registerLayoutComponents(toolContentLayout);
            scrollArea.Resize();
        }
    }
}