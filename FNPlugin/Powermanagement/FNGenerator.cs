using FNPlugin.Extensions;
using FNPlugin.Redist;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TweakScale;
using UnityEngine;

namespace FNPlugin
{
    enum PowerStates { PowerOnline, PowerOffline };

    [KSPModule("Super Capacitator")]
    class KspiSuperCapacitator : PartModule
    {
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Max Capacity", guiUnits = " MWe")]
        public float maxStorageCapacityMJ = 0;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Mass", guiUnits = " t")]
        public float partMass = 0;
    }



    [KSPModule("Thermal Electric Effect Generator")]
    class ThermalElectricEffectGenerator : FNGenerator {}

    [KSPModule("Integrated Thermal Electric Power Generator")]
    class IntegratedThermalElectricPowerGenerator : FNGenerator { }

    [KSPModule("Thermal Electric Power Generator")]
    class ThermalElectricPowerGenerator : FNGenerator {}

    [KSPModule("Integrated Charged Particles Power Generator")]
    class IntegratedChargedParticlesPowerGenerator : FNGenerator {}

    [KSPModule("Charged Particles Power Generator")]
    class ChargedParticlesPowerGenerator : FNGenerator {}

    [KSPModule(" Generator")]
    class FNGenerator : ResourceSuppliableModule, IUpgradeableModule, IElectricPowerGeneratorSource, IPartMassModifier, IRescalable<FNGenerator>
    {
        // Persistent
        [KSPField(isPersistant = true)]
        public bool IsEnabled = true;
        [KSPField(isPersistant = true)]
        public bool generatorInit = false;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public bool chargedParticleMode = false;
        [KSPField(isPersistant = true)]
        public float storedMassMultiplier;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_powerCapacity"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerCapacity = 100;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_powerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerPercentage = 100;

        // Settings
        [KSPField]
        public bool isMHD = false;
        [KSPField]
        public bool isLimitedByMinThrotle = false;
        [KSPField]
        public double powerOutputMultiplier = 1;
        [KSPField]
        public double hotColdBathRatio;
        [KSPField]
        public bool calculatedMass = false;
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public string originalName = "";
        [KSPField]
        public double pCarnotEff = 0.32;
        [KSPField]
        public double upgradedpCarnotEff = 0.64;
        [KSPField]
        public double directConversionEff = 0.6;
        [KSPField]
        public double upgradedDirectConversionEff = 0.865;
        [KSPField]
        public double efficiencyMk1 = 0;
        [KSPField]
        public double efficiencyMk2 = 0;
        [KSPField]
        public double efficiencyMk3 = 0;
        [KSPField]
        public double efficiencyMk4 = 0;
        [KSPField]
        public double efficiencyMk5 = 0;
        [KSPField]
        public double efficiencyMk6 = 0;
        [KSPField]
        public string Mk2TechReq = "";
        [KSPField]
        public string Mk3TechReq = "";
        [KSPField]
        public string Mk4TechReq = "";
        [KSPField]
        public string Mk5TechReq = "";
        [KSPField]
        public string Mk6TechReq = "";
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_maxGeneratorEfficiency")]
        public double maxEfficiency = 0;
        [KSPField]
        public string animName = "";
        [KSPField]
        public string upgradeTechReq = "";
        [KSPField]
        public float upgradeCost = 1;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_radius")]
        public double radius = 2.5;
        [KSPField]
        public string altUpgradedName = "";
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public bool maintainsMegaWattPowerBuffer = true;
        [KSPField]
        public bool fullPowerBuffer = false;
        [KSPField]
        public bool showSpecialisedUI = true;

        /// <summary>
        /// MW Power to part mass divider, need to be lower for SETI/NFE mode 
        /// </summary>
        [KSPField]
        public double rawPowerToMassDivider = 1000;
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_maxUsageRatio")]
        public double powerUsageEfficiency;
        [KSPField]
        public double massModifier = 1;
        [KSPField]
        public double rawMaximumPower;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_maxTheoreticalPower", guiFormat = "F3")]
        public string maximumTheoreticalPower;
        [KSPField]
        public double coreTemperateHotBathExponent = 0.75;
        [KSPField]
        public double capacityToMassExponent = 0.7;

        // Debugging
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_partMass", guiUnits = " t", guiFormat = "F3")]
        public float partMass;
        [KSPField]
        public double targetMass;
        [KSPField]
        public float initialMass;
        [KSPField]
        public double megajouleBarRatio;
        [KSPField]
        public double megajoulePecentage;

        // GUI
        [KSPField]
        public double rawThermalPower;
        [KSPField]
        public double rawChargedPower;
        [KSPField]
        public double rawReactorPower;
        [KSPField]
        public double maxThermalPower;
        [KSPField]
        public double maxChargedPower;
        [KSPField]
        public double maxReactorPower;
        [KSPField]
        public double potentialThermalPower;
        [KSPField]
        public double adjusted_thermal_power_needed;
        [KSPField]
        public double attachedPowerSourceRatio;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Generator_currentElectricPower", guiUnits = " MW_e", guiFormat = "F3")]
        public string OutputPower;
        [KSPField]
        public string MaxPowerStr;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Generator_CurrentElectricEfficiency")]
        public string OverallEfficiency;
        [KSPField]
        public string upgradeCostStr = "";
        [KSPField]
        public double heat_exchanger_thrust_divisor;
        [KSPField]
        public double requested_power_per_second;
        [KSPField]
        public double received_power_per_second;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Generator_coldBathTemp", guiUnits = " K", guiFormat = "F1")]
        public double coldBathTemp = 500;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Generator_hotBathTemp", guiUnits = " K", guiFormat = "F1")]
        public double hotBathTemp = 300;
        [KSPField]
        public double spareResourceCapacity;
        [KSPField]
        public double possibleSpareResourceCapacityFilling;
        [KSPField]
        public double currentUnfilledResourceDemand;
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = " MW", guiFormat = "F3")]
        public double electrical_power_currently_needed;
        [KSPField]
        public double maxStableMegaWattPower;
        [KSPField]
        public bool applies_balance;

        // Internal
        protected double outputPower;
        protected double _totalEff;
        protected double powerDownFraction;

        protected bool play_down = true;
        protected bool play_up = true;
        protected bool hasrequiredupgrade = false;

        protected long last_draw_update = 0;
        protected long update_count = 0;

        protected int partDistance;
        protected int shutdown_counter = 0;
        protected int startcount = 0;

        protected PowerStates _powerState;
        protected Animation anim;
        protected Queue<double> averageRadiatorTemperatureQueue = new Queue<double>();
        protected IPowerSource attachedPowerSource;
        protected ResourceBuffers resourceBuffers;

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Generator_activateGenerator", active = true)]
        public void ActivateGenerator()
        {
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Generator_deactivateGenerator", active = false)]
        public void DeactivateGenerator()
        {
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Generator_retrofitGenerator", active = true)]
        public void RetrofitGenerator()
        {
            if (ResearchAndDevelopment.Instance == null) return;

            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        [KSPAction("#LOC_KSPIE_Generator_activateGenerator")]
        public void ActivateGeneratorAction(KSPActionParam param)
        {
            ActivateGenerator();
        }

        [KSPAction("#LOC_KSPIE_Generator_deactivateGenerator")]
        public void DeactivateGeneratorAction(KSPActionParam param)
        {
            DeactivateGenerator();
        }

        [KSPAction("#LOC_KSPIE_Generator_toggleGenerator")]
        public void ToggleGeneratorAction(KSPActionParam param)
        {
            IsEnabled = !IsEnabled;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                Debug.Log("FNGenerator.OnRescale called with " + factor.absolute.linear);
                storedMassMultiplier = Mathf.Pow(factor.absolute.linear, 3);
                initialMass = part.prefabMass * storedMassMultiplier;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnRescale " + e.Message);
            }
        }

        public void Refresh()
        {
            Debug.Log("FNGenerator Refreshed");
            UpdateTargetMass();
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (!calculatedMass)
                return 0;

            var moduleMassDelta = (float)targetMass - initialMass;

            return moduleMassDelta;
        }

        public void upgradePartModule()
        {
            isupgraded = true;
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI] - attach " + part.partInfo.title);
                FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnEditorAttach " + e.Message);
            }
        }

        /// <summary>
        /// Event handler which is called when part is deptach to a exiting part
        /// </summary>
        private void OnEditorDetach()
        {
            try
            {
                if (attachedPowerSource == null)
                    return;

                Debug.Log("[KSPI] - detach " + part.partInfo.title);
                if (chargedParticleMode && attachedPowerSource.ConnectedChargedParticleElectricGenerator != null)
                    attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                if (!chargedParticleMode && attachedPowerSource.ConnectedThermalElectricGenerator != null)
                    attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.OnEditorDetach " + e.Message);
            }
        }

        private void OnDestroyed()
        {
            try
            {
                OnEditorDetach();

                RemoveItselfAsManager();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception on " + part.name + " durring FNGenerator.OnDestroyed with message " + e.Message);
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            String[] resources_to_supply = { ResourceManager.FNRESOURCE_MEGAJOULES, ResourceManager.FNRESOURCE_WASTEHEAT, ResourceManager.FNRESOURCE_THERMALPOWER, ResourceManager.FNRESOURCE_CHARGED_PARTICLES };
            this.resources_to_supply = resources_to_supply;

            resourceBuffers = new ResourceBuffers();
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_WASTEHEAT, wasteHeatMultiplier, 2.0e+5, true));
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.FNRESOURCE_MEGAJOULES));
            resourceBuffers.AddConfiguration(new ResourceBuffers.TimeBasedConfig(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, 1000 / powerOutputMultiplier));
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.Init(this.part);

            base.OnStart(state);

            targetMass = part.prefabMass * storedMassMultiplier;
            initialMass = part.prefabMass * storedMassMultiplier;

            if (initialMass == 0)
                initialMass = part.prefabMass;
            if (targetMass == 0)
                targetMass = part.prefabMass;

            InitializeEfficiency();
            Fields["powerCapacity"].guiActiveEditor = !isLimitedByMinThrotle;
            Fields["partMass"].guiActive = Fields["partMass"].guiActiveEditor = calculatedMass;
            Fields["powerPercentage"].guiActive = Fields["powerPercentage"].guiActiveEditor = showSpecialisedUI;
            Fields["radius"].guiActiveEditor = showSpecialisedUI;

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    hasrequiredupgrade = true;
                    upgradePartModule();
                }
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
                part.OnEditorDestroy += OnEditorDetach;

                part.OnJustAboutToBeDestroyed += OnDestroyed;
                part.OnJustAboutToDie += OnDestroyed;

                FindAndAttachToPowerSource();
                return;
            }

            if (this.HasTechsRequiredToUpgrade())
                hasrequiredupgrade = true;

            // only force activate if no certain partmodules are not present and not limited by minimum throtle
            if (!isLimitedByMinThrotle && part.FindModuleImplementing<MicrowavePowerReceiver>() == null)
            {
                Debug.Log("[KSPI] - Generator on " + part.name + " was Force Activated");
                part.force_activate();
            }

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if (anim != null)
            {
                anim[animName].layer = 1;
                if (!IsEnabled)
                {
                    anim[animName].normalizedTime = 1;
                    anim[animName].speed = -1;
                }
                else
                {
                    anim[animName].normalizedTime = 0;
                    anim[animName].speed = 1;
                }
                anim.Play();
            }

            if (generatorInit == false)
            {
                IsEnabled = true;
            }

            if (isupgraded)
                upgradePartModule();

            FindAndAttachToPowerSource();

            UpdateHeatExchangedThrustDivisor();
        }

        private void InitializeEfficiency()
        {
            if (chargedParticleMode && efficiencyMk1 == 0)
                efficiencyMk1 = directConversionEff;
            else if (!chargedParticleMode && efficiencyMk1 == 0)
                efficiencyMk1 = pCarnotEff;

            if (chargedParticleMode && efficiencyMk2 == 0)
                efficiencyMk2 = upgradedDirectConversionEff;
            else if (!chargedParticleMode && efficiencyMk2 == 0)
                efficiencyMk2 = upgradedpCarnotEff;

            if (efficiencyMk3 == 0)
                efficiencyMk3 = efficiencyMk2;
            if (efficiencyMk4 == 0)
                efficiencyMk4 = efficiencyMk3;
            if (efficiencyMk5 == 0)
                efficiencyMk5 = efficiencyMk4;
            if (efficiencyMk6 == 0)
                efficiencyMk6 = efficiencyMk5;

            if (String.IsNullOrEmpty(Mk2TechReq))
                Mk2TechReq = upgradeTechReq;

            int techLevel = 1;
            if (PluginHelper.UpgradeAvailable(Mk6TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk5TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk4TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk3TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk2TechReq))
                techLevel++;

            if (techLevel == 6)
                maxEfficiency = efficiencyMk6;
            else if (techLevel == 5)
                maxEfficiency = efficiencyMk5;
            else if (techLevel == 4)
                maxEfficiency = efficiencyMk4;
            else if (techLevel == 3)
                maxEfficiency = efficiencyMk3;
            else if (techLevel == 2)
                maxEfficiency = efficiencyMk2;
            else
                maxEfficiency = efficiencyMk1;
        }

        /// <summary>
        /// Finds the nearest avialable thermalsource and update effective part mass
        /// </summary>
        public void FindAndAttachToPowerSource()
        {
            partDistance = 0;

            // disconnect
            if (attachedPowerSource != null)
            {
                if (chargedParticleMode)
                    attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                else
                    attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }

            // first look if part contains an thermal source
            attachedPowerSource = part.FindModulesImplementing<IPowerSource>().FirstOrDefault();
            if (attachedPowerSource != null)
            {
                ConnectToPowerSource();
                Debug.Log("[KSPI] - Found power source localy");
                return;
            }

            if (!part.attachNodes.Any() || part.attachNodes.All(m => m.attachedPart == null))
            {
                Debug.Log("[KSPI] - not connected to any parts yet");
                UpdateTargetMass();
                return;
            }

            Debug.Log("[KSPI] - generator is currently connected to " + part.attachNodes.Count + " parts");
            // otherwise look for other non selfcontained thermal sources that is not already connected

            var searchResult = chargedParticleMode 
                ? FindChargedParticleSource()
                : isMHD 
                    ? FindPlasmaPowerSource() 
                    : FindThermalPowerSource();

            // quit if we failed to find anything
            if (searchResult == null)
            {
                Debug.LogWarning("[KSPI] - Failed to find power source");
                return;
            }

            // verify cost is not higher than 1
            partDistance = (int)Math.Max(Math.Ceiling(searchResult.Cost), 0);
            if (partDistance > 1)
            {
                Debug.LogWarning("[KSPI] - Found power source but at too high cost");
                return;
            }

            // update attached thermalsource
            attachedPowerSource = searchResult.Source;

            Debug.Log("[KSPI] - succesfully connected to " + attachedPowerSource.Part.partInfo.title);

            ConnectToPowerSource();
        }

        private void ConnectToPowerSource()
        {
            //connect with source
            if (chargedParticleMode)
                attachedPowerSource.ConnectedChargedParticleElectricGenerator = this;
            else
                attachedPowerSource.ConnectedThermalElectricGenerator = this;

            UpdateTargetMass();
        }

        private PowerSourceSearchResult FindThermalPowerSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                    p => p.IsThermalSource
                        && p.ConnectedThermalElectricGenerator == null
                        && p.ThermalEnergyEfficiency > 0, 3, 3, 3, true);
            return searchResult;
        }

        private PowerSourceSearchResult FindPlasmaPowerSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                     p => p.IsThermalSource
                         && p.ConnectedChargedParticleElectricGenerator == null
                         && p.PlasmaEnergyEfficiency > 0, 3, 3, 3, true);
            return searchResult;
        }

        private PowerSourceSearchResult FindChargedParticleSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                     p => p.IsThermalSource
                         && p.ConnectedChargedParticleElectricGenerator == null
                         && p.ChargedParticleEnergyEfficiency > 0, 3, 3, 3, true);
            return searchResult;
        }

        private void UpdateTargetMass()
        {
            try
            {
                if (attachedPowerSource == null)
                {
                    targetMass = initialMass;
                    return;
                }

                if (chargedParticleMode && attachedPowerSource.ChargedParticleEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.ChargedParticleEnergyEfficiency;
                else if (isMHD && attachedPowerSource.PlasmaEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.PlasmaEnergyEfficiency;
                else if (attachedPowerSource.ThermalEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.ThermalEnergyEfficiency;
                else
                    powerUsageEfficiency = 1;

                rawMaximumPower = attachedPowerSource.RawMaximumPower * powerUsageEfficiency;
                maximumTheoreticalPower = PluginHelper.getFormattedPowerString(rawMaximumPower * CapacityRatio * maxEfficiency);

                // verify if mass calculation is active
                if (!calculatedMass)
                {
                    targetMass = initialMass;
                    return;
                }

                // update part mass
                if (rawMaximumPower > 0 && rawPowerToMassDivider > 0)
                    targetMass = (massModifier * attachedPowerSource.ThermalProcessingModifier * rawMaximumPower * Math.Pow(CapacityRatio, capacityToMassExponent)) / rawPowerToMassDivider;
                else
                    targetMass = initialMass;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - FNGenerator.UpdateTargetMass " + e.Message);
            }
        }

        public double CapacityRatio
        {
            get { return (double)(decimal)(powerCapacity / 100); }
        }

        public double PowerRatio
        {
            get { return (double)(decimal)(powerPercentage / 100); }
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            Events["ActivateGenerator"].active = !IsEnabled && showSpecialisedUI;
            Events["DeactivateGenerator"].active = IsEnabled && showSpecialisedUI;

            Fields["OverallEfficiency"].guiActive = IsEnabled;
            Fields["MaxPowerStr"].guiActive = IsEnabled;
            Fields["coldBathTemp"].guiActive = !chargedParticleMode;
            Fields["hotBathTemp"].guiActive = !chargedParticleMode;

            if (ResearchAndDevelopment.Instance != null)
            {
                Events["RetrofitGenerator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
            }
            else
                Events["RetrofitGenerator"].active = false;

            if (IsEnabled)
            {
                if (play_up && anim != null)
                {
                    play_down = true;
                    play_up = false;
                    anim[animName].speed = 1;
                    anim[animName].normalizedTime = 0;
                    anim.Blend(animName, 2);
                }
            }
            else
            {
                if (play_down && anim != null)
                {
                    play_down = false;
                    play_up = true;
                    anim[animName].speed = -1;
                    anim[animName].normalizedTime = 1;
                    anim.Blend(animName, 2);
                }
            }

            if (IsEnabled)
            {
                var percentOutputPower = _totalEff * 100.0;
                var outputPowerReport = -outputPower;
                if (update_count - last_draw_update > 10)
                {
                    OutputPower = PluginHelper.getFormattedPowerString(outputPowerReport, "0.0", "0.000");
                    OverallEfficiency = percentOutputPower.ToString("0.00") + "%";

                    MaxPowerStr = (_totalEff >= 0)
                        ? !chargedParticleMode
                            ? PluginHelper.getFormattedPowerString(maxThermalPower * _totalEff * PowerRatio * CapacityRatio, "0.0", "0.000")
                            : PluginHelper.getFormattedPowerString(maxChargedPower * _totalEff * PowerRatio * CapacityRatio, "0.0", "0.000")
                        : 0 + "MW";

                    last_draw_update = update_count;
                }
            }
            else
                OutputPower = "Generator Offline";

            update_count++;
        }

        #region obsolete exposed public getters

        //public double getMaxPowerOutput()
        //{
        //	if (chargedParticleMode)
        //		return maxChargedPower * _totalEff;
        //	else
        //		return maxThermalPower * _totalEff;
        //}

        //public bool isActive() { return IsEnabled; }

        //public IPowerSource getThermalSource() { return attachedPowerSource; }

        # endregion

        public double MaxStableMegaWattPower
        {
            get
            {
                if (attachedPowerSource == null || !IsEnabled)
                    return 0;

                var maxPowerUsageRatio =
                    chargedParticleMode
                        ? attachedPowerSource.ChargedParticleEnergyEfficiency
                        : isMHD
                            ? attachedPowerSource.PlasmaEnergyEfficiency
                            : attachedPowerSource.ThermalEnergyEfficiency;

                return attachedPowerSource.StableMaximumReactorPower * attachedPowerSource.PowerRatio * maxPowerUsageRatio * maxEfficiency * CapacityRatio;
            }
        }        

        private void UpdateHeatExchangedThrustDivisor()
        {
            if (attachedPowerSource == null) return;

            if (attachedPowerSource.Radius <= 0 || radius <= 0)
            {
                heat_exchanger_thrust_divisor = 1;
                return;
            }

            heat_exchanger_thrust_divisor = radius > attachedPowerSource.Radius
                ? attachedPowerSource.Radius * attachedPowerSource.Radius / radius / radius
                : radius * radius / attachedPowerSource.Radius / attachedPowerSource.Radius;
        }

        private void UpdateGeneratorPower()
        {
            if (attachedPowerSource == null) return;

            if (!chargedParticleMode) // thermal or plasma mode
            {
                var chargedPowerModifier = attachedPowerSource.ChargedPowerRatio * attachedPowerSource.ChargedPowerRatio;

                var plasmaTemperature = attachedPowerSource.CoreTemperature <= attachedPowerSource.HotBathTemperature
                    ? attachedPowerSource.CoreTemperature
                    : attachedPowerSource.HotBathTemperature + Math.Pow(attachedPowerSource.CoreTemperature - attachedPowerSource.HotBathTemperature, coreTemperateHotBathExponent);

                hotBathTemp = applies_balance || !isMHD
                    ? attachedPowerSource.HotBathTemperature 
                    : attachedPowerSource.SupportMHD 
                        ? plasmaTemperature
                        : plasmaTemperature * chargedPowerModifier + (1 - chargedPowerModifier) * attachedPowerSource.HotBathTemperature;	// for fusion reactors connected to MHD

                averageRadiatorTemperatureQueue.Enqueue(FNRadiator.getAverageRadiatorTemperatureForVessel(vessel) * 0.75);

                while (averageRadiatorTemperatureQueue.Count > 10)
                    averageRadiatorTemperatureQueue.Dequeue();

                coldBathTemp = averageRadiatorTemperatureQueue.Average();
            }

            if (HighLogic.LoadedSceneIsEditor)
                UpdateHeatExchangedThrustDivisor();

            attachedPowerSourceRatio = attachedPowerSource.PowerRatio;

            rawThermalPower = isLimitedByMinThrotle
                    ? attachedPowerSource.MinimumPower
                    : attachedPowerSource.MaximumThermalPower * PowerRatio * CapacityRatio;
            rawChargedPower = attachedPowerSource.MaximumChargedPower * PowerRatio * CapacityRatio;

            maxChargedPower = rawChargedPower;
            maxThermalPower = rawThermalPower;
            rawReactorPower = rawThermalPower + rawChargedPower;

            maxReactorPower = rawReactorPower;

            if (!(attachedPowerSourceRatio > 0)) return;

            var maximumPowerUsageRatio = isMHD
                ? attachedPowerSource.PlasmaEnergyEfficiency
                : attachedPowerSource.ThermalEnergyEfficiency;

            potentialThermalPower = ((applies_balance ? maxThermalPower : rawReactorPower) / attachedPowerSourceRatio) * maximumPowerUsageRatio;

            maxThermalPower = Math.Min(maxReactorPower, potentialThermalPower);
            maxChargedPower = Math.Min(maxChargedPower, (maxChargedPower / attachedPowerSourceRatio) * attachedPowerSource.ChargedParticleEnergyEfficiency);
            maxReactorPower = (chargedParticleMode ? maxChargedPower : maxThermalPower) * maximumPowerUsageRatio;
        }

        // Update is called in the editor 
        public void Update()
        {
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsFlight) return;

            UpdateTargetMass();

            Fields["targetMass"].guiActive = attachedPowerSource != null && attachedPowerSource.Part != this.part;
        }

        public override void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {
            try
            {

                if (IsEnabled && attachedPowerSource != null && FNRadiator.hasRadiatorsForVessel(vessel))
                {
                    UpdateGeneratorPower();

                    // check if MaxStableMegaWattPower is changed
                    maxStableMegaWattPower = MaxStableMegaWattPower;

                    UpdateBuffers();

                    generatorInit = true;

                    // don't produce any power when our reactor has stopped
                    if (maxStableMegaWattPower <= 0)
                    {
                        PowerDown();
                        return;
                    }
                    powerDownFraction = 1;

                    double electricdtps;
                    double maxElectricdtps;

                    if (!chargedParticleMode) // thermal mode
                    {
                        hotColdBathRatio = Math.Max(Math.Min(1 - coldBathTemp / hotBathTemp, 1), 0);

                        _totalEff = Math.Min(maxEfficiency, hotColdBathRatio * maxEfficiency);

                        if (_totalEff <= 0.01 || coldBathTemp <= 0 || hotBathTemp <= 0 || maxThermalPower <= 0)
                        {
                            requested_power_per_second = 0;
                            return;
                        }

                        var thermalPowerCurrentlyNeededForElectricity = CalculateElectricalPowerCurrentlyNeeded();

                        var effectiveThermalPowerNeededForElectricity = thermalPowerCurrentlyNeededForElectricity / _totalEff;

                        var chargedBufferRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                        var availableChargedPowerRatio = Math.Max(Math.Min(2 * chargedBufferRatio - 0.25, 1), 0);

                        adjusted_thermal_power_needed = effectiveThermalPowerNeededForElectricity;
                        //adjusted_thermal_power_needed = applies_balance
                        //    ? effectiveThermalPowerNeededForElectricity
                        //    : (effectiveThermalPowerNeededForElectricity * (1 - attachedPowerSource.ChargedPowerRatio)) + (effectiveThermalPowerNeededForElectricity * attachedPowerSource.ChargedPowerRatio * availableChargedPowerRatio);

                        var thermalPowerRequested = Math.Max(Math.Min(maxThermalPower, adjusted_thermal_power_needed), attachedPowerSource.MinimumPower * (1 - attachedPowerSource.ChargedPowerRatio));
                        var reactorPowerRequested = Math.Max(Math.Min(maxReactorPower, effectiveThermalPowerNeededForElectricity), attachedPowerSource.MinimumPower);

                        requested_power_per_second = thermalPowerRequested;

                        double thermalPowerReceived = 0;

                        if (attachedPowerSource.ChargedPowerRatio != 1)
                        {
                            var maximumThermalPower = attachedPowerSource.MaximumThermalPower * powerUsageEfficiency * CapacityRatio;
                            var thermalPowerRequestRatio = Math.Min(1, maximumThermalPower > 0 ? thermalPowerRequested / maximumThermalPower : 0);
                            thermalPowerReceived = consumeFNResourcePerSecond(thermalPowerRequested, ResourceManager.FNRESOURCE_THERMALPOWER);
                            attachedPowerSource.NotifyActiveThermalEnergyGenerator(_totalEff, thermalPowerRequestRatio);
                        }

                        if (attachedPowerSource.ChargedPowerRatio == 1)
                        {
                            var maximumChargedPower = attachedPowerSource.MaximumChargedPower * powerUsageEfficiency * CapacityRatio;
                            var chargedPowerRequestRatio = Math.Min(1, maximumChargedPower > 0 ? thermalPowerRequested / maximumChargedPower : 0);
                            thermalPowerReceived += consumeFNResourcePerSecond(reactorPowerRequested, ResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                            attachedPowerSource.NotifyActiveChargedEnergyGenerator(_totalEff, chargedPowerRequestRatio);
                        }
                        else  if (attachedPowerSource.EfficencyConnectedChargedEnergyGenerator == 0 && thermalPowerReceived < reactorPowerRequested && attachedPowerSource.ChargedPowerRatio > 0.001)
                        {
                            var requestedChargedPower = Math.Min(Math.Min(reactorPowerRequested - thermalPowerReceived, maxChargedPower), Math.Max(0, maxReactorPower - thermalPowerReceived)) * availableChargedPowerRatio;

                            if (requestedChargedPower < 0.000025)
                                thermalPowerReceived += requestedChargedPower;
                            else
                                thermalPowerReceived += consumeFNResourcePerSecond(requestedChargedPower, ResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                        }

                        var effectiveInputPowerPerSecond = thermalPowerReceived * _totalEff;

                        received_power_per_second = effectiveInputPowerPerSecond;

                        if (!CheatOptions.IgnoreMaxTemperature)
                            consumeFNResourcePerSecond(effectiveInputPowerPerSecond, ResourceManager.FNRESOURCE_WASTEHEAT);

                        electricdtps = Math.Max(effectiveInputPowerPerSecond * powerOutputMultiplier, 0);

                        var effectiveMaxThermalPowerRatio = applies_balance
                            ? (1 - attachedPowerSource.ChargedPowerRatio)
                            : (1 - attachedPowerSource.ChargedPowerRatio) + (attachedPowerSource.ChargedPowerRatio * availableChargedPowerRatio);

                        maxElectricdtps = effectiveMaxThermalPowerRatio * attachedPowerSource.StableMaximumReactorPower * attachedPowerSource.PowerRatio * powerUsageEfficiency * _totalEff * CapacityRatio;
                        maxElectricdtps =  Math.Max(attachedPowerSource.ProducedThermalHeat * _totalEff , maxElectricdtps);
                    }
                    else // charged particle mode
                    {
                        _totalEff = maxEfficiency;

                        if (_totalEff <= 0) return;

                        requested_power_per_second = Math.Max(Math.Min(maxChargedPower, CalculateElectricalPowerCurrentlyNeeded() / _totalEff), attachedPowerSource.MinimumPower * attachedPowerSource.ChargedPowerRatio);

                        var maximumChargedPower = attachedPowerSource.MaximumChargedPower * attachedPowerSource.ChargedParticleEnergyEfficiency;
                        var chargedPowerRequestRatio = Math.Min(1, maximumChargedPower > 0 ? requested_power_per_second / maximumChargedPower : 0);
                        attachedPowerSource.NotifyActiveChargedEnergyGenerator(_totalEff, chargedPowerRequestRatio);

                        received_power_per_second = consumeFNResourcePerSecond(requested_power_per_second, ResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                        var effectiveInputPowerPerSecond = received_power_per_second * _totalEff;

                        if (!CheatOptions.IgnoreMaxTemperature)
                            consumeFNResourcePerSecond(effectiveInputPowerPerSecond, ResourceManager.FNRESOURCE_WASTEHEAT);

                        electricdtps = Math.Max(effectiveInputPowerPerSecond * powerOutputMultiplier, 0);
                        maxElectricdtps = maxChargedPower * _totalEff;
                    }

                    outputPower = isLimitedByMinThrotle 
                        ? -supplyManagedFNResourcePerSecond(electricdtps, ResourceManager.FNRESOURCE_MEGAJOULES) 
                        : -supplyFNResourcePerSecondWithMax(electricdtps, maxElectricdtps, ResourceManager.FNRESOURCE_MEGAJOULES);
                }
                else
                {
                    generatorInit = true;

                    //if (attachedPowerSource != null)
                    //    attachedPowerSource.RequestedThermalHeat = 0;

                    if (IsEnabled && !vessel.packed)
                    {
                        if (!FNRadiator.hasRadiatorsForVessel(vessel))
                        {
                            IsEnabled = false;
                            Debug.Log("[KSPI] - Generator Shutdown: No radiators available!");
                            ScreenMessages.PostScreenMessage("Generator Shutdown: No radiators available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            PowerDown();
                        }

                        if (attachedPowerSource == null)
                        {
                            IsEnabled = false;
                            Debug.Log("[KSPI] - Generator Shutdown: No reactor available!");
                            ScreenMessages.PostScreenMessage("Generator Shutdown: No reactor available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            PowerDown();
                        }
                    }
                    else
                    {
                        PowerDown();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in FNGenerator OnFixedUpdateResourceSuppliable: " + e.Message);
            }
        }

        private void UpdateBuffers()
        {
            if (!maintainsMegaWattPowerBuffer)
                return;

            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            if (maxStableMegaWattPower > 0)
            { 
                _powerState = PowerStates.PowerOnline;

                var stablePowerForBuffer = chargedParticleMode 
                       ? attachedPowerSource.ChargedPowerRatio * maxStableMegaWattPower 
                       : (1 - attachedPowerSource.ChargedPowerRatio) * maxStableMegaWattPower;

                var megawattBufferMultiplier = (attachedPowerSource.PowerBufferBonus + 1) * stablePowerForBuffer;
                resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, megawattBufferMultiplier);
                resourceBuffers.UpdateVariable(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, megawattBufferMultiplier);
            }
            resourceBuffers.UpdateBuffers();
        }

        private double CalculateElectricalPowerCurrentlyNeeded()
        {
            megajouleBarRatio = getResourceBarRatio(ResourceManager.FNRESOURCE_MEGAJOULES);
            megajoulePecentage = megajouleBarRatio * 100;

            if (isLimitedByMinThrotle)
                return attachedPowerSource.MinimumPower;

            currentUnfilledResourceDemand = GetCurrentUnfilledResourceDemand(ResourceManager.FNRESOURCE_MEGAJOULES);

            spareResourceCapacity = getSpareResourceCapacity(ResourceManager.FNRESOURCE_MEGAJOULES);
            maxStableMegaWattPower = MaxStableMegaWattPower;

            // stabalizes power at higher time warp
            var deltaTimeDivider = TimeWarp.fixedDeltaTime < 1 ? TimeWarp.fixedDeltaTime / 0.02 : TimeWarp.fixedDeltaTime * 50; 

            possibleSpareResourceCapacityFilling = Math.Min(spareResourceCapacity / deltaTimeDivider, maxStableMegaWattPower);

            applies_balance = attachedPowerSource.ShouldApplyBalance(chargedParticleMode ? ElectricGeneratorType.charged_particle : ElectricGeneratorType.thermal);

            if (applies_balance)
            {
                var chargedPowerPerformance = attachedPowerSource.EfficencyConnectedChargedEnergyGenerator * attachedPowerSource.ChargedPowerRatio;
                var thermalPowerPerformance = attachedPowerSource.EfficencyConnectedThermalEnergyGenerator * (1 - attachedPowerSource.ChargedPowerRatio);

                var totalPerformance = chargedPowerPerformance + thermalPowerPerformance;

                var balancePerformanceRatio = totalPerformance == 0 ? 0
                    : chargedParticleMode
                        ? chargedPowerPerformance / totalPerformance
                        : thermalPowerPerformance / totalPerformance;

                electrical_power_currently_needed = (currentUnfilledResourceDemand + possibleSpareResourceCapacityFilling) * balancePerformanceRatio;
            }
            else
            {
                electrical_power_currently_needed = currentUnfilledResourceDemand + possibleSpareResourceCapacityFilling;
            }

            return electrical_power_currently_needed;
        }

        private void PowerDown()
        {
            if (_powerState == PowerStates.PowerOffline) return;

            if (powerDownFraction > 0)
                powerDownFraction -= 0.01;

            if (powerDownFraction <= 0)
                _powerState = PowerStates.PowerOffline;

            var megawattBufferMultiplier = (attachedPowerSource.PowerBufferBonus + 1) * maxStableMegaWattPower;
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_WASTEHEAT, this.part.mass);
            resourceBuffers.UpdateVariable(ResourceManager.FNRESOURCE_MEGAJOULES, megawattBufferMultiplier * powerDownFraction);
            resourceBuffers.UpdateVariable(ResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, megawattBufferMultiplier * powerDownFraction);
            resourceBuffers.UpdateBuffers();
        }

        public override string GetInfo()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Generator_upgradeTechnologies") + "</color>");
            sb.Append("<size=10>");
            if (!string.IsNullOrEmpty(Mk2TechReq)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(Mk2TechReq)));
            if (!string.IsNullOrEmpty(Mk3TechReq)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(Mk3TechReq)));
            if (!string.IsNullOrEmpty(Mk4TechReq)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(Mk4TechReq)));
            if (!string.IsNullOrEmpty(Mk5TechReq)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(Mk5TechReq)));
            if (!string.IsNullOrEmpty(Mk6TechReq)) sb.AppendLine("- " + Localizer.Format(PluginHelper.GetTechTitleById(Mk6TechReq)));
            sb.AppendLine();
            sb.Append("</size>");

            sb.AppendLine("<color=#7fdfffff>" + Localizer.Format("#LOC_KSPIE_Generator_conversionEfficiency") + "</color>");
            sb.Append("<size=10>");
            sb.AppendLine("Mk1: " + (efficiencyMk1 * 100).ToString("F0") + "%");
            if (!string.IsNullOrEmpty(Mk2TechReq)) sb.AppendLine("Mk2: " + (efficiencyMk2 * 100).ToString("F0") + "%");
            if (!string.IsNullOrEmpty(Mk3TechReq)) sb.AppendLine("Mk3: " + (efficiencyMk3 * 100).ToString("F0") + "%");
            if (!string.IsNullOrEmpty(Mk4TechReq)) sb.AppendLine("Mk4: " + (efficiencyMk4 * 100).ToString("F0") + "%");
            if (!string.IsNullOrEmpty(Mk5TechReq)) sb.AppendLine("Mk5: " + (efficiencyMk5 * 100).ToString("F0") + "%");
            if (!string.IsNullOrEmpty(Mk6TechReq)) sb.AppendLine("Mk6: " + (efficiencyMk6 * 100).ToString("F0") + "%");
            sb.Append("</size>");

            return sb.ToString();
        }

        public override string getResourceManagerDisplayName()
        {
            var result = base.getResourceManagerDisplayName();
            if (isLimitedByMinThrotle)
                return result;

            if (attachedPowerSource != null && attachedPowerSource.Part != null)
                result += " (" + attachedPowerSource.Part.partInfo.title + ")";
            return result;
        }

        public override int getPowerPriority()
        {
            if (isLimitedByMinThrotle)
                return 1;

            if (attachedPowerSource == null)
                return base.getPowerPriority();

            return attachedPowerSource.ProviderPowerPriority;
        }
    }
}

