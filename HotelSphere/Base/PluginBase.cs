using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Base
{
    public abstract class PluginBase : IPlugin
    {
        private readonly string _pluginClassName;

        protected PluginBase(Type pluginClassType)
        {
            _pluginClassName = pluginClassType.ToString();
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                var context = new LocalPluginContext(serviceProvider);
                tracingService.Trace($"Entrando no plugin: {_pluginClassName}");

                ExecutePluginLogic(context);
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Erro inesperado em {_pluginClassName}: {ex}");
                throw new InvalidPluginExecutionException(
                    "Ocorreu um erro ao processar a operação. Contate o suporte.", ex);
            }
        }

        protected abstract void ExecutePluginLogic(LocalPluginContext context);
    }

}
