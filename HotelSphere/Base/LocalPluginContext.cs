using Microsoft.Xrm.Sdk;
using System;

namespace HotelSphere.Base
{
    public class LocalPluginContext
    {
        public IPluginExecutionContext Context { get; }
        public IOrganizationServiceFactory ServiceFactory { get; }
        public IOrganizationService Service { get; }
        public ITracingService TracingService { get; }

        public LocalPluginContext(IServiceProvider serviceProvider)
        {
            Context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            ServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            Service = (IOrganizationService)ServiceFactory.CreateOrganizationService(Context.UserId);
            TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        }
    }
}
