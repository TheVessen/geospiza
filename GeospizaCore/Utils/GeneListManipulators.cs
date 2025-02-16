﻿namespace GeospizaCore.Utils;

public static class GeneListManipulators
{
    /// <summary>
    ///     Calculates the actual value from a tick value within a genepool.
    /// </summary>
    /// <param name="tickValue">The tick value to be converted back to an actual value.</param>
    /// <param name="genepool">The genepool object, which should have Minimum, Maximum, and Decimals properties.</param>
    /// <returns>
    ///     The actual value corresponding to the tick value within the genepool range, rounded to the specified number of
    ///     decimals.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the tick value is out of the genepool range.</exception>
    public static decimal GetValueFromTick(int tickValue, dynamic genepool)
    {
        // Calculate the range of the genepool
        decimal range = genepool.Maximum - genepool.Minimum;
        var totalTicks = Convert.ToInt32(range * (decimal)Math.Pow(10, genepool.Decimals));
        if (tickValue < 0 || tickValue > totalTicks)
            throw new ArgumentOutOfRangeException(nameof(tickValue), "Tick value is out of range.");
        decimal actualValue = genepool.Minimum + (decimal)tickValue / totalTicks * range;
        return Math.Round(actualValue, genepool.Decimals);
    }
}