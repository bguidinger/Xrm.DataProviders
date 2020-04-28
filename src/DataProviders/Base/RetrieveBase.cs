namespace BGuidinger.Xrm.DataProviders
{
    using System;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Extensions;

    public abstract class RetrieveBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.Get<IPluginExecutionContext>();
            var service = serviceProvider.GetOrganizationService(Guid.Empty);
            var tracing = serviceProvider.Get<ITracingService>();

            var retriever = serviceProvider.Get<IEntityDataSourceRetrieverService>();
            var dataSource = retriever.RetrieveEntityDataSource();

            var dataService = GetDataService(service, tracing, dataSource);

            var target = context.InputParameterOrDefault<EntityReference>("Target");

            var entity = dataService.GetEntity(target);

            context.OutputParameters["BusinessEntity"] = entity;
        }

        public abstract IDataService GetDataService(IOrganizationService service, ITracingService tracing, Entity dataSource);
    }
}