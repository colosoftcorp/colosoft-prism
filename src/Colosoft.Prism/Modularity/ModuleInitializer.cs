using System;

namespace Colosoft.Prism.Modularity
{
    public delegate void ModuleCreatedEventHandler(object sender, ModuleCreatedArgs e);

    public class ModuleInitializer : global::Prism.Modularity.ModuleInitializer
    {
        private readonly global::Prism.Ioc.IContainerExtension serviceLocator;
        private readonly IModuleInitializerObserver observer;

        public event ModuleCreatedEventHandler Created;

        public ModuleInitializer(
            global::Prism.Ioc.IContainerExtension containerExtension,
            IModuleInitializerObserver observer)
            : base(containerExtension)
        {
            this.serviceLocator = containerExtension;
            this.observer = observer;
        }

        protected void OnCreated(global::Prism.Modularity.IModuleInfo moduleInfo, global::Prism.Modularity.IModule module, Exception exception)
        {
            this.observer.OnCreatedModule(moduleInfo, module, exception);

            this.Created?.Invoke(this, new ModuleCreatedArgs(moduleInfo, module, exception));
        }

        protected override global::Prism.Modularity.IModule CreateModule(global::Prism.Modularity.IModuleInfo moduleInfo)
        {
            if (moduleInfo is null)
            {
                throw new ArgumentNullException(nameof(moduleInfo));
            }

            var customInfo = moduleInfo as ModuleInfo;
            global::Prism.Modularity.IModule module = null;

            this.observer.OnCreatingModule(moduleInfo);

            try
            {
                var typeName = new Reflection.TypeName(moduleInfo.ModuleType);

                if (customInfo != null)
                {
                    module = this.serviceLocator.Resolve(typeof(global::Prism.Modularity.IModule), typeName.FullName) as global::Prism.Modularity.IModule;
                }
                else
                {
                    module = base.CreateModule(moduleInfo);
                }
            }
            catch (Exception ex)
            {
                if (ex is System.Reflection.TargetInvocationException)
                {
                    ex = ex.InnerException;
                }

                this.OnCreated(moduleInfo, null, ex);
                throw;
            }

            this.OnCreated(moduleInfo, module, null);

            return module;
        }
    }
}
