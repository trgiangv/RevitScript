using System.Security.Principal;

namespace RevitScript.Runtime.Common.Security {
    // access qualifiers
    public static class UserAuth {
        public static bool UserIsInSecurityGroup(string targetSid) {
            var wi = WindowsIdentity.GetCurrent();
            foreach (var sid in wi.Groups)
                if (sid.Value == targetSid)
                    return true;
            return false;
        }
    }
}
