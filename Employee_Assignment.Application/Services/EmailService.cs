using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Sends email to admin with approve/reject links
        /// </summary>
        public async Task SendLeaveRequestForApprovalEmailAsync(
            string adminEmail,
            string employeeName,
            DateTime startDate,
            DateTime endDate,
            int leaveRequestId,
            string actionToken)
        {
            // Get base URL from configuration or use default
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5224";

            var approveUrl = $"{baseUrl}/api/leave/{leaveRequestId}/email-action?approve=true&token={actionToken}";
            var rejectUrl = $"{baseUrl}/api/leave/{leaveRequestId}/email-action?approve=false&token={actionToken}";

            // Log email details (in production, replace with actual email sending)
            _logger.LogInformation(@"
================================================================================
📧 EMAIL: Leave Approval Required
================================================================================
To: {AdminEmail}
Subject: Leave Request Approval Needed

Dear Admin,

{EmployeeName} has requested leave from {StartDate} to {EndDate}.

Please review and take action:

✅ APPROVE: {ApproveUrl}

❌ REJECT: {RejectUrl}

Note: These links will expire once the request is processed.

Regards,
HR Management System
================================================================================
",
                adminEmail,
                employeeName,
                startDate.ToString("MMMM dd, yyyy"),
                endDate.ToString("MMMM dd, yyyy"),
                approveUrl,
                rejectUrl
            );

            // TODO: In production, integrate with actual email service:
            // - SendGrid
            // - AWS SES
            // - SMTP
            // Example:
            // await _emailClient.SendEmailAsync(adminEmail, subject, body);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Sends approval confirmation email to employee
        /// </summary>
        public async Task SendLeaveApprovedEmailAsync(
            string employeeEmail,
            string employeeName,
            DateTime startDate,
            DateTime endDate)
        {
            _logger.LogInformation(@"
================================================================================
📧 EMAIL: Leave Request Approved
================================================================================
To: {Email}
Subject: Your Leave Request Has Been Approved

Dear {Name},

Your leave request has been APPROVED.

Details:
- From: {StartDate}
- To: {EndDate}
- Status: Approved
- Approved Date: {ApprovedDate}

Enjoy your time off!

Regards,
HR Management System
================================================================================
",
                employeeEmail,
                employeeName,
                startDate.ToString("MMMM dd, yyyy"),
                endDate.ToString("MMMM dd, yyyy"),
                DateTime.Now.ToString("MMMM dd, yyyy")
            );

            await Task.CompletedTask;
        }

        /// <summary>
        /// Sends rejection notification email to employee
        /// </summary>
        public async Task SendLeaveRejectedEmailAsync(
            string employeeEmail,
            string employeeName,
            DateTime startDate,
            DateTime endDate,
            string reason)
        {
            _logger.LogInformation(@"
================================================================================
📧 EMAIL: Leave Request Rejected
================================================================================
To: {Email}
Subject: Your Leave Request Has Been Rejected

Dear {Name},

Unfortunately, your leave request has been REJECTED.

Details:
- From: {StartDate}
- To: {EndDate}
- Status: Rejected
- Rejected Date: {RejectedDate}
- Reason: {Reason}

Please contact HR if you have any questions.

Regards,
HR Management System
================================================================================
",
                employeeEmail,
                employeeName,
                startDate.ToString("MMMM dd, yyyy"),
                endDate.ToString("MMMM dd, yyyy"),
                DateTime.Now.ToString("MMMM dd, yyyy"),
                reason
            );

            await Task.CompletedTask;
        }
    }
}