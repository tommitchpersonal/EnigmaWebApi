using NumberRandomizer;

public class RandomSettingsGenerator : IRandomSettingsGenerator
{
    private IRandomArrayGenerator _arrGenerator;
    public RandomSettingsGenerator(IRandomArrayGenerator randomArrayGenerator)
    {
        _arrGenerator = randomArrayGenerator;
    }

    public EnigmaSettings GenerateRandomSettings()
    {
        var wheelSettings = new List<WheelSetting>();

        for (var i = 0; i < 3; i++)
        {
            var mappings = _arrGenerator.CreateRandomArray(0, 25, 1);
            wheelSettings.Add(new WheelSetting(){Mappings = mappings});
        }

        return new EnigmaSettings
        {
            WheelSettings = wheelSettings.ToArray()
        };
    }
}