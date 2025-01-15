namespace GeospizaManager.Core
{
  public class Fitness
  {
    private static readonly Lazy<Fitness> _instance = new Lazy<Fitness>(() => new Fitness());

    private double _fitness;

    private Fitness()
    {
    }

    public static Fitness Instance => _instance.Value;

    public void SetFitness(double value)
    {
      _fitness = value;
    }

    public double GetFitness()
    {
      return _fitness;
    }
  }
}