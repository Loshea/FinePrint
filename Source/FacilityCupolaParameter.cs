﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using KSP;
using KSPAchievements;

namespace FinePrint.Contracts.Parameters
{
    public class FacilityCupolaParameter : ContractParameter
    {
        public FacilityCupolaParameter()
        {

        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }

        protected override string GetTitle()
        {
            return "Have a research lab at the facility";
        }

        protected override void OnRegister()
        {
            this.DisableOnStateChange = false;
        }

        protected override void OnUpdate()
        {
            if (this.Root.ContractState == Contract.State.Active)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (FlightGlobals.ready)
                    {
                        bool lab = (Util.shipHasModule<ModuleScienceLab>());

                        if (this.State == ParameterState.Incomplete)
                        {
                            if (lab)
                                base.SetComplete();
                        }

                        if (this.State == ParameterState.Complete)
                        {
                            if (!lab)
                                base.SetIncomplete();
                        }
                    }
                }
            }
        }
    }
}