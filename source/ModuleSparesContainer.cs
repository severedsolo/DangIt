﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using CrewFiles;

namespace DangIt
{
    public class ModuleSparesContainer : PartModule
    {
        private bool eventAdded = false;

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("DangIt: Spares Container [" + this.GetInstanceID() + "]: OnStart, state is " + state);
            
            this.Events["TakeParts"].active = true;
        }

       
        //[KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, guiName = "Take spares", unfocusedRange = 20f)]
        [KSPEvent(active=true, guiActiveUnfocused=true, externalToEVAOnly=true, guiName="Take spares", unfocusedRange=DangIt.EvaRepairDistance)]
        public void TakeParts()
        {
            Part evaPart = DangIt.FindEVAPart();
            if (evaPart == null)
                Debug.Log("ERROR: Spares Container couldn't find an active EVA!");
            else
                FillEvaSuit(evaPart, this.part);

            Events["DepositParts"].active = true;

            if (!eventAdded)
            {
#if DEBUG
                Debug.Log("SPARES CONTAINER: adding the OnCrewBoardVessel event"); 
#endif
                GameEvents.onCrewBoardVessel.Add(OnCrewBoardVessel);
                eventAdded = true;
            }
        }



        [KSPEvent(guiActiveUnfocused = true, unfocusedRange = DangIt.EvaRepairDistance, guiName = "Deposit spares", active = false)]
        public void DepositParts()
        {
            Part evaPart = DangIt.FindEVAPart();
            if (evaPart == null)
                Debug.Log("ERROR: Spares Container couldn't find an active EVA!");
            else
                EmptyEvaSuit(evaPart, this.part);

            Events["DepositParts"].active = false;

            GameEvents.onCrewBoardVessel.Remove(OnCrewBoardVessel);
            eventAdded = false;
        }

        /*
        [KSPEvent(guiActiveUnfocused = true, unfocusedRange = DangIt.EvaRepairDistance, guiName = "Show perks", active = true)]
        public void ShowPerks()
        {
            this.Log("ShowPerks activated");
            ProtoCrewMember kerbal;

            try { kerbal = DangIt.FindEVAProtoCrewMember(); }
            catch (Exception e)
            {
                this.Log(e.Message);
                return;
            }

            if (kerbal == null)
                this.Log("ERROR: EVA kerbal not found!");
            else
            {
                ConfigNode kerbalFile = CrewFiles.CrewFiles.Instance.GetKerbalFile(kerbal);
                DangIt.Broadcast(kerbalFile.GetNode("PERKS").ToString());
            }

        }
        */


        protected void EmptyEvaSuit(Part evaPart, Part container)
        {
#if DEBUG
            Debug.Log("SPARES CONTAINER: Emptying the EVA suit from " + evaPart.name + " to " + container.name);
#endif
            // Compute how much can be left in the container
            double capacity = container.Resources[DangIt.Spares.Name].maxAmount - container.Resources[DangIt.Spares.Name].amount;
            double deposit = Math.Min(evaPart.Resources[DangIt.Spares.Name].amount, capacity);

            // Add it to the spares container and drain it from the EVA part
            container.RequestResource(DangIt.Spares.Name, -deposit);
            evaPart.RequestResource(DangIt.Spares.Name, deposit);

            // GUI acknowledge
            DangIt.Broadcast(evaPart.name + " has left " + deposit + " spares", 1f);
            ResourceDisplay.Instance.Refresh();
        }



        protected void FillEvaSuit(Part evaPart, Part container)
        {
            // Check if the EVA part contains the spare parts resource: if not, add a new config node
            if (!evaPart.Resources.Contains(DangIt.Spares.Name))
            {
#if DEBUG
                Debug.Log("SPARES CONTAINER: the eva part doesn't contain spares, adding the config node"); 
#endif
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", DangIt.Spares.Name);
                node.AddValue("maxAmount", DangIt.Spares.MaxEvaAmount);
                node.AddValue("amount", 0);
                evaPart.Resources.Add(node);
            }


            // Compute how much the kerbal can take
            double desired = Math.Min(DangIt.Spares.MaxEvaAmount - evaPart.Resources[DangIt.Spares.Name].amount, DangIt.Spares.Increment);
            double amountTaken = Math.Min(desired, container.Resources[DangIt.Spares.Name].amount);

            // Take it from the container and add it to the EVA
            container.RequestResource(DangIt.Spares.Name, amountTaken);
            evaPart.RequestResource(DangIt.Spares.Name, -amountTaken);

            // GUI stuff
            DangIt.Broadcast(evaPart.name + " has taken " + amountTaken + " spares", 1f);
            ResourceDisplay.Instance.Refresh();
        }


        /// <summary>
        /// When the kerbal boards a vessel, leave the spare parts in the command pod
        /// </summary>
        private void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> action)
        {
#if DEBUG
            Debug.Log("SPARES CONTAINER: OnCrewBoardVessel, emptying the EVA suit");
#endif
            Part evaPart = action.from;
            Part container = action.to;

            if (evaPart.Resources.Contains(DangIt.Spares.Name))
                EmptyEvaSuit(evaPart, container);
        }


        public void Log(string msg)
        {
            Vessel v = this.part.vessel;
            StringBuilder sb = new StringBuilder();

            sb.Append("[DangIt]: ");
            sb.Append("SparesContainer");
            sb.Append("[" + this.GetInstanceID() + "]");
            sb.Append(": " + msg);

            Debug.Log(sb.ToString());
        }


    }
}
