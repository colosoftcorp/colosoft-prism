using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Colosoft.Prism.Modularity
{
    public class ModuleTypeLoader : global::Prism.Modularity.IModuleTypeLoader
    {
        private readonly IModuleTypeLoaderObserver moduleTypeLoaderObserver;
        private readonly Mef.AssemblyRepositoryCatalog aggregateCatalog;
        private readonly Lazy<Reflection.IAssemblyInfoRepository> assemblyInfoRepository;

        public event EventHandler<global::Prism.Modularity.LoadModuleCompletedEventArgs> LoadModuleCompleted;

        public event EventHandler<global::Prism.Modularity.ModuleDownloadProgressChangedEventArgs> ModuleDownloadProgressChanged;

        public ModuleTypeLoader(
            IModuleTypeLoaderObserver moduleTypeLoaderObserver,
            Lazy<Reflection.IAssemblyInfoRepository> assemblyInfoRepository,
            Mef.AssemblyRepositoryCatalog aggregateCatalog)
        {
            this.moduleTypeLoaderObserver = moduleTypeLoaderObserver ?? throw new ArgumentNullException(nameof(moduleTypeLoaderObserver));
            this.assemblyInfoRepository = assemblyInfoRepository ?? throw new ArgumentNullException(nameof(assemblyInfoRepository));
            this.aggregateCatalog = aggregateCatalog ?? throw new ArgumentNullException(nameof(aggregateCatalog));
        }

        protected virtual void OnFailLoadAssembly(System.Reflection.AssemblyName name, Exception exception)
        {
        }

        public bool CanLoadModuleType(global::Prism.Modularity.IModuleInfo moduleInfo)
        {
            var customInfo = moduleInfo as Colosoft.Prism.Modularity.ModuleInfo;

            if (customInfo != null)
            {
                var moduleTypeName = new Reflection.TypeName(moduleInfo.ModuleType);

                try
                {
                    Reflection.AssemblyInfo assemblyInfo = null;
                    Exception error = null;
                    return this.assemblyInfoRepository.Value.TryGet(moduleTypeName.AssemblyName.Name, out assemblyInfo, out error);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Fail on check 'CanLoadModuleType' for module '{moduleInfo.ModuleName}'.", ex);
                }
            }

            return false;
        }

        public void LoadModuleType(global::Prism.Modularity.IModuleInfo moduleInfo)
        {
            if (moduleInfo is null)
            {
                throw new ArgumentNullException(nameof(moduleInfo));
            }

            var moduleTypeName = new Reflection.TypeName(moduleInfo.ModuleType);

            Reflection.AssemblyInfo assemblyInfo = null;

            try
            {
                Exception error = null;
                this.assemblyInfoRepository.Value.TryGet(moduleTypeName.AssemblyName.Name, out assemblyInfo, out error);

                if (error != null)
                {
                    throw error;
                }
            }
            catch (Exception ex)
            {
                this.RaiseLoadModuleCompleted(moduleInfo, ex);
                return;
            }

            if (assemblyInfo != null)
            {
                this.RaiseModuleDownloadProgressChanged(moduleInfo, 100, 100);

                try
                {
                    this.aggregateCatalog.Add(new Mef.AssemblyRepositoryCatalogRegister()
                        .Add<global::Prism.Modularity.IModule>(moduleTypeName.FullName));
                }
                catch (Exception ex)
                {
                    this.RaiseLoadModuleCompleted(moduleInfo, ex);
                    return;
                }

                this.RaiseLoadModuleCompleted(moduleInfo, null);
            }
            else
            {
                this.RaiseLoadModuleCompleted(moduleInfo, new InvalidOperationException($"Assembly '{moduleTypeName.AssemblyName.Name}' not found"));
            }
        }

        public System.Reflection.Assembly LoadAssembly(string codeBase)
        {
            if (codeBase is null)
            {
                throw new ArgumentNullException(nameof(codeBase));
            }

            System.Reflection.AssemblyName assemblyName;

            try
            {
                assemblyName = System.Reflection.AssemblyName.GetAssemblyName(codeBase);
            }
            catch (ArgumentException)
            {
                assemblyName = new System.Reflection.AssemblyName();
                assemblyName.CodeBase = codeBase;
            }
            catch (System.IO.FileNotFoundException)
            {
                assemblyName = new System.Reflection.AssemblyName();
                assemblyName.CodeBase = codeBase;
            }

            return System.Reflection.Assembly.Load(assemblyName);
        }

        public System.Reflection.Assembly LoadAssemblyGuarded(string codeBase, out Exception exception)
        {
            try
            {
                exception = null;
                return this.LoadAssembly(codeBase);
            }
            catch (System.IO.FileNotFoundException exception2)
            {
                exception = exception2;
            }
            catch (System.IO.FileLoadException exception3)
            {
                exception = exception3;
            }
            catch (BadImageFormatException exception4)
            {
                exception = exception4;
            }
            catch (System.Reflection.ReflectionTypeLoadException exception5)
            {
                exception = exception5;
            }

            return null;
        }

        private void RaiseModuleDownloadProgressChanged(
            global::Prism.Modularity.IModuleInfo moduleInfo,
            long bytesReceived,
            long totalBytesToReceive)
        {
            this.RaiseModuleDownloadProgressChanged(
                new global::Prism.Modularity.ModuleDownloadProgressChangedEventArgs(
                    moduleInfo, bytesReceived, totalBytesToReceive));
        }

        private void RaiseModuleDownloadProgressChanged(global::Prism.Modularity.ModuleDownloadProgressChangedEventArgs e)
        {
            this.ModuleDownloadProgressChanged?.Invoke(this, e);
        }

        private void RaiseLoadModuleCompleted(global::Prism.Modularity.IModuleInfo moduleInfo, Exception error)
        {
            this.RaiseLoadModuleCompleted(new global::Prism.Modularity.LoadModuleCompletedEventArgs(moduleInfo, error));
        }

        private void RaiseLoadModuleCompleted(global::Prism.Modularity.LoadModuleCompletedEventArgs e)
        {
            this.moduleTypeLoaderObserver.Message = ResourceMessageFormatter.Create(
                        () => Properties.Resources.ModuleTypeLoader_LoadedModule, e.ModuleInfo.ModuleName);

            this.moduleTypeLoaderObserver.Total = 100;
            this.moduleTypeLoaderObserver.Current = 100;
            this.moduleTypeLoaderObserver.SetStage(ModuleTypeLoaderStage.Loaded);

            this.LoadModuleCompleted?.Invoke(this, e);
        }
    }
}
