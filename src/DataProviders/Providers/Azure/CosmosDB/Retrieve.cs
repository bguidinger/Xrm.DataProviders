namespace BGuidinger.Xrm.DataProviders.CosmosDB
{
    using Microsoft.Xrm.Sdk;

    public class Retrieve : RetrieveBase
    {
        public override IDataService GetDataService(IOrganizationService service, ITracingService tracing, Entity dataSource)
        {
            return new DataService(service, tracing, dataSource);
        }
    }
}