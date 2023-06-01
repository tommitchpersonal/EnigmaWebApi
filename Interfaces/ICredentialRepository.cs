public interface ICredentialRepository
{
    public Task<bool> Authenticate(string username, string password);
    public string GetUsername();
}