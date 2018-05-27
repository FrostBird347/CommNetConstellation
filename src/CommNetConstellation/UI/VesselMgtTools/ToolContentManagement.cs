﻿using CommNet;
using CommNetConstellation.CommNetLayer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        public AbstractMgtTool(CNCCommNetVessel thisVessel, string uniqueCodename, string toolName, List<Callback> actionCallbacks = null)
        {
            this.cncVessel = thisVessel;
            this.codename = uniqueCodename + "_mgttool";
            this.toolName = toolName;
            this.actionCallbacks = actionCallbacks;
        }

        protected void selfRefresh()
        {
            this.toolManagement.selectTool(this.codename);
        }

        public abstract List<DialogGUIBase> getContentComponents();
        public virtual void cleanup()
        {
            this.antennas = null;
            this.initialFrequencies = null;
        }

        public virtual void precompute()
        {
            this.antennas = this.cncVessel.getAllAntennaInfo(true);
            this.initialFrequencies = this.cncVessel.getFrequencies();
        }
    }

    public class ToolContentManagement
    {
        protected DialogGUIVerticalLayout toolContentLayout;
        protected List<AbstractMgtTool> tools;
        private AbstractMgtTool currentTool;
        private float width, height;

        public ToolContentManagement(float width, float height)
        {
            this.tools = new List<AbstractMgtTool>();
            this.currentTool = null;
            this.width = width;
            this.height = height;
        }

        public void add(AbstractMgtTool newTool)
        {
            this.tools.Add(newTool);
            newTool.toolManagement = this;
        }

        public void clear()
        {
            this.tools.Clear();
        }

        public List<DialogGUIBase> getLayoutContents()
        {
            List<DialogGUIBase> layout = new List<DialogGUIBase>();

            layout.Add(new DialogGUILabel("<b>Management tools</b>", false, false));
            DialogGUIBase[] buttons = new DialogGUIBase[this.tools.Count];
            for (int i = 0; i < this.tools.Count; i++)
            {
                AbstractMgtTool thisTool = this.tools[i];
                buttons[i] = new DialogGUIButton(thisTool.toolName, delegate { selectTool(thisTool.codename); }, 50, 32, false);
            }
            layout.Add(new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.MiddleLeft, buttons));

            //Tool content
            this.toolContentLayout = new DialogGUIVerticalLayout(10, 100, 4, new RectOffset(5, 25, 5, 5), TextAnchor.UpperLeft, new DialogGUIBase[] { new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true) });
            layout.Add(new DialogGUIScrollList(new Vector2(this.width, this.height), false, true, this.toolContentLayout));

            return layout;
        }

        public void selectTool(string toolCodename)
        {
            if(this.currentTool != null)
                this.currentTool.cleanup();

            AbstractDialog.deregisterLayoutComponents(this.toolContentLayout);

            if ((this.currentTool = this.tools.Find(x => x.codename.Equals(toolCodename))) == null)
            {
                this.toolContentLayout.AddChildren(new DialogGUIBase[] { }); // empty
            }
            else
            {
                this.currentTool.precompute();
                this.toolContentLayout.AddChildren(this.currentTool.getContentComponents().ToArray());
            }

            AbstractDialog.registerLayoutComponents(this.toolContentLayout);
        }
    }
}
