namespace EnigmaWebApiTests;

public class EncryptionRequestTests
{
    [Test]
    public void RequestNotValidIfPlainTextNull()
    {
        CommonRun(null, false);
    }

    [Test]
    public void RequestNotValidIfPlainTextContainsNumbers()
    {
        CommonRun("hi5", false);
    }

    [Test]
    public void RequestNotValidIfPlainTextContainsSpecialCharacter()
    {
        CommonRun("hi!", false);
    }

    [Test]
    public void RequestValidIfOnlyLettersAndSpaces()
    {
        CommonRun("Hello World", true);
    }

    private void CommonRun(string? plainText, bool expectedResult)
    {
        var testRequest = new EncryptionRequest()
        {
            PlainText = plainText
        };

        var isValid = testRequest.IsValid();


        Assert.That(isValid, Is.EqualTo(expectedResult));
    }
}