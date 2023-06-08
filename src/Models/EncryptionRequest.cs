public class EncryptionRequest : IEnigmaRequest
{
    public string? PlainText {get; set;}

    public bool IsValid()
    {
        if (PlainText == null)
        {
            return false;
        }

        return PlainText.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));
    }
}