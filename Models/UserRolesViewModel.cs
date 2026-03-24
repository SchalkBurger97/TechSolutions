using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TechSolutions.Models
{
    public class UserRolesListViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Initials { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }

    public class AssignRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public List<RoleSelectionItem> Roles { get; set; } = new List<RoleSelectionItem>();
    }

    public class RoleSelectionItem
    {
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}