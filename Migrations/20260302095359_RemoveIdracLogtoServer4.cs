using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CORE_BE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdracLogtoServer4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalLogId",
                table: "IdracLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalLogId",
                table: "IdracLog");
        }
    }
}
