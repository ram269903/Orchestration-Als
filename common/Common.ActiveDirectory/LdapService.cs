using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Common.ActiveDirectory
{
    public class LdapService : ILdapService
    {
        private readonly string _searchBase;

        private readonly LdapSettings _ldapSettings;

        private readonly ILogger _logger;

        private readonly string[] _attributes =
        {
            "objectSid", "objectGUID", "objectCategory", "objectClass", "memberOf", "name", "cn", "distinguishedName",
            "sAMAccountName", "sAMAccountName", "userPrincipalName", "displayName", "givenName", "sn", "description",
            "telephoneNumber", "mail", "streetAddress", "postalCode", "l", "st", "co", "c"
        };

        public LdapService(LdapSettings ldapSettings, ILogger logger)
        {
            _logger = logger;

            _ldapSettings = ldapSettings;
            _searchBase = _ldapSettings.SearchBase;
        }

        public ILdapConnection GetConnection()
        {
            var ldapConnection = new LdapConnection() { SecureSocketLayer = _ldapSettings.UseSSL };

            //Connect function will create a socket connection to the server - Port 389 for insecure and 3269 for secure    
            ldapConnection.Connect(_ldapSettings.ServerName, _ldapSettings.ServerPort);
            //Bind function with null user dn and password value will perform anonymous bind to LDAP server 
            ldapConnection.Bind(_ldapSettings.Credentials.DomainUserName, _ldapSettings.Credentials.Password);

            return ldapConnection;
        }

        public ICollection<LdapEntry> GetGroups(string groupName, bool getChildGroups = false)
        {
            var groups = new Collection<LdapEntry>();

            var filter = $"(&(objectClass=group)(cn={groupName}))";

            using (var ldapConnection = GetConnection())
            {
                var search = ldapConnection.Search(
                    _searchBase,
                    LdapConnection.SCOPE_SUB,
                    filter,
                    _attributes,
                    false,
                    null,
                    null);

                LdapMessage message;

                while ((message = search.getResponse()) != null)
                {
                    if (!(message is LdapSearchResult searchResultMessage))
                    {
                        continue;
                    }

                    var entry = searchResultMessage.Entry;

                    groups.Add(CreateEntryFromAttributes(entry.DN, entry.getAttributeSet()));

                    if (!getChildGroups)
                    {
                        continue;
                    }

                    foreach (var child in GetChildren<LdapEntry>(string.Empty, entry.DN))
                    {
                        groups.Add(child);
                    }
                }
            }

            return groups;
        }

        public ICollection<LdapUser> GetAllUsers()
        {
            return GetUsersInGroups(null);
        }

        public ICollection<LdapUser> GetUsersInGroup(string group)
        {
            return GetUsersInGroups(GetGroups(group));
        }

        public ICollection<LdapUser> GetUsersInGroups(ICollection<LdapEntry> groups)
        {
            var users = new Collection<LdapUser>();

            if (groups == null || !groups.Any())
            {
                users.AddRange(GetChildren<LdapUser>(_searchBase));
            }
            else
            {
                foreach (var group in groups)
                {
                    users.AddRange(GetChildren<LdapUser>(_searchBase, @group.DistinguishedName));
                }
            }

            return users;
        }

        public ICollection<LdapUser> GetUsersByEmailAddress(string emailAddress)
        {
            var users = new Collection<LdapUser>();

            var filter = $"(&(objectClass=user)(mail={emailAddress}))";

            using (var ldapConnection = GetConnection())
            {
                var search = ldapConnection.Search(
                    _searchBase,
                    LdapConnection.SCOPE_SUB,
                    filter,
                    _attributes,
                    false, null, null);

                LdapMessage message;

                while ((message = search.getResponse()) != null)
                {
                    if (!(message is LdapSearchResult searchResultMessage))
                    {
                        continue;
                    }

                    users.Add(CreateUserFromAttributes(_searchBase,
                        searchResultMessage.Entry.getAttributeSet()));
                }
            }

            return users;
        }

        public LdapUser GetUserByUserName(string userName)
        {
            LdapUser user = null;

            var filter = $"(&(objectClass=user)(name={userName}))";

            using (var ldapConnection = GetConnection())
            {
                var search = ldapConnection.Search(
                    _searchBase,
                    LdapConnection.SCOPE_SUB,
                    filter,
                    _attributes,
                    false,
                    null,
                    null);

                LdapMessage message;

                while ((message = search.getResponse()) != null)
                {
                    if (!(message is LdapSearchResult searchResultMessage))
                    {
                        continue;
                    }

                    user = CreateUserFromAttributes(_searchBase, searchResultMessage.Entry.getAttributeSet());
                }
            }

            return user;
        }

        public LdapUser GetAdministrator()
        {
            var name = _ldapSettings.Credentials.DomainUserName.Substring(
                _ldapSettings.Credentials.DomainUserName.IndexOf("\\", StringComparison.Ordinal) != -1
                    ? _ldapSettings.Credentials.DomainUserName.IndexOf("\\", StringComparison.Ordinal) + 1
                    : 0);

            return GetUserByUserName(name);
        }

        public void AddUser(LdapUser user)
        {
            var dn = $"CN={user.FirstName} {user.LastName},{_ldapSettings.ContainerName}";

            var attributeSet = new LdapAttributeSet
        {
            new LdapAttribute("instanceType", "4"),
            new LdapAttribute("objectCategory", $"CN=Person,CN=Schema,CN=Configuration,{_ldapSettings.DomainDistinguishedName}"),
            new LdapAttribute("objectClass", new[] {"top", "person", "organizationalPerson", "user"}),
            new LdapAttribute("name", user.Name),
            new LdapAttribute("cn", $"{user.FirstName} {user.LastName}"),
            new LdapAttribute("sAMAccountName", user.Name),
            new LdapAttribute("userPrincipalName", user.Name),
            new LdapAttribute("unicodePwd", Convert.ToBase64String(Encoding.Unicode.GetBytes($"\"{user.Password}\""))),
            new LdapAttribute("userAccountControl", user.MustChangePasswordOnNextLogon ? "544" : "512"),
            new LdapAttribute("givenName", user.FirstName),
            new LdapAttribute("sn", user.LastName),
            new LdapAttribute("mail", user.EmailAddress)
        };

            if (user.DisplayName != null)
            {
                attributeSet.Add(new LdapAttribute("displayName", user.DisplayName));
            }

            if (user.Description != null)
            {
                attributeSet.Add(new LdapAttribute("description", user.Description));
            }
            if (user.Phone != null)
            {
                attributeSet.Add(new LdapAttribute("telephoneNumber", user.Phone));
            }
            if (user.Address?.Street != null)
            {
                attributeSet.Add(new LdapAttribute("streetAddress", user.Address.Street));
            }
            if (user.Address?.City != null)
            {
                attributeSet.Add(new LdapAttribute("l", user.Address.City));
            }
            if (user.Address?.PostalCode != null)
            {
                attributeSet.Add(new LdapAttribute("postalCode", user.Address.PostalCode));
            }
            if (user.Address?.StateName != null)
            {
                attributeSet.Add(new LdapAttribute("st", user.Address.StateName));
            }
            if (user.Address?.CountryName != null)
            {
                attributeSet.Add(new LdapAttribute("co", user.Address.CountryName));
            }
            if (user.Address?.CountryCode != null)
            {
                attributeSet.Add(new LdapAttribute("c", user.Address.CountryCode));
            }

            var newEntry = new Novell.Directory.Ldap.LdapEntry(dn, attributeSet);

            using (var ldapConnection = GetConnection())
            {
                ldapConnection.Add(newEntry);
            }
        }

        public void DeleteUser(string distinguishedName)
        {
            using (var ldapConnection = GetConnection())
            {
                ldapConnection.Delete(distinguishedName);
            }
        }

        public string GetGroupAuth() {

            DirectoryEntry rootEntry = new DirectoryEntry("LDAP://192.168.1.151:389/DC=BMNEW,DC=local/","user01","User@123");
            string result = string.Empty;

            // if you do repeated domain access, you might want to do this *once* outside this method, 
            // and pass it in as a second parameter!
            PrincipalContext yourDomain = new PrincipalContext(ContextType.Domain);

            // find the user
            UserPrincipal user = UserPrincipal.FindByIdentity(yourDomain, "user01");

            // if user is found
            if (user != null)
            {
                // get DirectoryEntry underlying it
                DirectoryEntry de = (user.GetUnderlyingObject() as DirectoryEntry);

                if (de != null)
                {
                    if (de.Properties.Contains("department"))
                    {
                        result = de.Properties["department"][0].ToString();
                    }
                }
            }

            return result;
        }
        public bool Authenticate(string distinguishedName, string password)
        {
            bool isValid = false;

            using (var ldapConnection = new LdapConnection() { SecureSocketLayer = false })
            {
                string userDN = "CN=" + distinguishedName + "," + _ldapSettings.ContainerName;

                ldapConnection.Connect(_ldapSettings.ServerName, _ldapSettings.ServerPort);

                try
                {

                    ldapConnection.Bind(userDN, password);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error at LDAP");
                    return false;
                }
            }

            }

        public bool AuthenticateWithOutDomain(string distinguishedName, string password)
        {
            bool isValid = false;

            using (var ldapConnection = new LdapConnection() { SecureSocketLayer = false })
            {
                string userDN = _ldapSettings.SearchBase + $@"\" + distinguishedName;

                ldapConnection.Connect(_ldapSettings.ServerName, _ldapSettings.ServerPort);

                try
                {
                    _logger?.LogInformation("Trying to authenticate user with Name: "+distinguishedName);

                    ldapConnection.Bind(userDN, password);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error at LDAP");
                    return false;
                }
            }
        }

            private ICollection<T> GetChildren<T>(string searchBase, string groupDistinguishedName = null)
            where T : ILdapEntry, new()
        {
            var entries = new Collection<T>();

            var objectCategory = "*";
            var objectClass = "*";

            if (typeof(T) == typeof(LdapEntry))
            {
                objectClass = "group";
                objectCategory = "group";

                entries = GetChildren(_searchBase, groupDistinguishedName, objectCategory, objectClass)
                    .Cast<T>().ToCollection();

            }

            if (typeof(T) == typeof(LdapUser))
            {
                objectCategory = "person";
                objectClass = "user";

                entries = GetChildren(_searchBase, null, objectCategory, objectClass).Cast<T>()
                    .ToCollection();

            }

            return entries;
        }

        private ICollection<ILdapEntry> GetChildren(string searchBase, string groupDistinguishedName = null,
            string objectCategory = "*", string objectClass = "*")
        {
            var allChildren = new Collection<ILdapEntry>();

            var filter = string.IsNullOrEmpty(groupDistinguishedName)
                ? $"(&(objectCategory={objectCategory})(objectClass={objectClass}))"
                : $"(&(objectCategory={objectCategory})(objectClass={objectClass})(memberOf={groupDistinguishedName}))";

            using (var ldapConnection = GetConnection())
            {
                var search = ldapConnection.Search(
                    searchBase,
                    LdapConnection.SCOPE_SUB,
                    filter,
                    _attributes,
                    false,
                    null,
                    null);

                LdapMessage message;

                while ((message = search.getResponse()) != null)
                {
                    if (!(message is LdapSearchResult searchResultMessage))
                    {
                        continue;
                    }

                    var entry = searchResultMessage.Entry;

                    if (objectClass == "group")
                    {
                        allChildren.Add(CreateEntryFromAttributes(entry.DN, entry.getAttributeSet()));

                        foreach (var child in GetChildren(string.Empty, entry.DN, objectCategory, objectClass))
                        {
                            allChildren.Add(child);
                        }
                    }

                    if (objectClass == "user")
                    {
                        allChildren.Add(CreateUserFromAttributes(entry.DN, entry.getAttributeSet()));
                    }

                    ;
                }
            }

            return allChildren;
        }

        private LdapUser CreateUserFromAttributes(string distinguishedName, LdapAttributeSet attributeSet)
        {
            var ldapUser = new LdapUser
            {
                ObjectSid = attributeSet.getAttribute("objectSid")?.StringValue,
                ObjectGuid = attributeSet.getAttribute("objectGUID")?.StringValue,
                ObjectCategory = attributeSet.getAttribute("objectCategory")?.StringValue,
                ObjectClass = attributeSet.getAttribute("objectClass")?.StringValue,
                IsDomainAdmin = attributeSet.getAttribute("memberOf") != null && attributeSet.getAttribute("memberOf").StringValueArray.Contains("CN=Domain Admins," + _ldapSettings.SearchBase),
                MemberOf = attributeSet.getAttribute("memberOf")?.StringValueArray,
                CommonName = attributeSet.getAttribute("cn")?.StringValue,
                Name = attributeSet.getAttribute("name")?.StringValue,
                SamAccountName = attributeSet.getAttribute("sAMAccountName")?.StringValue,
                UserPrincipalName = attributeSet.getAttribute("userPrincipalName")?.StringValue,
                //Name = attributeSet.getAttribute("name")?.StringValue,
                DistinguishedName = attributeSet.getAttribute("distinguishedName")?.StringValue ?? distinguishedName,
                DisplayName = attributeSet.getAttribute("displayName")?.StringValue,
                FirstName = attributeSet.getAttribute("givenName")?.StringValue,
                LastName = attributeSet.getAttribute("sn")?.StringValue,
                Description = attributeSet.getAttribute("description")?.StringValue,
                Phone = attributeSet.getAttribute("telephoneNumber")?.StringValue,
                EmailAddress = attributeSet.getAttribute("mail")?.StringValue,
                Address = new LdapAddress
                {
                    Street = attributeSet.getAttribute("streetAddress")?.StringValue,
                    City = attributeSet.getAttribute("l")?.StringValue,
                    PostalCode = attributeSet.getAttribute("postalCode")?.StringValue,
                    StateName = attributeSet.getAttribute("st")?.StringValue,
                    CountryName = attributeSet.getAttribute("co")?.StringValue,
                    CountryCode = attributeSet.getAttribute("c")?.StringValue
                },

                SamAccountType = int.Parse(attributeSet.getAttribute("sAMAccountType")?.StringValue ?? "0"),
            };

            return ldapUser;
        }

        private LdapEntry CreateEntryFromAttributes(string distinguishedName, LdapAttributeSet attributeSet)
        {
            return new LdapEntry
            {
                ObjectSid = attributeSet.getAttribute("objectSid")?.StringValue,
                ObjectGuid = attributeSet.getAttribute("objectGUID")?.StringValue,
                ObjectCategory = attributeSet.getAttribute("objectCategory")?.StringValue,
                ObjectClass = attributeSet.getAttribute("objectClass")?.StringValue,
                CommonName = attributeSet.getAttribute("cn")?.StringValue,
                Name = attributeSet.getAttribute("name")?.StringValue,
                DistinguishedName = attributeSet.getAttribute("distinguishedName")?.StringValue ?? distinguishedName,
                SamAccountName = attributeSet.getAttribute("sAMAccountName")?.StringValue,
                SamAccountType = int.Parse(attributeSet.getAttribute("sAMAccountType")?.StringValue ?? "0"),
            };
        }

        private SecurityIdentifier GetDomainSid()
        {
            var administratorAcount = new NTAccount(_ldapSettings.DomainName, "administrator");
            var administratorSId = (SecurityIdentifier)administratorAcount.Translate(typeof(SecurityIdentifier));
            return administratorSId.AccountDomainSid;
        }
    }
}

