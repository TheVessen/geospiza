using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ChartHopperGH;

public class ChartHopperGHInfo : GH_AssemblyInfo
{
    public override string Name => "ChartHopper";
    public override string Description => "Interactive Plotly.js charts for Grasshopper";
    public override Guid Id => new("B3F1A2D4-8E7C-4F5A-9B6D-2C1E3A4F5B6C");
    public override string AuthorName => "Felix Brunold";
    public override string AuthorContact => "";
    public override Bitmap Icon => null;
}
