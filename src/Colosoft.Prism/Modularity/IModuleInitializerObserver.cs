using System;

namespace Colosoft.Prism.Modularity
{
    public interface IModuleInitializerObserver
    {
        void OnCreatingModule(global::Prism.Modularity.IModuleInfo moduleInfo);

        void OnCreatedModule(global::Prism.Modularity.IModuleInfo moduleInfo, global::Prism.Modularity.IModule module, Exception exception);
    }
}
