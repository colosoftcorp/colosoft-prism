using System;

namespace Colosoft.Prism.Modularity
{
    public class ModuleCreatedArgs : EventArgs
    {
        public global::Prism.Modularity.IModuleInfo ModuleInfo { get; set; }

        public global::Prism.Modularity.IModule Module { get; set; }

        public Exception Exception { get; set; }

        public ModuleCreatedArgs(global::Prism.Modularity.IModuleInfo moduleInfo, global::Prism.Modularity.IModule module, Exception exception)
        {
            this.ModuleInfo = moduleInfo;
            this.Module = module;
            this.Exception = exception;
        }
    }
}
