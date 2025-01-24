using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using GeospizaPlugin.Properties;

namespace GeospizaPlugin;

public class GeospizaInfo : GH_AssemblyInfo
{
  public override string Name => "Geospiza";

  //Return a 24x24 pixel bitmap to represent this GHA library.
  public override Bitmap Icon => Resources.Solver;

  //Return a short string describing the purpose of this GHA library.
  public override string Description =>
    "Geospiza is a plugin designed for Grasshopper that implements evolutionary algorithms, and is compatible with headless Rhino environments eg. rhino.compute. It includes a range of selection, mutation, and crossover methods, allowing for versatility in addressing different types of computational problems. This tool is primarily aimed at facilitating the exploration and optimization of solutions in design and architecture-related computational tasks.";

  public override Guid Id => new("01219eea-fd96-48e1-829e-0973639bab7b");

  //Return a string identifying you or your company.
  public override string AuthorName => "Felix Brunold; Geospiza";

  //Return a string representing your preferred contact details.
  public override string AuthorContact => "felixbrunold@vektornode.com";

  public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
}