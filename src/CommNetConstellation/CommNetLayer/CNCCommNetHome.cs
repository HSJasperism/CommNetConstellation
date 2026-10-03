using System;
using System.Collections.Generic;
using CommNet;
using CommNetConstellation.UI;
using KSP.Localization;
using UnityEngine;

namespace CommNetConstellation.CommNetLayer
{
    /// <summary>
    /// Customise the home nodes
    /// </summary>
    public class CNCCommNetHome : CommNetHome, IComparable<CNCCommNetHome>
    {
        public static readonly Texture2D L0MarkTexture = UIUtils.loadImage("GroundStationL0Mark");
        public static readonly Texture2D L1MarkTexture = UIUtils.loadImage("GroundStationL1Mark");
        public static readonly Texture2D L2MarkTexture = UIUtils.loadImage("GroundStationL2Mark");
        public static readonly Texture2D L3MarkTexture = UIUtils.loadImage("GroundStationL3Mark");
        private static GUIStyle groundStationHeadline;

        private string stationInfoString = "";
        private Texture2D stationTexture;

        //to be saved to persistent.sfs
        [Persistent] public string ID;
        [Persistent] public Color Color = Color.red;
        [Persistent] protected string OptionalName = "";
        [Persistent] public short TechLevel;
        [Persistent] public bool OverrideLatLongAlt;
        [Persistent] public double CustomLatitude;
        [Persistent] public double CustomLongitude;
        [Persistent] public double CustomAltitude;
        [Persistent] public string CustomCelestialBody = "";
        [Persistent(collectionIndex = "Frequency")] protected List<short> Frequencies = new List<short>();

        //for low-gc operations
        protected short[] sorted_frequency_array;

        public double altitude
        {
            get => alt;
            set => alt = value;
        }
        public double latitude
        {
            get => lat;
            set => lat = value;
        }
        public double longitude
        {
            get => lon;
            set => lon = value;
        }
        public CommNode commNode => comm;

        public string stationName
        {
            get => (OptionalName.Length == 0)? displaynodeName : OptionalName;
            set { OptionalName = value; comm.name = comm.displayName = value; }
        }

        /// <summary>
        /// Empty constructor for ConfigNode.LoadObjectFromConfig()
        /// </summary>
        public CNCCommNetHome() { }

        public void copyOf(CommNetHome stockHome)
        {
            CNCLog.Verbose("Stock CommNet Home '{0}' added", stockHome.nodeName);

            ID = stockHome.nodeName;
            nodeName = stockHome.nodeName;
            displaynodeName = Localizer.Format(stockHome.displaynodeName);
            nodeTransform = stockHome.nodeTransform;
            isKSC = stockHome.isKSC;
            body = stockHome.GetComponentInParent<CelestialBody>();

            //comm, lat, alt, lon are initialised by CreateNode() later
        }

        /// <summary>
        /// Apply the changes from persistent.sfs
        /// </summary>
        public void applySavedChanges(CNCCommNetHome stationSnapshot)
        {
            Color = stationSnapshot.Color;
            Frequencies = stationSnapshot.Frequencies;
            OptionalName = stationSnapshot.OptionalName;
            TechLevel = stationSnapshot.TechLevel;
            OverrideLatLongAlt = stationSnapshot.OverrideLatLongAlt;
            CustomLatitude = stationSnapshot.CustomLatitude;
            CustomLongitude = stationSnapshot.CustomLongitude;
            CustomAltitude = stationSnapshot.CustomAltitude;
            CustomCelestialBody = stationSnapshot.CustomCelestialBody;
        }

        /// <summary>
        /// Replace one specific frequency with new frequency
        /// </summary>
        public void replaceFrequency(short oldFrequency, short newFrequency)
        {
            Frequencies.Remove(oldFrequency);
            Frequencies.Add(newFrequency);
            Frequencies.Sort();
            regenerateFrequencyArray(Frequencies);
        }

        /// <summary>
        /// Drop the specific frequency from the list
        /// </summary>
        public void deleteFrequency(short frequency)
        {
            Frequencies.Remove(frequency);
            regenerateFrequencyArray(Frequencies);
        }

        /// <summary>
        /// Get the *sorted* array of frequencies only
        /// </summary>
        public short[] getFrequencyArray()
        {
            if(sorted_frequency_array == null)
            {
                regenerateFrequencyArray(Frequencies);
            }
            return sorted_frequency_array;
        }

        /// <summary>
        /// Get the *sorted* list of frequencies only
        /// </summary>
        public List<short> getFrequencyList()
        {
            return Frequencies;
        }

        /// <summary>
        /// Remove all frequencies
        /// </summary>
        public void deleteFrequencies()
        {
            Frequencies.Clear();
            regenerateFrequencyArray(Frequencies);
        }

        /// <summary>
        /// Replace all frequencies
        /// </summary>
        public void replaceFrequencies(List<short> newFreqs)
        {
            Frequencies = newFreqs;
            regenerateFrequencyArray(Frequencies);
        }

        /// <summary>
        /// Increment Tech Level Ground Station to max 3
        /// </summary>
        public void incrementTechLevel()
        {
            if (TechLevel < 3 && !isKSC)
            {
                TechLevel++;
                refresh();
            }
        }

        /// <summary>
        /// Decrement Tech Level Ground Station to min 0
        /// </summary>
        public void decrementTechLevel()
        {
            if (TechLevel > 0 && !isKSC)
            {
                TechLevel--;
                refresh();
            }
        }

        /// <summary>
        /// Set Tech Level Ground Station
        /// </summary>
        public void setTechLevel(short level)
        {
            if (level is >= 0 and <= 3 && !isKSC)
            {
                TechLevel = level;
                refresh();
            }
        }

        /// <summary>
        /// Update latitude and longitude of celestial body
        /// </summary>
        public void setLatLongCoords(double setLat, double setLon, bool persistent = true)
        {
            OverrideLatLongAlt = persistent;
            latitude = CustomLatitude = setLat;
            longitude = CustomLongitude = setLon;
            refresh();
        }

        /// <summary>
        /// Update altitude on celestial body
        /// </summary>
        public void setAltitude(double setAlt, bool persistent = false)
        {
            OverrideLatLongAlt = persistent;
            altitude = CustomAltitude = setAlt;
            refresh();
        }

        /// <summary>
        /// Change how Start() runs
        /// </summary>
        protected override void Start()
        {
            groundStationHeadline ??= new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.yellow },
                alignment = TextAnchor.MiddleCenter
            };

            body = (CustomCelestialBody.Length > 0) ? FlightGlobals.Bodies.Find(x => x.name.Equals(CustomCelestialBody)) : GetComponentInParent<CelestialBody>();

            //one root cause is 3rd-party mod Making Less History, which disables 2 ground stations in Making History expansion
            if (body == null)
            {
                //self-destruct
                CNCLog.Error("CommNet Home '{0}' self-destructed due to missing info", ID);
                CNCCommNetScenario.Instance.groundStations.Remove(this);
                OnDestroy();
                Destroy(this);
                return;
            }

            // if (nodeTransform == null)
            // {
            //     nodeTransform = nodeTransform;
            // }

            if (CommNetNetwork.Initialized)
            {
                OnNetworkInitialized();
            }

            GameEvents.CommNet.OnNetworkInitialized.Add(OnNetworkInitialized);

            if (OverrideLatLongAlt)
            {
                latitude = CustomLatitude;
                longitude = CustomLongitude;
                altitude = CustomAltitude;
            }

            refresh();
        }

        protected override void OnDestroy()
        {
            GameEvents.CommNet.OnNetworkInitialized.Remove(OnNetworkInitialized);
            base.OnDestroy();
        }

        /// <summary>
        /// Draw graphic components on screen like RemoteTech's ground-station marks
        /// </summary>
        public void OnGUI()
        {
            if (HighLogic.CurrentGame == null)
                return;

            if (!(HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION))
                return;

            if ((!HighLogic.CurrentGame.Parameters.CustomParams<CommNetParams>().enableGroundStations && !isKSC) || !MapView.MapIsEnabled || MapView.MapCamera == null)
                return;

            if (CNCCommNetScenario.Instance == null || CNCCommNetScenario.Instance.hideGroundStations)
                return;

            Vector3d worldPos = ScaledSpace.LocalToScaledSpace(comm.precisePosition);

            if (MapView.MapCamera.transform.InverseTransformPoint(worldPos).z < 0f)
                return;

            if (isOccluded(comm.precisePosition, body))
                return;

            if (!isOccluded(comm.precisePosition, body) && IsCamDistanceToWide(comm.precisePosition))
                return;

            //maths calculations
            var screenPosition = PlanetariumCamera.Camera.WorldToScreenPoint(worldPos);
            var centerPosition = new Vector3(screenPosition.x - 8, (Screen.height - screenPosition.y) - 8);
            var groundStationRect = new Rect(centerPosition.x, centerPosition.y, 16, 16);

            //draw the dot
            Color previousColor = GUI.color;
            GUI.color = Color;
            GUI.DrawTexture(groundStationRect, stationTexture, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;

            //draw the headline above and below the dot
            if (UIUtils.ContainsMouse(groundStationRect))
            {
                Rect headlineRect = groundStationRect;

                //Name
                Vector2 nameDim = groundStationHeadline.CalcSize(new GUIContent(stationName));
                headlineRect.x -= nameDim.x/2 - 5;
                headlineRect.y -= nameDim.y + 5;
                headlineRect.width = nameDim.x;
                headlineRect.height = nameDim.y;
                GUI.Label(headlineRect, stationName, groundStationHeadline);

                //build station information
                if (TechLevel <= 0)
                {
                    stationInfoString = Localizer.Format("#CNC_CNCCommNetHome_nostation");//"Build a ground station";
                }
                else
                {
                    //frequency list
                    string freqStr = Localizer.Format("#CNC_ConstellationControl_getFreqString_nothing");//"No frequency assigned"

                    if (Frequencies.Count > 0)
                    {
                        freqStr = Localizer.Format("#CNC_CNCCommNetHome_freqlist");//"Broadcasting in"
                        for (int i = 0; i < Frequencies.Count; i++)
                            freqStr += "\n" + Localizer.Format("#CNC_CNCCommNetHome_frequency") + " " + Frequencies[i];//"~ frequency"
                    }

                    stationInfoString = string.Format("DSN Power: {1}\nTech Level: {0}\n{2}",
                                            TechLevel,
                                            UIUtils.RoundToNearestMetricFactor(comm.antennaRelay.power, 2),
                                            freqStr);
                }

                headlineRect = groundStationRect;
                Vector2 freqDim = groundStationHeadline.CalcSize(new GUIContent(stationInfoString));
                headlineRect.x -= freqDim.x / 2 - 5;
                headlineRect.y += groundStationRect.height + 5;
                headlineRect.width = freqDim.x;
                headlineRect.height = freqDim.y;
                GUI.Label(headlineRect, stationInfoString, groundStationHeadline);
            }
        }

        /// <summary>
        /// Check whether this vector3 location is behind the body
        /// Original code by regex from https://github.com/NathanKell/RealSolarSystem/blob/master/Source/KSCSwitcher.cs
        /// </summary>
        private bool isOccluded(Vector3d position, CelestialBody cBody)
        {
            var camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position);

            if (Vector3d.Angle(camPos - position, cBody.position - position) > 90)
                return false;
            return true;
        }

        /// <summary>
        /// Calculate the distance between the camera position and the ground station, and
        /// return true if the distance is >= DistanceToHideGroundStations from the settings file.
        /// </summary>
        private bool IsCamDistanceToWide(Vector3d loc)
        {
            Vector3d camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position);
            float distance = Vector3.Distance(camPos, loc);

            if (distance >= CNCSettings.Instance.DistanceToHideGroundStations)
                return true;
            return false;
        }

        /// <summary>
        /// Allow to be sorted easily
        /// </summary>
        public int CompareTo(CNCCommNetHome other)
        {
            return string.Compare(stationName, other.stationName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Regenerate frequency array used for low-gc operations
        /// </summary>
        protected void regenerateFrequencyArray(List<short> list)
        {
            if (list.Count == 0)
            {
                sorted_frequency_array = [];
            }

            sorted_frequency_array = new short[list.Count];
            for (int i=0; i< list.Count; i++)
            {
                sorted_frequency_array[i] = list[i];
            }

            GameUtils.Quicksort(sorted_frequency_array, 0, sorted_frequency_array.Length - 1);
        }

        /// <summary>
        /// Update relevant details based on Tech Level
        /// </summary>
        protected void refresh()
        {
            if (comm == null)
            {
                if (!HighLogic.CurrentGame.Parameters.CustomParams<CommNetParams>().enableGroundStations)
                {
                    CNCLog.Verbose("Ground station '{0}': CommNet option of enabling ground stations is disabled", ID);
                }
                else
                {
                    CNCLog.Verbose("Ground station '{0}': Null CommNode, likely due to a third-party CommNet mod", ID);
                }
                return;
            }

            if(comm != null && (comm.displayName.Length < 1 || comm.name.Length < 1))
            {
                comm.name = comm.displayName = stationName; //required for visual sequence of connected nodes
            }

            // Obtain Tech Level of Tracking Station in KCS
            if (isKSC)
            {
                TechLevel = (short)((2 * ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation)) + 1);
            }

            // Update power of ground station
            comm.antennaRelay.Update(GetDSNRange(TechLevel), GameVariables.Instance.GetDSNRangeCurve(), false);

            // Generate ground station information
            stationInfoString = (TechLevel == 0) ? "Build a ground station" :
                                                    string.Format("DSN Power: {1}\nBeamwidth: {2:0.00}°\nTech Level: {0}",
                                                    TechLevel,
                                                    UIUtils.RoundToNearestMetricFactor(comm.antennaRelay.power, 2),
                                                    90.0);

            // Generate visual ground station mark
            stationTexture = getGroundStationTexture(TechLevel);

            // Update position on celestial body
            comm.precisePosition = body.GetWorldSurfacePosition(latitude, longitude, altitude);
        }

        /// <summary>
        /// Get ground station texture based on tech level
        /// </summary>
        public static Texture2D getGroundStationTexture(int techLevel)
        {
            switch (techLevel)
            {
                case 0:
                    return L0MarkTexture;
                case 1:
                    return L1MarkTexture;
                case 2:
                    return L2MarkTexture;
                case 3:
                    return L3MarkTexture;
                default:
                    return L3MarkTexture;
            }
        }

        /// <summary>
        /// Custom DSN ranges instead of stock GameVariables.Instance.GetDSNRange
        /// </summary>
        /// Comment: Subclassing GameVariables.Instance.GetDSNRange to just change the ranges is too excessive at this point.
        public double GetDSNRange(short level)
        {
            double power;
            if (isKSC)
            {
                power = CNCSettings.Instance.KSCStationPowers[level - 1];
            }
            else
            {
                if (level == 0)
                {
                    power = 0.0;
                }
                else
                {
                    power = CNCSettings.Instance.GroundStationUpgradeablePowers[level - 1];
                }
            }

            return power * HighLogic.CurrentGame.Parameters.CustomParams<CommNetParams>().DSNModifier;
        }

        /// <summary>
        /// Overrode to correct the error of assigning position to comm's position (no setter)
        /// </summary>
        protected override void Update()
        {
            if (HighLogic.CurrentGame == null)
                return;

            if (!(HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION))
                return;

            if (comm != null && body != null)
            {
                comm.precisePosition = body.GetWorldSurfacePosition(lat, lon, alt);
                //this.comm.position has no setter
                comm.transform.position = comm.precisePosition;

                if (nodeTransform != null)
                {
                    nodeTransform.position = comm.precisePosition;
                }

                refresh();
            }
        }

        /// <summary>
        /// Overrode to remove unnecessary position calculation that is done in Update()
        /// </summary>
        protected override void OnNetworkPreUpdate()
        {
            //do nothing
        }
    }
}