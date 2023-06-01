public class UpdateSettingsRequest : IEnigmaRequest
{
    public EnigmaSettings? NewSettings {get; set;}

    public bool IsValid()
    {
        if (NewSettings?.WheelSettings == null)
        {
            return false;
        }

        foreach (var wheelSetting in NewSettings.WheelSettings)
        {
            if (wheelSetting?.Mappings == null)
            {
                return false;
            }

            if (wheelSetting.Mappings.Count() != 26)
            {
                return false;
            }

            if (wheelSetting.Mappings.Any(s => s < 0 || s > 25))
            {
                return false;
            }

            if (wheelSetting.Mappings.Distinct().Count() != wheelSetting.Mappings.Count())
            {
                return false;
            }
        }

        return true;
    }
}