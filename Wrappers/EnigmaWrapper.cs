using EnigmaLibrary;

public class EnigmaWrapper : IEnigmaWrapper
{
    public EnigmaWrapper(string owner, EnigmaSettings settings)
    {
        Owner = owner;
        EnigmaMachine = new EnigmaMachine(settings);
    }

    public string Owner{get; set;}
    public IEnigmaMachine EnigmaMachine{get; set;}
}