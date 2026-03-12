using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CORE_BE.Migrations
{
    /// <inheritdoc />
    public partial class ModifyTableStatusModule4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatusModule_Server_ServerId",
                table: "StatusModule");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_StatusModule_Server_ServerId1",
            //     table: "StatusModule");

            // migrationBuilder.DropIndex(
            //     name: "IX_StatusModule_ServerId1",
            //     table: "StatusModule");

            // migrationBuilder.DropColumn(
            //     name: "ServerId1",
            //     table: "StatusModule");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusModule_Server_ServerId",
                table: "StatusModule",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatusModule_Server_ServerId",
                table: "StatusModule");

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
                name: "FK_StatusModule_Server_ServerId",
                table: "StatusModule",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StatusModule_Server_ServerId1",
                table: "StatusModule",
                column: "ServerId1",
                principalTable: "Server",
                principalColumn: "Id");
        }
    }
}
