namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendLeaveRequestForApprovalEmailAsync(
            string adminEmail,
            string employeeName,
            DateTime startDate,
            DateTime endDate,
            int leaveRequestId,
            string actionToken
        );

        Task SendLeaveApprovedEmailAsync(
            string employeeEmail,
            string employeeName,
            DateTime startDate,
            DateTime endDate
        );

        Task SendLeaveRejectedEmailAsync(
            string employeeEmail,
            string employeeName,
            DateTime startDate,
            DateTime endDate,
            string reason
        );
    }
}
