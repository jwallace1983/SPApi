namespace SPApi.UnitTests
{
    public class SettingsTest
    {
        [Fact]
        public void ConstructorTest()
        {
            // Arrange / Act
            var target = new Settings();

            // Assert
            target.RequireHttps.Should().BeTrue();
            target.HelpKey.Should().BeNull();
            target.EnableHelp.Should().BeFalse();
            target.Endpoint.Should().Be("/api/data");
        }
    }
}
