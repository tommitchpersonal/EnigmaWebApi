using NumberRandomizer;
using log4net;

public class RandomSettingsGenerator : IRandomSettingsGenerator
{
    private ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

    private IRandomArrayGenerator _arrGenerator;

    public RandomSettingsGenerator(IRandomArrayGenerator randomArrayGenerator)
    {
        _arrGenerator = randomArrayGenerator;
    }

    public EnigmaSettings GenerateRandomSettings(int numberOfWheels)
    {
        _log.Info($"Generating random Enigma settings number of wheels: {numberOfWheels}");

        var wheelSettings = new List<WheelSetting>();

        for (var i = 0; i < numberOfWheels; i++)
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