namespace MethodOverloadGenerator.Models;

internal sealed record AllowedRulesContext
{
    public required bool AllowRule1 { get; init; }
    public required bool AllowRule2 { get; init; }
    public required bool AllowRule3 { get; init; }
    public required bool AllowRule4 { get; init; }
    public required bool AllowRule5 { get; init; }
}
