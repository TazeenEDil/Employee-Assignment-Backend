using System.Net;
using System.Net.Mail;
using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendLeaveApprovedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("=== Sending Leave Approval Email ===");
                _logger.LogInformation("To: {Email}", toEmail);

                var subject = "Leave Request Approved ✓";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; background-color: #f9f9f9; }}
                            .details {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #4CAF50; }}
                            .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Leave Request Approved</h1>
                            </div>
                            <div class='content'>
                                <p>Dear {employeeName},</p>
                                <p>We are pleased to inform you that your leave request has been <strong>approved</strong>.</p>
                                
                                <div class='details'>
                                    <h3>Leave Details:</h3>
                                    <ul>
                                        <li><strong>Start Date:</strong> {startDate:dddd, MMMM dd, yyyy}</li>
                                        <li><strong>End Date:</strong> {endDate:dddd, MMMM dd, yyyy}</li>
                                        <li><strong>Duration:</strong> {(endDate - startDate).Days + 1} day(s)</li>
                                        <li><strong>Status:</strong> <span style='color: #4CAF50; font-weight: bold;'>Approved</span></li>
                                    </ul>
                                </div>
                                
                                <p>Please make sure to complete any pending tasks before your leave begins.</p>
                                <p>Have a great time off!</p>
                            </div>
                            <div class='footer'>
                                <p>Best regards,<br/>HR Department<br/>Employee Management System</p>
                                <p><em>This is an automated message. Please do not reply to this email.</em></p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                await SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("✓ Leave approval email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error sending leave approval email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendLeaveRejectedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate, string reason)
        {
            try
            {
                _logger.LogInformation("=== Sending Leave Rejection Email ===");
                _logger.LogInformation("To: {Email}", toEmail);

                var subject = "Leave Request Rejected";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; background-color: #f9f9f9; }}
                            .details {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #f44336; }}
                            .reason {{ background-color: #fff3cd; padding: 10px; border-radius: 4px; margin: 10px 0; }}
                            .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Leave Request Rejected</h1>
                            </div>
                            <div class='content'>
                                <p>Dear {employeeName},</p>
                                <p>We regret to inform you that your leave request has been <strong>rejected</strong>.</p>
                                
                                <div class='details'>
                                    <h3>Leave Details:</h3>
                                    <ul>
                                        <li><strong>Start Date:</strong> {startDate:dddd, MMMM dd, yyyy}</li>
                                        <li><strong>End Date:</strong> {endDate:dddd, MMMM dd, yyyy}</li>
                                        <li><strong>Duration:</strong> {(endDate - startDate).Days + 1} day(s)</li>
                                        <li><strong>Status:</strong> <span style='color: #f44336; font-weight: bold;'>Rejected</span></li>
                                    </ul>
                                    
                                    <div class='reason'>
                                        <strong>Reason for Rejection:</strong><br/>
                                        {reason}
                                    </div>
                                </div>
                                
                                <p>If you have any questions or would like to discuss this decision, please contact the HR department.</p>
                            </div>
                            <div class='footer'>
                                <p>Best regards,<br/>HR Department<br/>Employee Management System</p>
                                <p><em>This is an automated message. Please do not reply to this email.</em></p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                await SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("✓ Leave rejection email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error sending leave rejection email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendLeaveRequestForApprovalEmailAsync(string adminEmail, string employeeName, DateTime startDate, DateTime endDate, int leaveRequestId, string actionToken)
        {
            try
            {
                _logger.LogInformation("=== Sending Leave Request Notification to Admin ===");
                _logger.LogInformation("To: {Email}", adminEmail);

                var subject = $"New Leave Request from {employeeName}";

                // Create action links
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5224";
                var approveLink = $"{baseUrl}/api/leave/{leaveRequestId}/email-action?approve=true&token={actionToken}";
                var rejectLink = $"{baseUrl}/api/leave/{leaveRequestId}/email-action?approve=false&token={actionToken}";

                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; background-color: #f9f9f9; }}
                            .details {{ background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #2196F3; }}
                            .actions {{ text-align: center; margin: 30px 0; }}
                            .btn {{ display: inline-block; padding: 12px 30px; margin: 0 10px; text-decoration: none; border-radius: 4px; font-weight: bold; }}
                            .btn-approve {{ background-color: #4CAF50; color: white; }}
                            .btn-reject {{ background-color: #f44336; color: white; }}
                            .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>New Leave Request</h1>
                            </div>
                            <div class='content'>
                                <p>A new leave request requires your approval.</p>
                                
                                <div class='details'>
                                    <h3>Request Details:</h3>
                                    <ul>
                                        <li><strong>Employee:</strong> {employeeName}</li>
                                        <li><strong>Start Date:</strong> {startDate:dddd, MMMM dd, yyyy}</li>
                                        <li><strong>End Date:</strong> {endDate:dddd, MMMM dd, yyyy}</li>
                                        <li><strong>Duration:</strong> {(endDate - startDate).Days + 1} day(s)</li>
                                    </ul>
                                </div>
                                
                                <div class='actions'>
                                    <a href='{approveLink}' class='btn btn-approve'>✓ Approve</a>
                                    <a href='{rejectLink}' class='btn btn-reject'>✗ Reject</a>
                                </div>
                                
                                <p style='text-align: center; color: #666;'><em>Or log into the system to review the request.</em></p>
                            </div>
                            <div class='footer'>
                                <p>Employee Management System</p>
                                <p><em>This is an automated message. Please do not reply to this email.</em></p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                await SendEmailAsync(adminEmail, subject, body);
                _logger.LogInformation("✓ Leave request notification sent to admin successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Error sending leave request notification to admin {Email}", adminEmail);
                throw;
            }
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                var smtpHost = emailSettings["SmtpHost"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var fromEmail = emailSettings["FromEmail"];
                var fromName = emailSettings["FromName"];
                var username = emailSettings["SmtpUsername"];
                var password = emailSettings["SmtpPassword"];
                var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

                _logger.LogInformation("📧 Preparing to send email");
                _logger.LogInformation("   To: {ToEmail}", toEmail);
                _logger.LogInformation("   From: {FromEmail}", fromEmail);
                _logger.LogInformation("   SMTP: {SmtpHost}:{SmtpPort}", smtpHost, smtpPort);
                _logger.LogInformation("   SSL: {EnableSsl}", enableSsl);

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl,
                    Timeout = 30000 // 30 seconds timeout
                };

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation("✅ Email sent successfully to {ToEmail}", toEmail);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "❌ SMTP error sending email to {ToEmail}. StatusCode: {StatusCode}", toEmail, ex.StatusCode);
                _logger.LogError("   SMTP Message: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error sending email to {ToEmail}", toEmail);
                throw;
            }
        }
    }
}