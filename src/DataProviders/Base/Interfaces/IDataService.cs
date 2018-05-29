namespace BGuidinger.Xrm.DataProviders
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;

    public interface IDataService
    {
        EntityCollection GetEntities(QueryExpression query);
        Entity GetEntity(EntityReference reference);
    }
}