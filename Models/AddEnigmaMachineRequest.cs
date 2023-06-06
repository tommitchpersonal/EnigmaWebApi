public class AddEnigmaMachineRequest
{
    public bool UseRandomWheels {get; set;} = false;
    public int NumberOfWheels {get; set;}
    public EnigmaSettings? EnigmaMachineSettings {get; set;}
    public bool IsValid()
    {
        if (!UseRandomWheels)
        {
            return EnigmaMachineSettings != null && EnigmaMachineSettings.IsValid();
        }
        else
        {
            return EnigmaMachineSettings == null && NumberOfWheels > 0;
        }
    }
}