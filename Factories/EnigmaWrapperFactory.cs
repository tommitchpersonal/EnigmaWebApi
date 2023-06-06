public class EnigmaWrapperFactory : IEnigmaWrapperFactory
{
    public IEnigmaWrapper CreateEnigmaWrapper(string owner, EnigmaSettings settings)
    {
        return new EnigmaWrapper(owner, settings);
    }
}