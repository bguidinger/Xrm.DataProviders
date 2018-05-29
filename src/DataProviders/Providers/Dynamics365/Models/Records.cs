namespace BGuidinger.Xrm.DataProviders.Dynamics365
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Records
    {
        [DataMember(Name = "@odata.context")]
        public string Context { get; set; }

        [DataMember(Name = "@odata.count")]
        public int Count { get; set; }

        [DataMember(Name = "@odata.nextLink")]
        public string NextLink { get; set; }

        [DataMember(Name = "value")]
        public IEnumerable<Record> Value { get; set; }
    }
}