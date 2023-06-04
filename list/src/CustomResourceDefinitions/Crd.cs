using k8s.Models;
using System.Text.Json.Serialization;

namespace lido.CustomResourceDefinitions
{
    public class Crd : CustomResource<CrdSpec, CrdStatus>
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

            return $"{Metadata.Name}";
        }
    }

    public class Names
    {
        [JsonPropertyName("kind")]
        public string kind { get; set; }
    }
    public class CrdSpec
    {
        [JsonPropertyName("names")]
        public Names names { get; set; }
    }

    public class CrdStatus : V1Status
    {
    }
}
