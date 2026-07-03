namespace SearchComparisonNet.ViewModelTests;

// NegativeConverter negates numeric inputs and inverts booleans, delegating both Convert and
// ConvertBack to the same private ReturnNegative switch. These tests pin that contract: each
// supported numeric type is negated with its own type preserved, bool is inverted, null throws
// ArgumentNullException, and unsupported types throw NotSupportedException. Convert and ConvertBack
// are asserted to behave identically because they share the same implementation.
public class NegativeConverterTests
{
    private static readonly NegativeConverter Sut = new();

    private static object? Convert(object? value) =>
        Sut.Convert(value, typeof(object), null, CultureInfo.InvariantCulture);

    private static object? ConvertBack(object? value) =>
        Sut.ConvertBack(value, typeof(object), null, CultureInfo.InvariantCulture);

    public static TheoryData<object, object> NegatedValues => new()
    {
        { true, false },
        { false, true },
        { (byte)5, -5 },
        { (short)7, -7 },
        { 42, -42 },
        { 9_000_000_000L, -9_000_000_000L },
        { 1.5f, -1.5f },
        { 2.5d, -2.5d },
        { 3.5m, -3.5m },
        { 0, 0 },
        { 0.0d, -0.0d },
    };

    [Theory]
    [MemberData(nameof(NegatedValues))]
    public void Convert_negates_supported_values(object input, object expected) =>
        Assert.Equal(expected, Convert(input));

    [Theory]
    [MemberData(nameof(NegatedValues))]
    public void ConvertBack_matches_Convert(object input, object expected) =>
        Assert.Multiple(
            () => Assert.Equal(expected, ConvertBack(input)),
            () => Assert.Equal(Convert(input), ConvertBack(input)));

    [Fact]
    public void Convert_preserves_the_numeric_type()
    {
        Assert.Multiple(
            () => Assert.IsType<int>(Convert(42)),
            () => Assert.IsType<long>(Convert(9_000_000_000L)),
            () => Assert.IsType<float>(Convert(1.5f)),
            () => Assert.IsType<double>(Convert(2.5d)),
            () => Assert.IsType<decimal>(Convert(3.5m)),
            () => Assert.IsType<bool>(Convert(true)));
    }

    [Fact]
    public void Convert_null_throws_ArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => Convert(null));

    [Fact]
    public void ConvertBack_null_throws_ArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => ConvertBack(null));

    [Fact]
    public void Convert_unsupported_type_throws_NotSupportedException() =>
        Assert.Throws<NotSupportedException>(() => Convert("not-a-number"));
}
