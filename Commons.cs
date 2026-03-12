using System.DirectoryServices;
using Microsoft.Exchange.WebServices.Data;
using System.Net;

namespace CORE_BE
{
    public class Commons
    {
        /// <summary>
        /// Escape special characters for LDAP filter to prevent LDAP injection
        /// </summary>
        private static string EscapeLdapFilter(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }

        public static bool LoginLDAP(string userName, string password, string domainName, IConfiguration config)
        {
            var ldapSettings = config.GetSection("LdapSettings");
            var thacoDomains = ldapSettings.GetSection("ThacoDomains").Get<string[]>() ?? [];
            var primaryServers = ldapSettings.GetSection("PrimaryServers").Get<string[]>() ?? [];
            var secondaryServers = ldapSettings.GetSection("SecondaryServers").Get<string[]>() ?? [];

            string domainAndUsername;
            string[] ldapServers;

            if (thacoDomains.Contains(domainName, StringComparer.OrdinalIgnoreCase))
            {
                domainAndUsername = userName + "@thaco.com.vn";
                ldapServers = primaryServers;
            }
            else
            {
                domainAndUsername = userName + "@thacomazda.vn";
                ldapServers = secondaryServers;
            }

            // Try each LDAP server in order until one succeeds
            DirectoryEntry entry = null;
            foreach (var server in ldapServers)
            {
                try
                {
                    entry = new DirectoryEntry($"LDAP://{server}", domainAndUsername, password);
                    // Force authentication by accessing NativeObject
                    _ = entry.NativeObject;
                    break; // Connection succeeded
                }
                catch
                {
                    entry = null;
                    continue; // Try next server
                }
            }

            if (entry == null)
                return false;

            try
            {
                var search = new DirectorySearcher(entry);
                var safeUserName = EscapeLdapFilter(userName);
                search.Filter = $"(SAMAccountName={safeUserName})";
                search.PropertiesToLoad.Add("cn");
                SearchResult result = search.FindOne();
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool LoginExchange(string email, string password)
        {
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013);
            service.Credentials = new WebCredentials(email, password);
            service.Url = new Uri("https://mail.thaco.com.vn/ews/exchange.asmx");
            try
            {
                var findFolderResults = service.FindFolders(
                    WellKnownFolderName.Root,
                    new FolderView(1)
                );
                return findFolderResults != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
