using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CORE_BE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdracLogtoServer3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonVi_Users_CreatedBy",
                table: "DonVi");

            migrationBuilder.DropForeignKey(
                name: "FK_IdracLog_Users_CreatedBy",
                table: "IdracLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Menu_Roles_Users_CreatedBy",
                table: "Menu_Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Menus_Users_CreatedBy",
                table: "Menus");

            migrationBuilder.DropForeignKey(
                name: "FK_Server_Users_CreatedBy",
                table: "Server");

            migrationBuilder.DropIndex(
                name: "IX_Server_CreatedBy",
                table: "Server");

            migrationBuilder.DropIndex(
                name: "IX_Menus_CreatedBy",
                table: "Menus");

            migrationBuilder.DropIndex(
                name: "IX_Menu_Roles_CreatedBy",
                table: "Menu_Roles");

            migrationBuilder.DropIndex(
                name: "IX_IdracLog_CreatedBy",
                table: "IdracLog");

            migrationBuilder.DropIndex(
                name: "IX_DonVi_CreatedBy",
                table: "DonVi");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "Server",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "Menus",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "Menu_Roles",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "IdracLog",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "DonVi",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "Server",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "Menus",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "Menu_Roles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "IdracLog",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                table: "DonVi",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Server_CreatedBy",
                table: "Server",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_CreatedBy",
                table: "Menus",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Roles_CreatedBy",
                table: "Menu_Roles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_IdracLog_CreatedBy",
                table: "IdracLog",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DonVi_CreatedBy",
                table: "DonVi",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_DonVi_Users_CreatedBy",
                table: "DonVi",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IdracLog_Users_CreatedBy",
                table: "IdracLog",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Menu_Roles_Users_CreatedBy",
                table: "Menu_Roles",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Menus_Users_CreatedBy",
                table: "Menus",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Server_Users_CreatedBy",
                table: "Server",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
