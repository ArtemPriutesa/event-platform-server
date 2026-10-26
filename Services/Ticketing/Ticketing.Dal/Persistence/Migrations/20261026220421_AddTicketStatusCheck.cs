using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Dal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketStatusCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "tickets_status_check",
                table: "tickets",
                sql: "status IN ('VALID', 'USED', 'REFUNDED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "tickets_status_check",
                table: "tickets");
        }
    }
}
