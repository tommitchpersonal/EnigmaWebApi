namespace EnigmaWebApiTests;

public class RandomSettingsGeneratorTests
{
    private readonly IRandomArrayGenerator _arrGenerator = Substitute.For<IRandomArrayGenerator>();

    [Test]
    public void CreatesRandomSettingsWithSpecifiedNumberOfWheels()
    {
        // Arrange
        var lowestValue = 0;
        var highestValue = 25;
        var interval = 1;

        var testArr = TestArray();

        _arrGenerator.CreateRandomArray(lowestValue, highestValue, interval).Returns(testArr);
        var numberOfWheels = 3;

        // Execute
        var sut = new RandomSettingsGenerator(_arrGenerator);
        var output = sut.GenerateRandomSettings(numberOfWheels);

        // Assert
        _arrGenerator.Received(numberOfWheels).CreateRandomArray(0, 25, 1);

        Assert.That(output?.WheelSettings, Is.Not.Null);
        Assert.That(output?.WheelSettings?.Length, Is.EqualTo(numberOfWheels));

        // We already assert they are not null
        foreach (var wheelSetting in output!.WheelSettings!)
        {
            Assert.That(wheelSetting.Mappings, Is.EqualTo(testArr));
        }
    } 

    private int[] TestArray()
    {
        var arraySize = 26;
        var testArr = new int[arraySize];

        for (var i = 0; i < arraySize; i++)
        {
            testArr[i] = i;
        }

        return testArr;
    }
}