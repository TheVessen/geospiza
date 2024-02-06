using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Geospiza
{
    public class GeospizaInfo : GH_AssemblyInfo
    {
        public override string Name => "Geospiza";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("01219eea-fd96-48e1-829e-0973639bab7b");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}