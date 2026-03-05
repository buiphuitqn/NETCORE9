using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CORE_BE.Migrations
{
    /// <inheritdoc />
    public partial class StatusModule3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatusModuleHistory_Server_ServerId",
                table: "StatusModuleHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StatusModuleHistory",
                table: "StatusModuleHistory");

            migrationBuilder.RenameTable(
                name: "StatusModuleHistory",
                newName: "statusModuleHistory");

            migrationBuilder.RenameIndex(
                name: "IX_StatusModuleHistory_ServerId",
                table: "statusModuleHistory",
                newName: "IX_statusModuleHistory_ServerId");

            migrationBuilder.AddColumn<Guid>(
                name: "DonVi_Id",
                table: "Server",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_statusModuleHistory",
                table: "statusModuleHistory",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Server_DonVi_Id",
                table: "Server",
                column: "DonVi_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Server_DonVi_DonVi_Id",
                table: "Server",
                column: "DonVi_Id",
                principalTable: "DonVi",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_statusModuleHistory_Server_ServerId",
                table: "statusModuleHistory",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Server_DonVi_DonVi_Id",
                table: "Server");

            migrationBuilder.DropForeignKey(
                name: "FK_statusModuleHistory_Server_ServerId",
                table: "statusModuleHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_statusModuleHistory",
                table: "statusModuleHistory");

            migrationBuilder.DropIndex(
                name: "IX_Server_DonVi_Id",
                table: "Server");

            migrationBuilder.DropColumn(
                name: "DonVi_Id",
                table: "Server");

            migrationBuilder.RenameTable(
                name: "statusModuleHistory",
                newName: "StatusModuleHistory");

            migrationBuilder.RenameIndex(
                name: "IX_statusModuleHistory_ServerId",
                table: "StatusModuleHistory",
                newName: "IX_StatusModuleHistory_ServerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StatusModuleHistory",
                table: "StatusModuleHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusModuleHistory_Server_ServerId",
                table: "StatusModuleHistory",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
