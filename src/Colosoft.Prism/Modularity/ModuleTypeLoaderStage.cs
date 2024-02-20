using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosoft.Prism.Modularity
{
    public enum ModuleTypeLoaderStage
    {
        GetModulesFromProvider,
        Loading,
        Downloading,
        Loaded,
    }
}
