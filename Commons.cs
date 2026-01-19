using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.DirectoryServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CORE_BE
{
    public class Commons
    {
        public static bool LoginLDAP(string userName, string password, string domainName)
        {
            string domainAndUsername = "";
            DirectoryEntry entry;
            if (
                domainName == "thaco.com.vn"
                || domainName == "thagrico.vn"
                || domainName == "thiso.vn"
                || domainName == "thilogi.com.vn"
                || domainName == "thadico.vn"
            )
            {
                domainAndUsername = userName + "@thaco.com.vn";
                try
                {
                    entry = new DirectoryEntry("LDAP://10.10.2.73", domainAndUsername, password);
                }
                catch (System.Exception)
                {
                    try
                    {
                        entry = new DirectoryEntry(
                            "LDAP://10.10.2.10",
                            domainAndUsername,
                            password
                        );
                    }
                    catch (System.Exception)
                    {
                        try { }
                        catch (System.Exception)
                        {
                            entry = new DirectoryEntry(
                                "LDAP://10.10.2.9",
                                domainAndUsername,
                                password
                            );
                            throw;
                        }
                        throw;
                    }
                    throw;
                }
                // entry = new DirectoryEntry("LDAP://thaco.com.vn", domainAndUsername, password);
            }
            else
            {
                domainAndUsername = userName + "@thacomazda.vn";
                try
                {
                    entry = new DirectoryEntry("LDAP://10.40.12.2", domainAndUsername, password);
                }
                catch (System.Exception)
                {
                    entry = new DirectoryEntry("LDAP://10.10.2.54", domainAndUsername, password);
                    throw;
                }
                // entry = new DirectoryEntry("LDAP://vinamazda.vn", domainAndUsername, password);
            }
            try
            {
                // Bind to the native AdsObject to force authentication.
                Object obj = entry.NativeObject;
                DirectorySearcher search = new DirectorySearcher(entry);
                search.Filter = "(SAMAccountName=" + userName + ")";
                search.PropertiesToLoad.Add("cn");
                SearchResult result = search.FindOne();
                if (null == result)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (System.Exception)
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
                if (findFolderResults != null)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
