using PubnubApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Visyde;

public class SettingsWindow : MonoBehaviour
{
    [Header("UI References")]
    public Dropdown localeDropdown;
    public Button saveButton;
    public Button closeButton;
    public Toggle frameRateSetting;

    //internals
    private int originalLanguageLocation;
    private bool fpsToggled;
    

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadDropdownOptions());
        fpsToggled = frameRateSetting.isOn = Connector.IsFPSSettingEnabled;
        saveButton.onClick.AddListener(async () => await SaveOptions());
    }

    private IEnumerator LoadDropdownOptions()
    {
        // Wait for the localization system to initialize, loading Locales, preloading etc.
        yield return LocalizationSettings.InitializationOperation;

        // Generate list of available Locales
        var options = new List<Dropdown.OptionData>();
        int selected = 0;
        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; ++i)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[i];
            if (LocalizationSettings.SelectedLocale.Identifier.Code.Equals(locale.Identifier.Code)) 
            {
                selected = i;
                originalLanguageLocation = i;
            }
            options.Add(new Dropdown.OptionData(locale.name));
        }
        localeDropdown.onValueChanged.AddListener(LocaleSelected);
        localeDropdown.options = options;
        localeDropdown.value = selected;
        localeDropdown.RefreshShownValue(); // Refresh to ensure proper display
    }

    private void LocaleSelected(int index)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
    }

    /// <summary>
    /// Save any changes, if there are any, and close the settings window.
    /// </summary>
    public async Task<bool> SaveOptions()
    {
        bool changesMade = false;

        //Frame Rate has Changed
        if (fpsToggled != frameRateSetting.isOn)
        {
            changesMade = true;
            fpsToggled = frameRateSetting.isOn;
            Application.targetFrameRate = frameRateSetting.isOn ? 60 : 30;
        }

        //Language Has Changed
        if (LocalizationSettings.AvailableLocales.Locales[originalLanguageLocation] != LocalizationSettings.SelectedLocale)
        {
            changesMade = true;
            originalLanguageLocation = LocalizationSettings.AvailableLocales.Locales.FindIndex(locale => locale == LocalizationSettings.SelectedLocale);
        }

        //Some kind of change was made, update the player metadata.
        if(changesMade)
        {           
            //If successful, update the Connector object.
            if (PNManager.pubnubInstance.CachedPlayers.ContainsKey(Connector.instance.GetPubNubObject().GetCurrentUserId())
                && PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Custom != null)
            {
                Dictionary<string, object> customData = PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Custom;
                if(customData.ContainsKey("language"))
                {
                    customData["language"] = LocalizationSettings.AvailableLocales.Locales[originalLanguageLocation].Identifier.Code;
                }

                else
                {
                    customData.Add("language", LocalizationSettings.AvailableLocales.Locales[originalLanguageLocation].Identifier.Code);
                }
                
                if(customData.ContainsKey("60fps"))
                {
                    customData["60fps"] = fpsToggled;
                }

                else
                {
                    customData.Add("60fps", fpsToggled);
                }
                
                //Acceptable to not need to await for this call to finish.
                await PNManager.pubnubInstance.UpdateUserMetadata(Connector.instance.GetPubNubObject().GetCurrentUserId(), PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Name, customData);
            }
            
        }
        //Close the Chat Window.
        CloseWindow();

        return true;
    }

    /// <summary>
    /// Close the settings window (and discard any changes)
    /// </summary>
    public void CloseWindow()
    {
        //Reset internals
        localeDropdown.value = originalLanguageLocation;
        frameRateSetting.isOn = fpsToggled;
        gameObject.SetActive(false);
    }
}
