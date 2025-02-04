namespace GeospizaCore.Core;

/// <summary>
///     Describes a pair of individuals
/// </summary>
public class IndividualPair
{
    public IndividualPair(Individual individual1, Individual individual2)
    {
        Individual1 = individual1;
        Individual2 = individual2;
    }

    public Individual Individual1 { get; }
    public Individual Individual2 { get; }
}