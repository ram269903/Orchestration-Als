﻿using System.Collections.Generic;

namespace Common.ActiveDirectory
{
    public interface ILdapService
    {
        ICollection<LdapEntry> GetGroups(string groupName, bool getChildGroups = false);

        ICollection<LdapUser> GetUsersInGroup(string groupName);

        ICollection<LdapUser> GetUsersInGroups(ICollection<LdapEntry> groups = null);

        ICollection<LdapUser> GetUsersByEmailAddress(string emailAddress);

        ICollection<LdapUser> GetAllUsers();

        LdapUser GetAdministrator();

        LdapUser GetUserByUserName(string userName);

        void AddUser(LdapUser user);

        void DeleteUser(string distinguishedName);

        bool Authenticate(string distinguishedName, string password);

        //void AddUser(LdapUser user, string password);
    }
}
