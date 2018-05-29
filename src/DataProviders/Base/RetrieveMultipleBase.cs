namespace BGuidinger.Xrm.DataProviders
{
    using System;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Extensions;
    using Microsoft.Xrm.Sdk.Query;

    public abstract class RetrieveMultipleBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.Get<IPluginExecutionContext>();
            var service = serviceProvider.GetOrganizationService(Guid.Empty);

            var retriever = serviceProvider.Get<IEntityDataSourceRetrieverService>();
            var dataSource = retriever.RetrieveEntityDataSource();

            var dataService = GetDataService(service, dataSource);

            var query = context.InputParameterOrDefault<QueryExpression>("Query");

            var entities = dataService.GetEntities(query);

            context.OutputParameters["BusinessEntityCollection"] = entities;
        }

        public abstract IDataService GetDataService(IOrganizationService service, Entity dataSource);
    }
}