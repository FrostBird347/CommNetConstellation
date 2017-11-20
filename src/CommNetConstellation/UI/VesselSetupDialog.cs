﻿using CommNetConstellation.CommNetLayer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommNetConstellation.UI.VesselMgtTools;
using CommNetManagerAPI;
using CommNet;

namespace CommNetConstellation.UI
{
    /// <summary>
    /// Edit the constellation membership of this vessel (Controller)
    /// </summary>
    public class VesselSetupDialog : AbstractDialog
    {
        private Vessel hostVessel = null; // could be null (in editor)
        private CNCCommNetVessel cncVessel = null;
        private string description = "Something";

        private const string nofreqMessage = "No active frequency to broadcast!";
        private UIStyle nofreqMessageStyle;

        private Callback<Vessel> updateCallback;
        private DialogGUIVerticalLayout frequencyRowLayout;
        private ToolContentManagement toolMgt;
        private static readonly Texture2D colorTexture = UIUtils.loadImage("colorDisplay");

        public VesselSetupDialog(string title, Vessel vessel, Callback<Vessel>  updateCallback) : base("vesselEdit",
                                                                                                                title, 
                                                                                                                0.5f, //x
                                                                                                                0.5f, //y
                                                                                                                500, //width
                                                                                                                600, //height
                                                                                                                new DialogOptions[] {})
        {
            this.hostVessel = vessel;
            this.cncVessel = ((ModularCommNetVessel)hostVessel.Connection).GetModuleOfType<CNCCommNetVessel>();
            this.updateCallback = updateCallback;
            this.description = string.Format("Active frequencies allow this vessel '{0}' to talk with other vessels, which share one or more of these frequencies.", this.hostVessel.vesselName);

            this.toolMgt = new ToolContentManagement();
            UpdateListTool updateTool = new UpdateListTool(cncVessel);
            this.toolMgt.add(updateTool);
            AntennaTool antennaTool = new AntennaTool(cncVessel, refreshFrequencyRows);
            this.toolMgt.add(antennaTool);
            VanillaFreqTool vanillaTool = new VanillaFreqTool(cncVessel, refreshFrequencyRows);
            this.toolMgt.add(vanillaTool);

            this.nofreqMessageStyle = new UIStyle();
            this.nofreqMessageStyle.alignment = TextAnchor.MiddleCenter;
            this.nofreqMessageStyle.fontStyle = FontStyle.Bold;
            this.nofreqMessageStyle.normal = HighLogic.UISkin.label.normal;

            this.GetInputLocks();
        }

        protected override void OnPreDismiss()
        {
            this.updateCallback?.Invoke(this.hostVessel);
            this.ReleaseInputLocks();
        }

        protected override List<DialogGUIBase> drawContentComponents()
        {
            List<DialogGUIBase> listComponments = new List<DialogGUIBase>();

            List<short> vesselFrequencyList = cncVessel.getFrequencies();
            vesselFrequencyList.Sort();

            listComponments.Add(new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.UpperCenter, new DialogGUIBase[] { new DialogGUILabel(this.description + "\n\n", false, false) }));

            //frequency list
            listComponments.Add(new DialogGUILabel("<b>Active frequencies</b>", false, false));
            DialogGUIBase[] frequencyRows;
            if (vesselFrequencyList.Count == 0)
            {
                frequencyRows = new DialogGUIBase[2];
                frequencyRows[0] = new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true);
                frequencyRows[1] = new DialogGUILabel(nofreqMessage, nofreqMessageStyle, true, false);
            }
            else
            {
                frequencyRows = new DialogGUIBase[vesselFrequencyList.Count + 1];
                frequencyRows[0] = new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true);
                for (int i = 0; i < vesselFrequencyList.Count; i++)
                {
                    frequencyRows[i + 1] = createFrequencyRow(vesselFrequencyList[i]);
                }
            }
            frequencyRowLayout = new DialogGUIVerticalLayout(10, 100, 4, new RectOffset(5, 25, 5, 5), TextAnchor.UpperLeft, frequencyRows);
            listComponments.Add(new DialogGUIScrollList(Vector2.one, false, true, frequencyRowLayout));

            //tools
            listComponments.AddRange(this.toolMgt.getLayoutContents());

            return listComponments;
        }

        private DialogGUIHorizontalLayout createFrequencyRow(short freq)
        {
            Color color = Constellation.getColor(freq);
            string name = Constellation.getName(freq);

            DialogGUIImage colorImage = new DialogGUIImage(new Vector2(32, 32), Vector2.one, color, colorTexture);
            DialogGUILabel nameLabel = new DialogGUILabel(name, 160, 12);
            DialogGUILabel eachFreqLabel = new DialogGUILabel(string.Format("(<color={0}>{1}</color>)", UIUtils.colorToHex(color), freq), 70, 12);
            DialogGUILabel freqPowerLabel = new DialogGUILabel(string.Format("Combined Comm Power: {0}", UIUtils.RoundToNearestMetricFactor(cncVessel.getMaxComPower(freq), 2)), 180, 12);
            return new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.MiddleLeft, new DialogGUIBase[] { colorImage, nameLabel, eachFreqLabel, freqPowerLabel });
        }

        private void refreshFrequencyRows()
        {
            deregisterLayoutComponents(frequencyRowLayout);

            List<short> vesselFrequencyList = cncVessel.getFrequencies();
            vesselFrequencyList.Sort();

            for (int i = 0; i < vesselFrequencyList.Count; i++)
            {
                frequencyRowLayout.AddChild(createFrequencyRow(vesselFrequencyList[i]));
            }

            if (vesselFrequencyList.Count == 0)
            {
                frequencyRowLayout.AddChild(new DialogGUILabel(nofreqMessage, nofreqMessageStyle, true, false));
            }

            registerLayoutComponents(frequencyRowLayout);
        }
    }
}
