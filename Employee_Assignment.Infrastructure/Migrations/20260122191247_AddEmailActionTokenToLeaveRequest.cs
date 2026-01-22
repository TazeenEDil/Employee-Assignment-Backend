using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Employee_Assignment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailActionTokenToLeaveRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EmailActionToken",
                table: "LeaveRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EmailActionToken",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
