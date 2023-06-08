public class Credentials
{
    public const string Key = "Credentials";

    public Credentials()
    {
        
    }

    public Credentials(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public string Username {get; set;} = string.Empty;
    public string Password {get; set;} = string.Empty;
}