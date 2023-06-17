using k8s.Models;
using System.Text.Json.Serialization;

namespace list.crd.block
{
    public class CrdBlock : CustomResourceDefinitions.CustomResource<CrdBlockSpec, CrdBlockStatus>
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

    public class Block
    {
        [JsonPropertyName("list")]
        public string list { get; set; }
        [JsonPropertyName("block")]
        public string block { get; set; }
        [JsonPropertyName("owner")]
        public string owner { get; set; }
        [JsonPropertyName("index")]
        public string index { get; set; }
        [JsonPropertyName("size")]
        public string size { get; set; }
        [JsonPropertyName("state")]
        public string state { get; set; }
        [JsonPropertyName("when")]
        public string when { get; set; }
    }
    public class CrdBlockSpec
    {
        [JsonPropertyName("block")]
        public Block block { get; set; }
    }

    public class CrdBlockStatus : V1Status
    {
        [JsonPropertyName("state")]
        public string state { get; set; }
    }
}
