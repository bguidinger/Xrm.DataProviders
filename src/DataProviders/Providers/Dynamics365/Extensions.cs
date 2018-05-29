namespace BGuidinger.Xrm.DataProviders.Dynamics365
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Metadata;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;

    public static partial class Extensions
    {
        public static string UrlEncode(this Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(x => $"{x.Key}={WebUtility.UrlEncode(x.Value)}"));
        }

        public static void Write(this WebRequest request, Dictionary<string, string> parameters)
        {
            var body = Encoding.UTF8.GetBytes(parameters.UrlEncode());

            request.ContentLength = body.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }
        }

        public static Entity ToEntity(this Record record, EntityMetadata metadata)
        {
            var entity = new Entity(metadata.LogicalName);

            foreach (var attribute in metadata.Attributes)
            {
                if (record.ContainsKey(attribute.ExternalName))
                {
                    var value = (object)record[attribute.ExternalName];
                    entity.Attributes[attribute.LogicalName] = attribute.ToXrm(value);
                    if (attribute.LogicalName == metadata.PrimaryIdAttribute)
                    {
                        entity.Id = (Guid)attribute.ToXrm(value);
                    }
                }
            }

            return entity;
        }

        public static object ToXrm(this AttributeMetadata metadata, object value)
        {
            switch (metadata.AttributeType)
            {
                case AttributeTypeCode.Picklist:
                    return value == null ? null : new OptionSetValue((int)value);
                case AttributeTypeCode.Uniqueidentifier:
                    return value == null ? (Guid?)null : new Guid((string)value);
                case AttributeTypeCode.DateTime:
                    return value == null ? (DateTime?)null : DateTime.Parse((string)value);
                case AttributeTypeCode.Money:
                    return value == null ? null : new Money((decimal)value);
                default:
                    return value;
            }
        }
    }
}