﻿using CommNet;
using KSP.UI.Screens.Mapview;
using System.Collections.Generic;
using UnityEngine;
using CommNetManagerAPI;

namespace CommNetConstellation.CommNetLayer
{
    //TODO: Add RT's Multi-Path mode

    /// <summary>
    /// CommNetUI is the view in the Model–view–controller sense. Everything a player is seeing goes through this class
    /// </summary>
    public class CNCCommNetUI : CommNetUI
    {
        public static new CNCCommNetUI Instance
        {
            get;
            protected set;
        }

        /// <summary>
        /// Activate things when the player enter a scene that uses CommNet UI
        /// </summary>
        public override void Show()
        {
            registerMapNodeIconCallbacks();
            base.Show();
        }

        /// <summary>
        /// Clean up things when the player exits a scene that uses CommNet UI
        /// </summary>
        public override void Hide()
        {
            deregisterMapNodeIconCallbacks();
            base.Hide();
        }

        /// <summary>
        /// Run own display updates
        /// </summary>
        protected override void UpdateDisplay()
        {
            base.UpdateDisplay();
            updateView();   
        }

        /// <summary>
        /// Register own callbacks
        /// </summary>
        protected void registerMapNodeIconCallbacks()
        {
            List<CNCCommNetVessel> commnetVessels = CNCCommNetScenario.Instance.getCommNetVessels();

            for (int i = 0; i < commnetVessels.Count; i++)
            {
                MapObject mapObj = commnetVessels[i].Vessel.mapObject;

                if (mapObj.type == MapObject.ObjectType.Vessel)
                    mapObj.uiNode.OnUpdateVisible += new Callback<MapNode, MapNode.IconData>(this.OnMapNodeUpdateVisible);
            }
        }

        /// <summary>
        /// Remove own callbacks
        /// </summary>
        protected void deregisterMapNodeIconCallbacks()
        {
            List<CNCCommNetVessel> commnetVessels = CNCCommNetScenario.Instance.getCommNetVessels();

            for (int i = 0; i < commnetVessels.Count; i++)
            {
                MapObject mapObj = commnetVessels[i].Vessel.mapObject;
                mapObj.uiNode.OnUpdateVisible -= new Callback<MapNode, MapNode.IconData>(this.OnMapNodeUpdateVisible);
            }
        }

        /// <summary>
        /// Update the MapNode object of each CommNet vessel
        /// </summary>
        private void OnMapNodeUpdateVisible(MapNode node, MapNode.IconData iconData)
        {
            CNCCommNetVessel thisVessel = ((ModularCommNetVessel)node.mapObject.vessel.connection).GetModuleOfType<CNCCommNetVessel>();
            //(CNCCommNetVessel) node.mapObject.vessel.connection;

            if(thisVessel != null && node.mapObject.type == MapObject.ObjectType.Vessel)
            {
                if (thisVessel.getStrongestFrequency() < 0) // blind vessel
                    iconData.color = Color.grey;
                else
                    iconData.color = Constellation.getColor(thisVessel.getStrongestFrequency());
            }
        }

        /// <summary>
        /// Compute the color based on the connection between two nodes
        /// </summary>
        private Color getConstellationColor(CommNode a, CommNode b)
        {
            //Assume the connection between A and B passes the check test
            List<short> commonFreqs = Constellation.NonLinqIntersect(CNCCommNetScenario.Instance.getFrequencies(a), CNCCommNetScenario.Instance.getFrequencies(b));
            IRangeModel rangeModel = CNCCommNetScenario.RangeModel;
            short strongestFreq = -1;
            double longestRange = 0.0;

            CNCCommNetVessel vesselA = ((ModularCommNetVessel)a.GetVessel().Connection).GetModuleOfType<CNCCommNetVessel>();
            //(CNCCommNetVessel)CNCCommNetScenario.Instance.findCorrespondingVessel(a).Connection;
            CNCCommNetVessel vesselB = ((ModularCommNetVessel)b.GetVessel().Connection).GetModuleOfType<CNCCommNetVessel>();
            //(CNCCommNetVessel)CNCCommNetScenario.Instance.findCorrespondingVessel(b).Connection;
            for (int i = 0; i < commonFreqs.Count; i++)
            {
                short thisFreq = commonFreqs[i];
                double thisRange = rangeModel.GetMaximumRange(CNCCommNetScenario.Instance.getCommPower(a, thisFreq), CNCCommNetScenario.Instance.getCommPower(b, thisFreq));

                if(thisRange > longestRange)
                {
                    longestRange = thisRange;
                    strongestFreq = thisFreq;
                }
            }

            return Constellation.getColor(strongestFreq); 
        }

        /// <summary>
        /// Render the CommNet presentation
        /// </summary>
        private void updateView()
        {
            CommNetwork net = CommNetNetwork.Instance.CommNet;
            CommNetVessel cnvessel = null;
            CommNode node = null;
            CommPath path = null;

            if (this.vessel != null && this.vessel.connection != null && this.vessel.connection.Comm.Net != null)
			{
                cnvessel = this.vessel.connection;
                node = cnvessel.Comm;
                path = cnvessel.ControlPath;
			}

            //work out how many connections to paint
            int numLinks = 0;
            switch (CommNetUI.Mode)
            {
                case CommNetUI.DisplayMode.None:
                    numLinks = 0;
                    break;

                case CommNetUI.DisplayMode.FirstHop:
                case CommNetUI.DisplayMode.Path:
                    if (cnvessel.ControlState == VesselControlState.Probe || cnvessel.ControlState == VesselControlState.Kerbal ||
                        path == null || path.Count == 0)
                    {
                        numLinks = 0;
                    }
                    else
                    {
                        if (CommNetUI.Mode == CommNetUI.DisplayMode.FirstHop)
                        {
                            path.First.GetPoints(this.points);
                            numLinks = 1;
                        }
                        else
                        {
                            path.GetPoints(this.points, true);
                            numLinks = path.Count;
                        }
                    }
                    break;

                case CommNetUI.DisplayMode.VesselLinks:
                    numLinks = node.Count;
                    node.GetLinkPoints(this.points);
                    break;

                case CommNetUI.DisplayMode.Network:
                    if (net.Links.Count == 0)
                    {
                        numLinks = 0;
                    }
                    else
                    {
                        numLinks = net.Links.Count;
                        net.GetLinkPoints(this.points);
                    }
                    break;
            }// end of switch

            //check if nothing to draw
            if (numLinks == 0)
            {
                if (this.line != null)
                    this.line.active = false;

                this.points.Clear();
                return;
            }

            //paint eligible connections
            switch (CommNetUI.Mode)
            {
                case CommNetUI.DisplayMode.FirstHop:
                {
                    float lvl = Mathf.Pow((float)path.First.signalStrength, this.colorLerpPower);
                    Color customHighColor = getConstellationColor(path.First.a, path.First.b);
                    if (this.swapHighLow)
                        this.line.SetColor(Color.Lerp(customHighColor, this.colorLow, lvl), 0);
                    else
                        this.line.SetColor(Color.Lerp(this.colorLow, customHighColor, lvl), 0);
                    break;
                }
                case CommNetUI.DisplayMode.Path:
                {
                    int linkIndex = numLinks;
                    for(int i=linkIndex-1; i>=0; i--)
                    {
                        float lvl = Mathf.Pow((float)path[i].signalStrength, this.colorLerpPower);
                        Color customHighColor = getConstellationColor(path[i].a, path[i].b);
                        if (this.swapHighLow)
                            this.line.SetColor(Color.Lerp(customHighColor, this.colorLow, lvl), i);
                        else
                            this.line.SetColor(Color.Lerp(this.colorLow, customHighColor, lvl), i);
                    }
                    break;
                }
                case CommNetUI.DisplayMode.VesselLinks:
                {
                    var itr = node.Values.GetEnumerator();
                    int linkIndex = 0;
                    while(itr.MoveNext())
                    {
                        CommLink link = itr.Current;
                        float lvl = Mathf.Pow((float)link.GetSignalStrength(link.a != node, link.b != node), this.colorLerpPower);
                        Color customHighColor = getConstellationColor(link.a, link.b);
                        if (this.swapHighLow)
                            this.line.SetColor(Color.Lerp(customHighColor, this.colorLow, lvl), linkIndex++);
                        else
                            this.line.SetColor(Color.Lerp(this.colorLow, customHighColor, lvl), linkIndex++);
                    }
                    break;
                }
                case CommNetUI.DisplayMode.Network:
                {
                    for (int i = numLinks-1; i >= 0; i--)
                    {
                        CommLink commLink = net.Links[i];
                        float f = (float)net.Links[i].GetBestSignal();
                        float t = Mathf.Pow(f, this.colorLerpPower);
                        Color customHighColor = getConstellationColor(commLink.a, commLink.b);
                        if (this.swapHighLow)
                            this.line.SetColor(Color.Lerp(customHighColor, this.colorLow, t), i);
                        else
                            this.line.SetColor(Color.Lerp(this.colorLow, customHighColor, t), i);
                    }
                    break;
                }
            } // end of switch
        }
    }
}
