namespace SearchComparisonNet.ViewModelTests;

// NumStringConverter formats numbers for display and parses them back. Its Convert branch is
// intentionally subtle: only *double* inputs can be "abs-log small" (|log10| < 3), which selects
// the D3 padded / G3 form; every non-double that parses as a long always takes the G3 branch; a
// whole-valued double stringifies without a decimal point and therefore flows through the long
// branch too. These tests pin that documented behavior with explicit, invariant-culture cases so
// they are deterministic on any locale. ConvertBack is asserted to round-trip via InvariantCulture,
// matching Convert (the fix in chore/kernel-and-converter-polish).
public class NumStringConverterTests
{
    private static readonly NumStringConverter Sut = new();

    private static object? Convert(object? value) =>
        Sut.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

    private static object? ConvertBack(object? value) =>
        Sut.ConvertBack(value, typeof(object), null, CultureInfo.InvariantCulture);

    [Fact]
    public void Convert_null_returns_null() =>
        Assert.Null(Convert(null));

    // Non-double integers never satisfy "isAbsLogSmall" (value is double is false), so they always
    // take the G3 branch regardless of magnitude.
    [Theory]
    [InlineData(42, "42")]
    [InlineData(7, "7")]
    [InlineData(123456, "1.23E+05")]
    public void Convert_integer_uses_G3(int input, string expected) =>
        Assert.Equal(expected, Convert(input));

    // Whole-valued doubles stringify without a decimal point, so long.TryParse succeeds and the
    // isAbsLogSmall flag (|log10| < 3) chooses between D3 (padded) and G3.
    [Theory]
    [InlineData(42.0, "042")]     // |log10(42)| ~= 1.62 < 3  -> D3
    [InlineData(500.0, "500")]    // |log10(500)| ~= 2.70 < 3 -> D3
    [InlineData(1234.0, "1.23E+03")] // |log10(1234)| ~= 3.09, not < 3 -> G3
    public void Convert_whole_double_selects_D3_or_G3(double input, string expected) =>
        Assert.Equal(expected, Convert(input));

    // Fractional doubles fail long.TryParse and fall to the double branch: G3 when abs-log small,
    // scientific otherwise.
    [Theory]
    [InlineData(1.5, "1.5")]              // |log10(1.5)| ~= 0.18 < 3 -> G3
    [InlineData(0.0005, "5.00E-04")]      // |log10(0.0005)| ~= 3.30, not < 3 -> scientific
    public void Convert_fractional_double_selects_G3_or_scientific(double input, string expected) =>
        Assert.Equal(expected, Convert(input));

    [Fact]
    public void ConvertBack_parses_integer_as_int()
    {
        var result = ConvertBack("42");

        Assert.Multiple(
            () => Assert.IsType<int>(result),
            () => Assert.Equal(42, result));
    }

    [Fact]
    public void ConvertBack_parses_decimal_as_double()
    {
        var result = ConvertBack("1.5");

        Assert.Multiple(
            () => Assert.IsType<double>(result),
            () => Assert.Equal(1.5d, result));
    }

    // The InvariantCulture fix: "1.5" must parse as one-point-five (not fifteen) irrespective of the
    // ambient culture's decimal separator.
    [Fact]
    public void ConvertBack_uses_invariant_decimal_separator() =>
        Assert.Equal(1.5d, ConvertBack("1.5"));

    [Fact]
    public void ConvertBack_non_numeric_returns_original_string() =>
        Assert.Equal("abc", ConvertBack("abc"));
}
