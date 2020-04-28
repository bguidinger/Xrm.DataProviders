namespace BGuidinger.Xrm.DataProviders.CosmosDB
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Records
    {
        [DataMember(Name = "_rid")]
        public string RecordId { get; set; }

        [DataMember(Name = "_count")]
        public int Count { get; set; }

        [DataMember(Name = "Documents")]
        public IEnumerable<Record> Documents { get; set; }
    }
}