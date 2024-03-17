namespace Geospiza.Core;

public class Pair
{
  public Pair(Individual individual1, Individual individual2)
  {
    Individual1 = individual1;
    Individual2 = individual2;
  }

  public Individual Individual1 { get; }
  public Individual Individual2 { get; }
}