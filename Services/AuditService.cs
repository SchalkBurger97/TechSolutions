using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Microsoft.AspNet.Identity;
using TechSolutions.Models;

namespace TechSolutions.Services
{
    public class AuditService : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public AuditService()
        {
            _context = new ApplicationDbContext();
        }

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Core log method ──
        public void Log(
            string userId,
            string userName,
            string userRole,
            string action,
            string entityType,
            int? entityId,
            string entityDescription,
            string summary,
            string fieldChanges = null,
            HttpRequestBase request = null)
        {
            var entry = new AuditLog
            {
                UserID = userId,
                UserName = userName,
                UserRole = userRole,
                Action = action,
                EntityType = entityType,
                EntityID = entityId,
                EntityDescription = entityDescription,
                Summary = summary,
                FieldChanges = fieldChanges,
                IPAddress = request?.UserHostAddress,
                UserAgent = request?.UserAgent?.Length > 500
                                    ? request.UserAgent.Substring(0, 500)
                                    : request?.UserAgent,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(entry);
            _context.SaveChanges();
        }

        // ── Convenience overload used from controllers ──
        public void Log(
            System.Web.Mvc.Controller controller,
            string action,
            string entityType,
            int? entityId,
            string entityDescription,
            string summary,
            string fieldChanges = null)
        {
            var userId = controller.User?.Identity?.GetUserId() ?? "unknown";
            var userName = controller.User?.Identity?.Name ?? "unknown";
            var userRole = GetPrimaryRole(controller);

            Log(userId, userName, userRole, action, entityType,
                entityId, entityDescription, summary, fieldChanges,
                controller.Request);
        }

        // ── Build field changes string by comparing two objects ──
        public string BuildFieldChanges(Dictionary<string, (string OldValue, string NewValue)> changes)
        {
            if (changes == null || changes.Count == 0) return null;

            var sb = new StringBuilder();
            foreach (var kvp in changes)
            {
                if (kvp.Value.OldValue != kvp.Value.NewValue)
                {
                    sb.Append($"{kvp.Key}: '{kvp.Value.OldValue ?? "—"}' → '{kvp.Value.NewValue ?? "—"}'; ");
                }
            }

            return sb.Length > 0 ? sb.ToString().TrimEnd(' ', ';') : null;
        }

        private string GetPrimaryRole(System.Web.Mvc.Controller controller)
        {
            if (controller.User == null) return "Unknown";
            if (controller.User.IsInRole("Admin")) return "Admin";
            if (controller.User.IsInRole("Auditor")) return "Auditor";
            if (controller.User.IsInRole("Employee")) return "Employee";
            return "Unknown";
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
