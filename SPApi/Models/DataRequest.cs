using System.Collections.Generic;

namespace SPApi.Models
{
    public class DataRequest
    {
        public string Schema { get; set; }

        public string Object { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new();

        public string Context { get; set; }

        public int CommandTimeout { get; set; } = 30;

        public string User { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Claims { get; set; }
    }
}
