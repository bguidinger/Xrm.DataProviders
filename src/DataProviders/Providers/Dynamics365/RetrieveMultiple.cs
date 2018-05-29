namespace BGuidinger.Xrm.DataProviders.Dynamics365
{
    using Microsoft.Xrm.Sdk;

    public class RetrieveMultiple : RetrieveMultipleBase
    {
        public override IDataService GetDataService(IOrganizationService service, Entity dataSource)
        {
            return new DataService(service, dataSource);
        }
    }
}