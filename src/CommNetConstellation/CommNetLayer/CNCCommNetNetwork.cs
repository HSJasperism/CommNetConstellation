using CommNet;
using UnityEngine;

namespace CommNetConstellation.CommNetLayer
{
    /// <summary>
    /// Extend the functionality of the KSP's CommNetNetwork (co-primary model in the Model–view–controller sense; CommNet is the other co-primary one)
    /// </summary>
    public class CNCCommNetNetwork : CommNetNetwork
    {
        //Part of inactive network optimisation in CNCCommNetNetwork.Update()
        //private float nextUpdateTime = 0.0f;
        //private const float networkInterval = 0.1f; // in seconds

        private bool isPlanetarium = false;

        protected override void Awake()
        {
            CNCLog.Verbose("CNC Network booting");

            Instance = this;
            CommNet = new CNCCommNetwork();
            GameEvents.CommNet.OnNetworkInitialized.Fire();

            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                GameEvents.onPlanetariumTargetChanged.Add(OnMapFocusChange);
                isPlanetarium = true;
            }
            GameEvents.OnGameSettingsApplied.Add(ResetNetwork);
        }

        protected override void OnDestroy()
        {
            CNCLog.Verbose("CNC Network shutting down");

            if (isPlanetarium) GameEvents.onPlanetariumTargetChanged.Remove(OnMapFocusChange);
            GameEvents.OnGameSettingsApplied.Remove(ResetNetwork);

            base.OnDestroy();
        }

        protected new void ResetNetwork()
        {
            CNCLog.Verbose("CNC Network rebooted");

            CommNet = new CNCCommNetwork();
            GameEvents.CommNet.OnNetworkInitialized.Fire();
        }

        protected override void Update()
        {
            //Comment: Not recommended to run along with other active optimisation of evaluating
            //subset of connections in CNCCommNetwork.UpdateNetwork()
            //Effect of running both optimisations is unacceptable low rate of connection check per second
            //if (Time.time >= nextUpdateTime)
            //{
            base.Update();
                //nextUpdateTime += networkInterval;
            //}
        }
    }
}