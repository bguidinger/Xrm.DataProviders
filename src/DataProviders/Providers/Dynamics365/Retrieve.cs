namespace BGuidinger.Xrm.DataProviders.Dynamics365
{
    using Microsoft.Xrm.Sdk;

    public class Retrieve : RetrieveBase
    {
        public override IDataService GetDataService(IOrganizationService service, Entity dataSource)
        {
            return new DataService(service, dataSource);
        }
    }
}