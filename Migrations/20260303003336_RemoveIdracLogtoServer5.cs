using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CORE_BE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdracLogtoServer5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServerId1",
                table: "IdracLog",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdracLog_ServerId1",
                table: "IdracLog",
                column: "ServerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_IdracLog_Server_ServerId1",
                table: "IdracLog",
                column: "ServerId1",
                principalTable: "Server",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdracLog_Server_ServerId1",
                table: "IdracLog");

            migrationBuilder.DropIndex(
                name: "IX_IdracLog_ServerId1",
                table: "IdracLog");

            migrationBuilder.DropColumn(
                name: "ServerId1",
                table: "IdracLog");
        }
    }
}
