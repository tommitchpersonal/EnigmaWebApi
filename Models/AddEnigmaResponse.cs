public class AddEnigmaResponse
{
    public AddEnigmaResponse(string id, EnigmaSettings settings)
    {
        Id = id;
        EnigmaSettings = settings;
    }

    public string Id {get; set;}
    public EnigmaSettings EnigmaSettings {get; set;}
}