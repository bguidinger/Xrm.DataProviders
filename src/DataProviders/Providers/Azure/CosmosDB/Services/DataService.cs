namespace BGuidinger.Xrm.DataProviders.CosmosDB
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Data.Mappings;
    using Microsoft.Xrm.Sdk.Extensions;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    public class DataService : IDataService
    {
        private readonly IOrganizationService _service;
        private readonly ITracingService _tracing;
        private readonly Entity _dataSource;

        public DataService(IOrganizationService service, ITracingService tracing, Entity dataSource)
        {
            _service = service;
            _tracing = tracing;
            _dataSource = dataSource;
        }

        public EntityCollection GetEntities(QueryExpression query)
        {
            _tracing.Trace("Entity Name: " + query.EntityName);

            var metadata = _service.GetEntityMetadata(query.EntityName);

            var visitor = new SqlVisitor(metadata);
            query.Accept(visitor);

            _tracing.Trace("Query: " + visitor.Query);

            var entities = ExecuteQuery(visitor.Query, metadata);

            return new EntityCollection(entities.ToArray());
        }

        public Entity GetEntity(EntityReference reference)
        {
            var metadata = _service.GetEntityMetadata(reference.LogicalName);

            var query = new QueryExpression(reference.LogicalName);
            query.Criteria.AddCondition(metadata.PrimaryIdAttribute, ConditionOperator.Equal, reference.Id);

            return GetEntities(query).Entities.FirstOrDefault();
        }

        private IEnumerable<Entity> ExecuteQuery(string query, EntityMetadata metadata)
        {
            var uri = _dataSource.GetAttributeValue<string>("bg_uri");
            var key = _dataSource.GetAttributeValue<string>("bg_key");
            var database = _dataSource.GetAttributeValue<string>("bg_database");
            var collection = metadata.ExternalName;

            var date = DateTime.UtcNow.ToString("R");

            var resourceType = "docs";
            var resourceId = $"dbs/{database}/colls/{collection}";

            var hash = GenerateHash("POST", resourceType, resourceId, date, key, "master", "1.0");

            var request = WebRequest.CreateHttp($"{uri}/{resourceId}/{resourceType}");
            request.Method = "POST";
            request.ContentType = "application/query+json";
            request.Headers.Add("Authorization", hash);
            request.Headers.Add("X-MS-Date", date);
            request.Headers.Add("X-MS-Version", "2017-02-22");
            request.Headers.Add("X-MS-DocumentDB-IsQuery", "True");

            var body = Encoding.UTF8.GetBytes($"{{\"query\": \"{query}\"}}");
            using (var stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }

            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            {
                var mapper = EntityMapFactory.Create(metadata, new DefaultTypeMapFactory(), null);

                var settings = new DataContractJsonSerializerSettings()
                {
                    UseSimpleDictionaryFormat = true
                };
                var serializer = new DataContractJsonSerializer(typeof(Records), settings);

                var records = serializer.ReadObject(stream) as Records;
                foreach (var record in records.Documents)
                {
                    var entityId = record[mapper.MapAttributeNameExternal(metadata.PrimaryIdAttribute)];
                    var entity = new Entity(metadata.LogicalName, new Guid(entityId.ToString()));

                    foreach (var attribute in metadata.Attributes)
                    {
                        _tracing.Trace("Attribute: " + attribute.LogicalName);
                        var logicalName = attribute.LogicalName;
                        var externalName = mapper.MapAttributeNameExternal(logicalName);

                        if (record.ContainsKey(externalName))
                        {
                            entity[logicalName] = record[externalName];
                        }
                    }

                    yield return entity;
                }
            }
        }

        private string GenerateHash(string verb, string resourceType, string resourceId, string date, string key, string keyType, string version)
        {
            var hash = new HMACSHA256 { Key = Convert.FromBase64String(key) };

            var payLoad = $"{verb.ToLower()}\n{resourceType.ToLower()}\n{resourceId}\n{date.ToLower()}\n\n";
            var payloadHash = hash.ComputeHash(Encoding.UTF8.GetBytes(payLoad));

            var signature = Convert.ToBase64String(payloadHash);

            return WebUtility.UrlEncode($"type={keyType}&ver={version}&sig={signature}");
        }
    }
}