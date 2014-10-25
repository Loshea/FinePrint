using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using KSP;
using KSPAchievements;
using FinePrint.Contracts.Parameters;

namespace FinePrint.Contracts
{
    /// <summary>
    /// This Contract type requests non-KSA kerbals be taken
    /// into various scenairos, from High Altitude, to Orbit %CelestialBosy%, and 
    /// can apply constraints on duration, G-Load, and stability of ride.
    /// This is going to be very close to the ArialContract.  I may look at
    /// SubClassing, once i get more comfortable.  For now, though, A tourist
    /// Wants either a sub-orbital or LKO ride.
    /// </summary>
    class TouristContract : Contract
    {
        KerbalRoster newTouristRoster=null;
        CelestialBody targetBody = null;
		double minAltitude = 0.0;
		double maxAltitude = 2000.0;
        double centerLatitude = 0.0;
        double centerLongitude = 0.0;
        private bool isLowAltitude = false;
        private string MinOrMax = "Max";
        private double sustainedGs = 9.0;


        protected override void OnAccepted()
        {
            ProtoCrewMember touristKerbal= newTouristRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Tourist);
            touristKerbal.name = Util.randomKerbalName(1);
            Debug.Log("Generated a new " + touristKerbal.type + " named " + touristKerbal.name);
            base.OnAccepted();
        }

        protected override void OnCompleted()
        {
            base.OnCompleted();
        }

		protected override bool Generate()
		{
           

            int offeredContracts = 0;
            int activeContracts = 0;
            foreach (TouristContract contract in ContractSystem.Instance.GetCurrentContracts<TouristContract>())
            {
                if (contract.ContractState == Contract.State.Offered)
                    offeredContracts++;
                else if (contract.ContractState == Contract.State.Active)
                    activeContracts++;
            }

            if (offeredContracts >= FPConfig.Tourist.MaximumAvailable || activeContracts >= FPConfig.Tourist.MaximumActive)
                return false;

            double range = 10000.0;
			System.Random generator = new System.Random(this.MissionSeed);
			int additionalWaypoints = 0;
            List<CelestialBody> allBodies = GetBodies_Reached(true, false); //Make this simple - allow kerbin, but not the sun.  In fact, we should proabaly
                                                                            //Just stick with Kerbin for starters.  Would you want to ride with a Justing Beiber for
                                                                            //10 months on the way to mars?  No.  You'd kill the little fucker.
			List<CelestialBody> atmosphereBodies = new List<CelestialBody>();

			foreach (CelestialBody body in allBodies)
			{
				if (body.atmosphere)
					atmosphereBodies.Add(body);
			}

			if (atmosphereBodies.Count == 0)
				return false;

			targetBody = atmosphereBodies[generator.Next(0, atmosphereBodies.Count)];

            //TODO: Find some common ground to calculate these values automatically without specific names.
            ///One ofthe many parameters for thsi contract are going to be altitude.  After we set the altitude and orbital
            ///parameters, we'll see if there are any G restrictions to worry about. Using simple min and max height for now, but will likely
            ///Add inclination and tighen up the min/max per-contract.  For example, if we pull an orbital contract, then we'll set a minimum of 75KM
            ///but the contract may actually specify a range of 85 to 95 km - though that may be more for the science than anything else
            ///I likethe waypoints that are setup, so we may also through thows in as an ObsticalCourse type contract.  Or science.
			switch (targetBody.GetName())
			{
				case "Jool":
					
					break;
				case "Duna":
					
					break;
				case "Laythe":
					
					break;
				case "Eve":
					
					break;
				case "Kerbin":
					additionalWaypoints = 2;
					minAltitude = 75000.0;
					maxAltitude = 300000.0;
					break;
				default:
					additionalWaypoints = 0;
					minAltitude = 0.0;
					maxAltitude = 10000.0;
					break;
			}

            int waypointCount = 0;
            float fundsMultiplier = 1;
            float scienceMultiplier = 1;
            float reputationMultiplier = 1;
            float wpFundsMultiplier = 1;
            float wpScienceMultiplier = 1;
            float wpReputationMultiplier = 1;
            
            double altitudeHalfQuarterRange = Math.Abs(maxAltitude - minAltitude) * 0.125;
			double upperMidAltitude = ((maxAltitude + minAltitude) / 2.0) + altitudeHalfQuarterRange;
			double lowerMidAltitude = ((maxAltitude + minAltitude) / 2.0) - altitudeHalfQuarterRange;
			minAltitude = Math.Round((minAltitude + (generator.NextDouble() * (lowerMidAltitude - minAltitude))) / 100.0) * 100.0;
			maxAltitude = Math.Round((upperMidAltitude + (generator.NextDouble() * (maxAltitude - upperMidAltitude))) / 100.0) * 100.0;

			switch(this.prestige)
            {
                case ContractPrestige.Trivial:
				    waypointCount = FPConfig.Aerial.TrivialWaypoints;
				    waypointCount += additionalWaypoints;
                    range = FPConfig.Aerial.TrivialRange;

                    if (Util.IsGasGiant(targetBody))
                    {
                        if (generator.Next(0, 100) < FPConfig.Aerial.ExceptionalLowAltitudeChance/2)
                        {
                            minAltitude *= FPConfig.Aerial.ExceptionalLowAltitudeMultiplier;
                            maxAltitude *= FPConfig.Aerial.ExceptionalLowAltitudeMultiplier;
                            isLowAltitude = true;
                        }
                    }
                    else
                    {
                        if (generator.Next(0, 100) < FPConfig.Aerial.TrivialLowAltitudeChance)
                        {
                            minAltitude *= FPConfig.Aerial.TrivialLowAltitudeMultiplier;
                            maxAltitude *= FPConfig.Aerial.TrivialLowAltitudeMultiplier;
                            isLowAltitude = true;
                        }
                    }

                    if (generator.Next(0, 100) < FPConfig.Aerial.TrivialHomeNearbyChance && targetBody == Planetarium.fetch.Home)
                        WaypointManager.ChooseRandomPositionNear(out centerLatitude, out centerLongitude, SpaceCenter.Instance.Latitude, SpaceCenter.Instance.Longitude, targetBody.GetName(), FPConfig.Aerial.TrivialHomeNearbyRange, true);
                    else
                        WaypointManager.ChooseRandomPosition(out centerLatitude, out centerLongitude, targetBody.GetName(), true, false);

                    break;
                case ContractPrestige.Significant:
                    waypointCount = FPConfig.Aerial.SignificantWaypoints;
				    waypointCount += additionalWaypoints;
                    range = FPConfig.Aerial.SignificantRange;
                    fundsMultiplier = FPConfig.Aerial.Funds.SignificantMultiplier;
                    scienceMultiplier = FPConfig.Aerial.Science.SignificantMultiplier;
                    reputationMultiplier = FPConfig.Aerial.Reputation.SignificantMultiplier;
                    wpFundsMultiplier = FPConfig.Aerial.Funds.WaypointSignificantMultiplier;
                    wpScienceMultiplier = FPConfig.Aerial.Science.WaypointSignificantMultiplier;
                    wpReputationMultiplier = FPConfig.Aerial.Reputation.WaypointSignificantMultiplier;

                    if (Util.IsGasGiant(targetBody))
                    {
                        if (generator.Next(0, 100) < FPConfig.Aerial.SignificantLowAltitudeChance/2)
                        {
                            minAltitude *= FPConfig.Aerial.SignificantLowAltitudeMultiplier;
                            maxAltitude *= FPConfig.Aerial.SignificantLowAltitudeMultiplier;
                            isLowAltitude = true;
                        }
                    }
                    else
                    {
                        if (generator.Next(0, 100) < FPConfig.Aerial.SignificantLowAltitudeChance)
                        {
                            minAltitude *= FPConfig.Aerial.SignificantLowAltitudeMultiplier;
                            maxAltitude *= FPConfig.Aerial.SignificantLowAltitudeMultiplier;
                            isLowAltitude = true;
                        }
                    }

                    if (generator.Next(0, 100) < FPConfig.Aerial.SignificantHomeNearbyChance && targetBody == Planetarium.fetch.Home)
                        WaypointManager.ChooseRandomPositionNear(out centerLatitude, out centerLongitude, SpaceCenter.Instance.Latitude, SpaceCenter.Instance.Longitude, targetBody.GetName(), FPConfig.Aerial.SignificantHomeNearbyRange, true);
                    else
                        WaypointManager.ChooseRandomPosition(out centerLatitude, out centerLongitude, targetBody.GetName(), true, false);

                    break;
                case ContractPrestige.Exceptional:
                    waypointCount = FPConfig.Aerial.ExceptionalWaypoints;
				    waypointCount += additionalWaypoints;
                    range = FPConfig.Aerial.ExceptionalRange;
                    fundsMultiplier = FPConfig.Aerial.Funds.ExceptionalMultiplier;
                    scienceMultiplier = FPConfig.Aerial.Science.ExceptionalMultiplier;
                    reputationMultiplier = FPConfig.Aerial.Reputation.ExceptionalMultiplier;
                    wpFundsMultiplier = FPConfig.Aerial.Funds.WaypointExceptionalMultiplier;
                    wpScienceMultiplier = FPConfig.Aerial.Science.WaypointExceptionalMultiplier;
                    wpReputationMultiplier = FPConfig.Aerial.Reputation.WaypointExceptionalMultiplier;

                    if (Util.IsGasGiant(targetBody))
                    {
                        if (generator.Next(0, 100) < FPConfig.Aerial.TrivialLowAltitudeChance/2)
                        {
                            minAltitude *= FPConfig.Aerial.TrivialLowAltitudeMultiplier;
                            maxAltitude *= FPConfig.Aerial.TrivialLowAltitudeMultiplier;
                            isLowAltitude = true;
                        }
                    }
                    else
                    {
                        if (generator.Next(0, 100) < FPConfig.Aerial.ExceptionalLowAltitudeChance)
                        {
                            minAltitude *= FPConfig.Aerial.ExceptionalLowAltitudeMultiplier;
                            maxAltitude *= FPConfig.Aerial.ExceptionalLowAltitudeMultiplier;
                            isLowAltitude = true;
                        }
                    }

                    if (generator.Next(0, 100) < FPConfig.Aerial.ExceptionalHomeNearbyChance && targetBody == Planetarium.fetch.Home)
                        WaypointManager.ChooseRandomPositionNear(out centerLatitude, out centerLongitude, SpaceCenter.Instance.Latitude, SpaceCenter.Instance.Longitude, targetBody.GetName(), FPConfig.Aerial.ExceptionalHomeNearbyRange, true);
                    else
                        WaypointManager.ChooseRandomPosition(out centerLatitude, out centerLongitude, targetBody.GetName(), true, false);

                    break;
            }

			for (int x = 0; x < waypointCount; x++)
			{
				ContractParameter newParameter;
				newParameter = this.AddParameter(new FlightWaypointParameter(x, targetBody, minAltitude, maxAltitude, centerLatitude, centerLongitude, range), null);
				newParameter.SetFunds(Mathf.Round(FPConfig.Aerial.Funds.WaypointBaseReward * wpFundsMultiplier), targetBody);
                newParameter.SetReputation(Mathf.Round(FPConfig.Aerial.Reputation.WaypointBaseReward * wpReputationMultiplier), targetBody);
                newParameter.SetScience(Mathf.Round(FPConfig.Aerial.Science.WaypointBaseReward * wpScienceMultiplier), targetBody);
			}

			base.AddKeywords(new string[] { "surveyflight" });
            base.SetExpiry(FPConfig.Aerial.Expire.MinimumExpireDays, FPConfig.Aerial.Expire.MaximumExpireDays);
            base.SetDeadlineDays(FPConfig.Aerial.Expire.DeadlineDays, targetBody);
            base.SetFunds(Mathf.Round(FPConfig.Aerial.Funds.BaseAdvance * fundsMultiplier), Mathf.Round(FPConfig.Aerial.Funds.BaseReward * fundsMultiplier), Mathf.Round(FPConfig.Aerial.Funds.BaseFailure * fundsMultiplier), targetBody);
            base.SetScience(Mathf.Round(FPConfig.Aerial.Science.BaseReward * scienceMultiplier), targetBody);
            base.SetReputation(Mathf.Round(FPConfig.Aerial.Reputation.BaseReward * reputationMultiplier), Mathf.Round(FPConfig.Aerial.Reputation.BaseFailure * reputationMultiplier), targetBody);
			return true;
		}

		public override bool CanBeCancelled()
		{
			return true;
		}

		public override bool CanBeDeclined()
		{
			return true;
		}

		protected override string GetHashString()
		{
			return (this.MissionSeed.ToString() + this.DateAccepted.ToString());
		}

		protected override string GetTitle()
		{
            return "Take a tourist  into space around " + targetBody.theName + " at an altitude of " + (int)minAltitude + " to " + (int)maxAltitude + ".";
		}

		protected override string GetDescription()
		{
			//those 3 strings appear to do nothing
			return TextGen.GenerateBackStories(Agent.Name, Agent.GetMindsetString(), "Sight Seeing", "Take a tourist for a spin in space", "tourist", new System.Random().Next());
		}

		protected override string GetSynopsys()
		{
            return "In order to increase our reputation with the public, and to be able to continue to fund Jeb's snacks habit, we have decided to open up a space tourism service.  We want you to take a tourist into orbit around " + targetBody.theName+".\nNeedless to say,they should be brought back alive.";

		}

		protected override string MessageCompleted()
		{
            return "Your passenger has enjoyed their orbit around " + targetBody.theName + ", and will be recommending you to their friends!";
		}

        protected override string GetNotes()
        {
            string notes = "";
            notes += "Your passenger has requested a " + MinOrMax + "G-Load of " + sustainedGs + " for this flight.";
           /*if (Util.IsGasGiant(targetBody) && !isLowAltitude)
                notes += "Warning: this contract specifies flight in the atmosphere of a gas giant.";
            else if (Util.IsGasGiant(targetBody) && isLowAltitude)
                notes += "Warning: this is a low altitude flight contract for a gas giant. We recommend sending cheap unmanned probes on a one way trip.";
            else
                return null;

            //In Gene's dialogue, the notes smush up against the parameters. Add one new line.
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                notes += "\n";*/

            return notes;
        }

		protected override void OnLoad(ConfigNode node)
		{
            Util.CheckForPatchReset();
			Util.LoadNode(node, "TouristContract", "targetBody", ref targetBody, Planetarium.fetch.Home);
			Util.LoadNode(node, "TouristContract", "minAltitude", ref minAltitude, 0.0);
            Util.LoadNode(node, "TouristContract", "maxAltitude", ref maxAltitude, 10000);
            Util.LoadNode(node, "TouristContract", "MinOrMax", ref MinOrMax, "Max");
            Util.LoadNode(node, "TouristContract", "sustainedGs", ref sustainedGs, 9.0);
            Util.LoadNode(node, "TouristContract", "centerLatitude", ref centerLatitude, 0.0);
            Util.LoadNode(node, "TouristContract", "centerLongitude", ref centerLongitude, 0.0);
            Util.LoadNode(node, "TouristContract", "isLowAltitude", ref isLowAltitude, false);
		}

		protected override void OnSave(ConfigNode node)
		{
			int bodyID = targetBody.flightGlobalsIndex;
			node.AddValue("targetBody", bodyID);
			node.AddValue("minAltitude", minAltitude);
			node.AddValue("maxAltitude", maxAltitude);
            node.AddValue("centerLatitude", centerLatitude);
            node.AddValue("centerLongitude", centerLongitude);
            node.AddValue("isLowAltitude", isLowAltitude);
		}

		public override bool MeetRequirements()
		{
            return true;
		}

    }
}
