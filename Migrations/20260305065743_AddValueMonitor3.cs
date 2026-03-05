using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CORE_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddValueMonitor3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServerId1",
                table: "StatusModule",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusModule_ServerId1",
                table: "StatusModule",
                column: "ServerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusModule_Server_ServerId1",
                table: "StatusModule",
                column: "ServerId1",
                principalTable: "Server",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatusModule_Server_ServerId1",
                table: "StatusModule");

            migrationBuilder.DropIndex(
                name: "IX_StatusModule_ServerId1",
                table: "StatusModule");

            migrationBuilder.DropColumn(
                name: "ServerId1",
                table: "StatusModule");
        }
    }
}
