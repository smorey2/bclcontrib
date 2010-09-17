﻿using System.Linq;
using System.Security.Principal;
using System.Collections.Generic;
namespace System.DirectoryServices
{
    public class DirectoryGateway
    {
        public static readonly SecurityIdentifier WorldSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        public static readonly SecurityIdentifier AnonymousSid = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null);

        private readonly string _domain;
        private readonly string _userId;
        private readonly string _password;

        public DirectoryGateway(string domain, string userId, string password, string userContainer)
        {
            _domain = domain;
            _userId = userId;
            _password = password;
            UserContainer = userContainer;
        }

        public string UserContainer { get; private set; }

        protected T MakeAsAuthenticated<T, TTag>(Func<string, string, string, TTag, T> builder, TTag tag)
        {
            return builder(_domain, _userId, _password, tag);
        }

        public static string ParseUserId(IPrincipal user)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            return (user.Identity != null ? ParseUserId(user.Identity.Name) : null);
        }
        public static string ParseUserId(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            var index = path.IndexOf("\\");
            return (index > 0 ? path.Substring(index + 1) : path);
            //var userPath = path.Split(new char[] { '\\' });
            //return userPath[userPath.Length - 1];
        }

        public static IEnumerable<T> GetDirectoryEntriesByIdentity<T>(DirectoryEntry directoryEntry, string[] propertiesToLoad, IEnumerable<string> identities, Func<SearchResult, T> selector)
        {
            if ((identities == null) || (!identities.GetEnumerator().MoveNext()))
                return new List<T>();
            //
            var searcher = new DirectorySearcher(directoryEntry, "query", propertiesToLoad);
            //return identities
            //    .Select(identity => UserPrincipal.FindByIdentity(context, identityType, identity))
            //    .Where(p => p != null)
            //    .Select(selector)
            //    .ToList();
            return null;
        }

        public static IEnumerable<T> GetDirectoryEntryMemberOf<T>(string container, DirectoryEntry directoryEntry, Func<string, T> selector) { return GetDirectoryEntryMemberOf<T>(container, null, directoryEntry, selector); }
        public static IEnumerable<T> GetDirectoryEntryMemberOf<T>(string container, string suffix, DirectoryEntry directoryEntry, Func<string, T> selector)
        {
            if (string.IsNullOrEmpty(container))
                throw new ArgumentNullException("container");
            suffix = (suffix ?? string.Empty);
            if (directoryEntry == null)
                throw new ArgumentNullException("directoryEntry");
            if (selector == null)
                throw new ArgumentNullException("builder");
            var memberOf = directoryEntry.Properties["memberOf"].Value;
            string filter = suffix + "," + container;
            //
            IEnumerable<string> set;
            if (memberOf is object[])
                set = ((object[])memberOf).Cast<string>();
            else if (memberOf is string)
                set = new[] { (string)memberOf };
            else
                return null;
            //
            return set
                .Where(c => c.EndsWith(filter) && (c.LastIndexOf(',', c.Length - filter.Length - 1) == -1))
                .Select(c => selector(c.Substring(3, c.Length - filter.Length - 3) + suffix))
                .Where(c => c != null)
                .ToList();
        }

        public void MoveDirectoryEntryTo(DirectoryEntry directoryEntry, string newContainer) { MoveDirectoryEntryTo(directoryEntry, GetDirectoryEntry(newContainer)); }
        public static void MoveDirectoryEntryTo(DirectoryEntry directoryEntry, DirectoryEntry newContainer)
        {
            if (directoryEntry == null)
                throw new ArgumentNullException("directoryEntry");
            if (newContainer != null)
            {
                directoryEntry.MoveTo(newContainer);
                directoryEntry.CommitChanges();
            }
        }

        public DirectoryEntry GetDirectoryEntry(string container)
        {
            var authenticationTypes = (_domain.EndsWith(":636") ? AuthenticationTypes.SecureSocketsLayer | AuthenticationTypes.Secure : AuthenticationTypes.Secure);
            return new DirectoryEntry("LDAP://" + _domain + "/" + container, _userId, _password, authenticationTypes);
        }
    }
}