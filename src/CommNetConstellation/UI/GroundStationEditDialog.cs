using CommNetConstellation.CommNetLayer;
using CommNetConstellation.UI.DialogGUI;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;

namespace CommNetConstellation.UI
{
    /// <summary>
    /// Edit the ground station (Controller)
    /// </summary>
    public class GroundStationEditDialog : AbstractDialog
    {
        private CNCCommNetHome hostStation;
        private string description = Localizer.Format("#CNC_AntennaSetup_DescText1");//"Something"

        private List<short> freqListShown = new List<short>();
        private Color constellColor = Color.white;

        private DialogGUITextInput nameInput;
        private DialogGUITextInput frequencyInput;
        private Callback<string> updateCallback;
        private DialogGUIImage stationColorImage;
        private DialogGUIVerticalLayout frequencyRowLayout;

        private static readonly Texture2D colorTexture = UIUtils.loadImage("colorDisplay");

        public GroundStationEditDialog(string title, CNCCommNetHome thisStation, Callback<string> updateCallback) : base("gsEdit",
                                                                                                                title,
                                                                                                                0.5f, //x
                                                                                                                0.5f, //y
                                                                                                                290, //width
                                                                                                                300, //height
                                                                                                                [DialogOptions.HideCloseButton])
        {
            hostStation = thisStation;
            this.updateCallback = updateCallback;
            description = Localizer.Format("#CNC_GroundStationEdit_desc", thisStation.stationName);//string.Format("You are editing the ground station '{0}'.", )
            freqListShown.AddRange(thisStation.getFrequencyList());
            freqListShown.Sort();
            constellColor = thisStation.Color;

            GetInputLocks();
        }

        protected override void OnPreDismiss()
        {
            updateCallback(hostStation.ID);
            ReleaseInputLocks();
        }

        protected override List<DialogGUIBase> drawContentComponents()
        {
            List<DialogGUIBase> listComponments =
            [
                new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.UpperCenter, new DialogGUILabel(description + "\n\n"))
            ];

            DialogGUILabel nameLabel = new DialogGUILabel("<b>" + Localizer.Format("#CNC_Generic_nameLabel") + "</b>", 30, 12);//Name
            nameInput = new DialogGUITextInput(hostStation.stationName, false, CNCSettings.MaxLengthName, setNameInput, 145, 25);
            DialogGUIButton defaultButton = new DialogGUIButton(Localizer.Format("#CNC_Generic_Resetbutton"), defaultNameClick, 40, 25, false);//"Reset"
            DialogGUIHorizontalLayout nameGroup = new DialogGUIHorizontalLayout(true, false, 4, new RectOffset(), TextAnchor.MiddleCenter, nameLabel, nameInput, new DialogGUIFlexibleSpace(), defaultButton);
            listComponments.Add(nameGroup);

            DialogGUILabel freqLabel = new DialogGUILabel("<b>" + Localizer.Format("#CNC_GroundStationEdit_freqLabel") + "</b>", 85, 12);//New frequency
            frequencyInput = new DialogGUITextInput("", false, CNCSettings.MaxDigits, setFreqInput, 60, 25);
            DialogGUIButton addButton = new DialogGUIButton(Localizer.Format("#CNC_Generic_Addbutton"), addClick, 40, 25, false);//"Add"

            stationColorImage = new DialogGUIImage(new Vector2(16f, 16f), Vector2.zero, hostStation.Color, CNCCommNetHome.getGroundStationTexture(hostStation.TechLevel));
            DialogGUIHorizontalLayout imageBtnLayout = new DialogGUIHorizontalLayout(true, true, 0f, new RectOffset(5, 5, 5, 5), TextAnchor.MiddleCenter, stationColorImage);
            DialogGUIButton colorButton = new DialogGUIButton("", colorEditClick, 26, 26, false, imageBtnLayout);

            DialogGUIHorizontalLayout freqGRoup = new DialogGUIHorizontalLayout(true, false, 4, new RectOffset(), TextAnchor.MiddleCenter, freqLabel, frequencyInput, addButton, new DialogGUISpace(50), colorButton);
            listComponments.Add(freqGRoup);

            //Prepare a list container for the GUILayout rows
            DialogGUIBase[] rows = new DialogGUIBase[hostStation.getFrequencyList().Count + 1];
            rows[0] = new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true);
            for (int i = 0; i < freqListShown.Count; i++)
            {
                rows[i + 1] = createConstellationRow(freqListShown[i]);
            }

            frequencyRowLayout = new DialogGUIVerticalLayout(10, 100, 0, new RectOffset(5, 25, 5, 5), TextAnchor.UpperLeft, rows);
            listComponments.Add(new CustomDialogGUIScrollList(new Vector2(240, 100), false, true, frequencyRowLayout));

            DialogGUIButton updateButton = new DialogGUIButton(Localizer.Format("#CNC_Generic_UpdateButton"), updateAction, false);//"Update"
            DialogGUIButton cancelButton = new DialogGUIButton(Localizer.Format("#CNC_Generic_CancelButton"), delegate { dismiss(); }, false);//"Cancel"
            DialogGUIHorizontalLayout actionGroup = new DialogGUIHorizontalLayout(true, false, 4, new RectOffset(), TextAnchor.MiddleLeft, new DialogGUIFlexibleSpace(), updateButton, cancelButton, new DialogGUIFlexibleSpace());
            listComponments.Add(actionGroup);

            return listComponments;
        }

        private DialogGUIHorizontalLayout createConstellationRow(short freq)
        {
            Color color = Constellation.getColor(freq);
            string name = Constellation.getName(freq);

            DialogGUIImage colorImage = new DialogGUIImage(new Vector2(32, 32), Vector2.one, color, colorTexture);
            DialogGUILabel nameLabel = new DialogGUILabel(name, 140, 12);
            DialogGUILabel eachFreqLabel = new DialogGUILabel(string.Format("(<color={0}>{1}</color>)", UIUtils.colorToHex(color), freq), 40, 12);
            DialogGUIButton removeButton = new DialogGUIButton(Localizer.Format("#CNC_Generic_DropButton"), delegate { deleteFreqClick(freq); }, 40, 25, false);//"Drop"
            return new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.MiddleLeft, new DialogGUIBase[] { colorImage, nameLabel, eachFreqLabel, removeButton });
        }

        /// <summary>
        /// For the dialog to call upon new user input
        /// </summary>
        private string setFreqInput(string newFreqStr)
        {
            //do nothing
            return newFreqStr;
        }

        /// <summary>
        /// For the dialog to call upon new station-name input
        /// </summary>
        private string setNameInput(string newNameInput)
        {
            //do nothing
            return newNameInput;
        }

        /// <summary>
        /// Action to revert the station's name back to the stock name
        /// </summary>
        private void defaultNameClick()
        {
            nameInput.uiItem.GetComponent<TMP_InputField>().text = hostStation.displaynodeName;
        }

        /// <summary>
        /// Action to add the input frequency to the temporary list to be committed
        /// </summary>
        private void addClick()
        {
            try
            {
                try // input checks
                {
                    short newFreq = short.Parse(frequencyInput.uiItem.GetComponent<TMP_InputField>().text);

                    if (!Constellation.isFrequencyValid(newFreq))
                    {
                        throw new Exception(Localizer.Format("#CNC_CheckFrequency_Valid", short.MaxValue)); //"Frequency must be between 0 and " +
                    }

                    if (freqListShown.Contains(newFreq))
                    {
                        throw new Exception(Localizer.Format("#CNC_CheckFrequency_Contained")); //"Ground station has this frequency already"
                    }

                    if (!GameUtils.NonLinqAny(CNCCommNetScenario.Instance.constellations, newFreq))
                    {
                        throw new Exception(Localizer.Format("#CNC_CheckFrequency_Exist"));//"Please choose an existing constellation"
                    }


                    //ALL OK
                    freqListShown.Add(newFreq);
                    freqListShown.Sort();
                    CNCLog.Verbose($"Added g-station freq: {newFreq}");
                    refreshList(freqListShown);
                }
                catch (FormatException e)
                {
                    CNCLog.Error(e);
                    throw new FormatException(Localizer.Format("#CNC_CheckFrequency_Format"));//"Frequency must be numeric only"
                }
                catch (OverflowException e)
                {
                    CNCLog.Error(e);
                    throw new OverflowException(Localizer.Format("#CNC_CheckFrequency_Overflow", short.MaxValue));//string.Format("Frequency must be equal to or less than {0}", )
                }
            }
            catch (Exception e)
            {
                CNCLog.Error(e);
                var msg = new ScreenMessage("<color=red>" + e.Message + "</color>", CNCSettings.ScreenMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                ScreenMessages.PostScreenMessage(msg);
            }
        }

        /// <summary>
        /// Action to remove the particular frequency from the station's list
        /// </summary>
        private void deleteFreqClick(short frequency)
        {
            freqListShown.Remove(frequency);
            CNCLog.Verbose($"Removed g-station freq: {frequency}");
            refreshList(freqListShown);
        }

        /// <summary>
        /// Launch the color picker to change the color
        /// </summary>
        private void colorEditClick()
        {
            new ColorPickerDialog(constellColor, userChooseColor).launch();
        }

        /// <summary>
        /// Callback for the color picker to pass the new color to
        /// </summary>
        public void userChooseColor(Color newChosenColor)
        {
            constellColor = newChosenColor;
            stationColorImage.uiItem.GetComponent<RawImage>().color = constellColor;
        }

        /// <summary>
        /// Clear and recreate the list of constellations
        /// </summary>
        private void refreshList(List<short> freqs)
        {
            List<DialogGUIBase> rows = frequencyRowLayout.children;

            //deregister
            int size = rows.Count;
            for (int i = size - 1; i >= 0; i--)
            {
                DialogGUIBase thisChild = rows[i];
                if (!(thisChild is DialogGUIContentSizer)) // avoid if DialogGUIContentSizer is detected
                {
                    rows.RemoveAt(i);
                    thisChild.uiItem.gameObject.DestroyGameObjectImmediate();
                }
            }

            //create
            for (int i = 0; i < freqs.Count; i++)
            {
                rows.Add(createConstellationRow(freqs[i]));
            }

            //register
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(frequencyRowLayout.uiItem.gameObject.transform);
            for (int i = 0; i < rows.Count; i++)
            {
                if (!(rows[i] is DialogGUIContentSizer)) // avoid if DialogGUIContentSizer is detected
                    rows[i].Create(ref stack, HighLogic.UISkin); // recursively create child's children
            }
        }

        /// <summary>
        /// Action to update the ground station
        /// </summary>
        private void updateAction()
        {
            bool changesCommitted = false;
            string newName = nameInput.uiItem.GetComponent<TMP_InputField>().text.Trim();

            if (!hostStation.stationName.Equals(newName)) // different name
            {
                hostStation.stationName = newName.Equals(hostStation.displaynodeName) ? "" : newName;

                ScreenMessage msg = new ScreenMessage(Localizer.Format("#CNC_ScreenMsg_GroundStationNameUpdate", hostStation.stationName), CNCSettings.ScreenMessageDuration, ScreenMessageStyle.UPPER_CENTER); //string.Format("Ground station is renamed to '{0}'", )
                ScreenMessages.PostScreenMessage(msg);
                changesCommitted = true;
            }

            if (hostStation.Color != constellColor)
            {
                hostStation.Color = constellColor;
                ScreenMessage msg = new ScreenMessage(Localizer.Format("#CNC_ScreenMsg_GroundStationColorUpdate", UIUtils.colorToHex(hostStation.Color)), CNCSettings.ScreenMessageDuration, ScreenMessageStyle.UPPER_CENTER); //string.Format("Ground station is '{0}' now", )
                ScreenMessages.PostScreenMessage(msg);
                changesCommitted = true;
            }

            int commonFreq = GameUtils.NonLinqIntersect(freqListShown.ToArray(), hostStation.getFrequencyArray()).Length;
            if (!(commonFreq == hostStation.getFrequencyList().Count && commonFreq == freqListShown.Count))
            {
                hostStation.replaceFrequencies(freqListShown);
                ScreenMessage msg = new ScreenMessage(Localizer.Format("#CNC_ScreenMsg_GroundStationFreqUpdate"), CNCSettings.ScreenMessageDuration, ScreenMessageStyle.UPPER_CENTER); //"Ground station's frequency list is updated"
                ScreenMessages.PostScreenMessage(msg);
                changesCommitted = true;
            }

            var strFreqs = new StringBuilder();
            foreach (short freq in freqListShown) strFreqs.Append(freq).Append(" ");

            CNCLog.Verbose($"Updated g-station: {newName} - {strFreqs}");

            if (changesCommitted) dismiss();
        }
    }
}