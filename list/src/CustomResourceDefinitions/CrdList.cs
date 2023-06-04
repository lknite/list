using k8s.Models;
using System.Text.Json.Serialization;

namespace list.crd.list
{
    public class CrdList : CustomResourceDefinitions.CustomResource<CrdListSpec, CrdListStatus>
    {
        public override string ToString()
        {
            /*
            var labels = "{";
            foreach (var kvp in Metadata.Labels)
            {
                labels += kvp.Key + " : " + kvp.Value + ", ";
            }
            labels = labels.TrimEnd(',', ' ') + "}";

            return $"{Metadata.Name} (Labels: {labels}), Spec.Enabled: {Spec.Enabled}, Spec.Short: {Spec.Short}, Spec.Long: {Spec.Long}, Spec.Path: {Spec.Path}";
            */

            //return $"{Metadata.Name}, Spec.Enabled: {Spec.Enabled}, Spec.Short: {Spec.Short}, Spec.Long: {Spec.Long}, Spec.Path: {Spec.Path}";
            return "?";
        }
    }

    public class Attr
    {
        [JsonPropertyName("name")]
        public string name { get; set; }
        [JsonPropertyName("value")]
        public string value { get; set; }
    }
    public class List
    {
        [JsonPropertyName("owner")]
        public string owner { get; set; }
        [JsonPropertyName("task")]
        public string task { get; set; }
        [JsonPropertyName("action")]
        public string action { get; set; }
        [JsonPropertyName("state")]
        public string state { get; set; }
        [JsonPropertyName("total")]
        public string total { get; set; }
        [JsonPropertyName("size")]
        public string size { get; set; }
        [JsonPropertyName("priority")]
        public int priority { get; set; }
        [JsonPropertyName("complete")]
        public string complete { get; set; }
        [JsonPropertyName("percent")]
        public string percent { get; set; }
        [JsonPropertyName("timeout")]
        public int timeout { get; set; }
        [JsonPropertyName("ts_add")]
        public string ts_add { get; set; }
        [JsonPropertyName("ts_start")]
        public string ts_start { get; set; }
        [JsonPropertyName("ts_suspend")]
        public string ts_suspend { get; set; }
        [JsonPropertyName("ts_resume")]
        public string ts_resume { get; set; }
        [JsonPropertyName("ts_complete")]
        public string ts_complete { get; set; }
        [JsonPropertyName("attrs")]
        public List<Attr> attrs { get; set; }
    }
    public class CrdListSpec
    {
        [JsonPropertyName("list")]
        public List list { get; set; }
    }

    public class CrdListStatus : V1Status
    {
        [JsonPropertyName("state")]
        public string state { get; set; }
    }
}
