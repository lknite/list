

using lido.crd.token;
using lido.K8sHelpers;

namespace lido.Helpers
{
    public class ApiKeyManager
    {
        // maps api_key = email
        private IDictionary<Guid, string> vault = new Dictionary<Guid, string>();

        // websockets use an api_key to identify the user, one time, upon initial connect
        public ApiKeyManager()
        {
            Console.WriteLine("Instantiate ApiKeyManager");
        }

        // adds user email and a new vetted api_key
        public Guid add(string claims)
        {
            /*
            if (vault.ContainsKey(email))
            {
                vault.Remove(email);
            }
            */

            Guid guid = Guid.NewGuid();
            Console.WriteLine("new guid: " + guid);

            vault.Add(guid, claims);
            zK8sToken.Post(guid, claims);

            return guid;
        }
        public async Task<string> get(Guid guid)
        {
            string claims;

            // first try in memory vault
            if (!vault.TryGetValue(guid, out claims))
            {
                try
                {
                    CrdToken token = await zK8sToken.generic.ReadNamespacedAsync<CrdToken>(
                                Globals.service.kubeconfig.Namespace, guid.ToString());
                    claims = token.Spec.token.claims;
                }
                catch
                {
                    throw new KeyNotFoundException();
                }
            }

            return claims;
        }
    }
}
