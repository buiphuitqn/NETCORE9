using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CORE_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddValueMonitor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValueMonitor",
                table: "statusModuleHistory",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueMonitor",
                table: "statusModuleHistory");
        }
    }
}
