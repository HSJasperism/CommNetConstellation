using UnityEngine;
using CommNetConstellation.UI;
using KSP.UI.Screens;
using KSP.Localization;

namespace CommNetConstellation
{
    /// <summary>
    /// Script to be ran in flight and tracking station
    /// </summary>
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class CommNetConstellationTracking : CommNetConstellation
    {
        public override void Start()
        {
            SetupAppLauncher(ApplicationLauncher.AppScenes.TRACKSTATION);
        }

        protected override void Launch()
        {
            controlDialog ??= new ConstellationControlDialog(Localizer.Format("#CNC_CommNetConstellation_title"));
            controlDialog.launch();
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CommNetConstellation : MonoBehaviour
    {
        protected ApplicationLauncherButton launcherButton;
        protected ConstellationControlDialog controlDialog;
        protected static Texture2D appIconTexture;

        public virtual void Start()
        {
            SetupAppLauncher(ApplicationLauncher.AppScenes.MAPVIEW);
        }

        public void OnDestroy()
        {
            if (launcherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(launcherButton);
            }
        }

        protected virtual void Launch()
        {
            controlDialog ??= new ConstellationControlDialog(Localizer.Format("#CNC_CommNetConstellation_title"));
            controlDialog.launch();
        }

        protected virtual void Dismiss()
        {
            if (controlDialog != null)
            {
                controlDialog.dismiss();
                controlDialog = null;
            }
        }

        protected virtual void SetupAppLauncher(ApplicationLauncher.AppScenes scenes)
        {
            if (appIconTexture == null)
            {
                var interfaceTexture = UIUtils.loadImage("cnclauncherbutton");
                var temp = UIUtils.getReadableCopy(interfaceTexture);
                appIconTexture = UIUtils.createSubregionTexture(temp, 1, 1, 38, 38);
                Texture2D.DestroyImmediate(temp);
            }

            this.launcherButton = ApplicationLauncher.Instance.AddModApplication(
                Launch, Dismiss, OnHover, OnHoverOut, OnEnable, OnDisable,
                scenes, appIconTexture);
        }

        /// <summary>
        /// Called when scene is entered
        /// </summary>
        protected virtual void OnEnable()
        {
        }

        /// <summary>
        /// Called when scene is exited
        /// </summary>
        protected virtual void OnDisable()
        {
        }

        /// <summary>
        /// Called when mouse cursor is over app button
        /// </summary>
        protected virtual void OnHover()
        {
        }

        /// <summary>
        /// Called when mouse cursor is out of app button
        /// </summary>
        protected virtual void OnHoverOut()
        {
        }
    }
}