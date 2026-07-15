using Xunit;

namespace LibraryManagement.Business.UnitTests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void BusinessAssemblyCanBeLoaded()
    {
        var assembly = typeof(Business.AssemblyMarker).Assembly;

        Assert.Equal("LibraryManagement.Business", assembly.GetName().Name);
    }
}
