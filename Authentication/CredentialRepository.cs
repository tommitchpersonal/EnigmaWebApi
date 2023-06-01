using Microsoft.Extensions.Options;

public class CredentialRepository : ICredentialRepository
{
    private Credentials _allowedUser = new();

    public CredentialRepository()
    {
        
    }

    public CredentialRepository(IOptions<Credentials> credentials)
    {
        _allowedUser = new Credentials(credentials.Value.Username, credentials.Value.Password);
    }
    
    public async Task<bool> Authenticate(string username, string password)
    {
        var result = await Task.FromResult(_allowedUser.Username == username && _allowedUser.Password == password);
        return result;
    }

    public string GetUsername()
    {
        return _allowedUser.Username;
    }
}