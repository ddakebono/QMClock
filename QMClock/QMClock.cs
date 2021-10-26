using System;
using System.Collections;
using System.Linq;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRC.UI.Core;
using VRC.UI.Elements;
using Log = MelonLoader.MelonLogger;
using Object = UnityEngine.Object;

namespace QMClock
{
    public static class BuildInfo
    {
        public const string Name = "QMClock"; // Name of the Mod.  (MUST BE SET)
        public const string Author = "DDAkebono#0001"; // Author of the Mod.  (Set as null if none)
        public const string Company = "BTK-Development"; // Company that made the Mod.  (Set as null if none)
        public const string Version = "1.0.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }
    
    public class QMClock : MelonMod
    {
        
        //Prefs
        private readonly string _prefsCategory = "QMClock";
        private readonly string _prefs12HourClock = "12HourClock";
        private readonly string _prefsShowsSeconds = "ShowSeconds";

        private const float SizePerElement = 140f;
        
        private TMP_Text _clock;
        
        //Reference GameObjects
        private GameObject _pingText;
        private GameObject _panelRoot;
        
        private int _scenesLoaded = 0;
        private string _timeFormat;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (_scenesLoaded <= 2)
            {
                _scenesLoaded++;
                if (_scenesLoaded == 2)
                    UiManagerInit();
            }
        }

        public override void OnPreferencesSaved()
        {
            _timeFormat = GetTimeFormat();
        }

        private void UiManagerInit()
        {
            Log.Msg("Starting up QMClock!");
            
            if (MelonHandler.Mods.Any(x => x.Info.Name.Equals("BTKCompanionLoader", StringComparison.OrdinalIgnoreCase)))
            {
                Log.Msg("Hold on a sec! Looks like you've got BTKCompanion installed, this mod is built in and not needed!");
                Log.Error("QMClock has not started up! (BTKCompanion Running)");
                return;
            }

            MelonPreferences.CreateCategory(_prefsCategory, "Quick Menu Clock");
            MelonPreferences.CreateEntry<bool>(_prefsCategory, _prefs12HourClock, false, "12 Hour Clock Mode");
            MelonPreferences.CreateEntry<bool>(_prefsCategory, _prefsShowsSeconds, false, "Show Seconds");

            _timeFormat = GetTimeFormat();
            
            MelonCoroutines.Start(WaitForQMInit());
        }

        private string GetTimeFormat()
        {
            string format = "HH:mm";

            if (MelonPreferences.GetEntry<bool>(_prefsCategory, _prefs12HourClock).Value)
                format = "h:mm tt";

            if (MelonPreferences.GetEntry<bool>(_prefsCategory, _prefsShowsSeconds).Value)
                format = format.Replace("mm", "mm:ss");

            return format;
        }

        private TMP_Text CreateQMTextElement(string elementName)
        {
            if (_panelRoot == null && _pingText == null) return null;
            
            GameObject newElement = Object.Instantiate(_pingText, _panelRoot.transform, false);

            newElement.name = elementName;

            TMP_Text newElementText = newElement.GetComponent<TMP_Text>();
            newElementText.richText = true;

            ResizeRootPanel();

            return newElementText;
        }
        
        private bool SetupDebugInfoPanelAndReferences()
        {
            DebugInfoPanel debugPanelComponent = Object.FindObjectOfType<DebugInfoPanel>();

            if (debugPanelComponent == null)
            {
                Log.Error("Unable to find the DebugInfoPanel!");
                return false;
            }

            _panelRoot = debugPanelComponent.field_Public_GameObject_0;
            _pingText = debugPanelComponent.field_Public_TextBinding_0.gameObject;
            GameObject debugPanelBG = _panelRoot.transform.Find("Background").gameObject;

            if (debugPanelBG == null || _pingText == null)
            {
                Log.Error("Unable to find DebugInfoPanel GameObjects");
                return false;
            }

            //Copy background image to panel
            Image background = debugPanelBG.GetComponent<Image>();
            Image panelBG = _panelRoot.AddComponent<Image>();
            panelBG.sprite = background.sprite;
            panelBG.color = new Color(0, 0, 0, .95f);
            Object.Destroy(debugPanelBG);

            //Setup HorizontalLayoutGroup
            HorizontalLayoutGroup horizLayout = _panelRoot.AddComponent<HorizontalLayoutGroup>();
            horizLayout.padding.left = 20;
            horizLayout.padding.right = 20;
            horizLayout.spacing = 1.5f;

            return true;
        }
        
        private void CreateClockElement()
        {
            _clock = CreateQMTextElement("BTKClockElement");
            if (_clock == null) return;
            _clock.text = "00:00";
        }

        private void ResizeRootPanel()
        {
            if (_panelRoot == null) return;
            
            RectTransform debugPanelRect = _panelRoot.GetComponent<RectTransform>();
            Vector2 adjust = debugPanelRect.sizeDelta;
            adjust.x = SizePerElement*_panelRoot.transform.childCount;
            debugPanelRect.sizeDelta = adjust;
        }
        
        private DateTime fixedTime()
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(DateTime.UtcNow);
        }

        private IEnumerator TimeUpdate()
        {
            while (true)
            {
                if (_clock != null)
                    _clock.text = fixedTime().ToString(_timeFormat);

                yield return new WaitForSeconds(.5f);
            }
        }
        
        private IEnumerator WaitForQMInit()
        {
            while (UIManager.field_Private_Static_UIManager_0 == null) yield return null;
            while (Object.FindObjectOfType<QuickMenu>() == null) yield return null;
            
            SetupPostQMInit();
        }

        private void SetupPostQMInit()
        {
            if (!SetupDebugInfoPanelAndReferences()) return;
            
            CreateClockElement();
            
            MelonCoroutines.Start(TimeUpdate());
        }
    }
}