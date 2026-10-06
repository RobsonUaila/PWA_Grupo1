using Drought.Domain.Enums;
using Xunit;

namespace Drought.Tests;

/// <summary>Testes unitários simples sobre o mapeamento de classes de seca.</summary>
public class PredictionResultMappingTests
{
    [Theory]
    [InlineData("Normal", DroughtClass.Normal)]
    [InlineData("SecaModerada", DroughtClass.SecaModerada)]
    [InlineData("SecaSevera", DroughtClass.SecaSevera)]
    [InlineData("SecaExtrema", DroughtClass.SecaExtrema)]
    public void Enum_Parse_MapsCorrectly(string text, DroughtClass expected)
    {
        var parsed = Enum.Parse<DroughtClass>(text);
        Assert.Equal(expected, parsed);
    }
}
