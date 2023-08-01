using Newtonsoft.Json;
using PubnubApi;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Visyde;

public class LocaleDropdown : MonoBehaviour
{
    public Dropdown dropdown;

    IEnumerator Start()
    {
        // Wait for the localization system to initialize, loading Locales, preloading etc.
        yield return LocalizationSettings.InitializationOperation;

        // Generate list of available Locales
        var options = new List<Dropdown.OptionData>();
        int selected = 0;
        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; ++i)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[i];
            if (LocalizationSettings.SelectedLocale == locale)
                selected = i;
            options.Add(new Dropdown.OptionData(locale.name));
        }
        dropdown.options = options;

        dropdown.value = selected;
        dropdown.onValueChanged.AddListener(LocaleSelected);
    }

    void LocaleSelected(int index)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
        Connector.UserLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;
        /*
        // TODO: Update the language selection for the player by storing in the metadata.
        Dictionary<string, object> customData = new Dictionary<string, object>();
        customData["language"] = Connector.UserLanguage;

        // Set Metadata for UUID set in the pubnub instance
        PNResult<PNSetUuidMetadataResult> setUuidMetadataResponse = await Connector.instance.GetPubNubObject().SetUuidMetadata()
            .Uuid(Connector.instance.GetPubNubObject().GetCurrentUserId())
            .Name(Connector.instance.GetPubNubObject().GetCurrentUserId())
            .Custom(customData)
            .IncludeCustom(true)
            .ExecuteAsync();
        PNSetUuidMetadataResult setUuidMetadataResult = setUuidMetadataResponse.Result;
        PNStatus setUUIDResponseStatus = setUuidMetadataResponse.Status;
        */
    }
}