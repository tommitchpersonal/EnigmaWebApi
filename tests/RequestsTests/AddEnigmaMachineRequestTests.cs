namespace EnigmaWebApiTests;

public class AddEnigmaMachineRequestTests
{
    [Test]
    public void NotValidIfUsingRandomWheelsButNumberOfWheelsIsLessThan1()
    {
        var sut = new AddEnigmaMachineRequest()
        {
            UseRandomWheels = true,
            NumberOfWheels = 0
        };

        var result = sut.IsValid();

        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void NotValidIfUsingRandomWheelsButSettingsAreDefined()
    {
        var sut = new AddEnigmaMachineRequest()
        {
            UseRandomWheels = true,
            NumberOfWheels = 1,
            EnigmaMachineSettings = new EnigmaSettings()
        };

        var result = sut.IsValid();
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void ValidIfUsingRandomWheelsAndNumberOfWheelsGreaterThan0()
    {
        var sut = new AddEnigmaMachineRequest()
        {
            UseRandomWheels = true,
            NumberOfWheels = 1
        };

        var result = sut.IsValid();
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void NotValidIfUsingSetWheelsAndEnigmaSettingsAreUndefined()
    {
        var sut = new AddEnigmaMachineRequest()
        {
            UseRandomWheels = false
        };

        var result = sut.IsValid();
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void NotValidIfUsingSetWheelsAndEnigmaSettingsAreInvalid()
    {
        var sut = new AddEnigmaMachineRequest()
        {
            UseRandomWheels = false,
            EnigmaMachineSettings = new EnigmaSettings()
            {
                WheelSettings = new WheelSetting[]
                {
                    new WheelSetting()
                    {
                        Mappings = new int[25]
                    }
                }
            }
        };

        var result = sut.IsValid();
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void ValidIfUsingSetWheelsAndEnigmaSettingsAreValid()
    {
        var sut = new AddEnigmaMachineRequest()
        {
            UseRandomWheels = false,
            EnigmaMachineSettings = new EnigmaSettings()
            {
                WheelSettings = new WheelSetting[]
                {
                    new WheelSetting()
                    {
                        Mappings = GenerateValidMapping()
                    }
                }
            }
        };

        var result = sut.IsValid();
        Assert.That(result, Is.EqualTo(true));
    }

    private int[] GenerateValidMapping()
    {
        var size = 26;
        var mapping = new int[size];

        for (var i = 0; i < 26; i++)
        {
            mapping[i] = i;
        }

        return mapping;
    }
}
