using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CORE_BE.Migrations
{
    /// <inheritdoc />
    public partial class CodeReviewFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdracLog_Server_ServerId",
                table: "IdracLog");

            migrationBuilder.DropForeignKey(
                name: "FK_IdracLog_Server_ServerId1",
                table: "IdracLog");

            migrationBuilder.DropForeignKey(
                name: "FK_InfoServer_Server_ServerId",
                table: "InfoServer");

            migrationBuilder.DropForeignKey(
                name: "FK_Server_DonVi_DonVi_Id",
                table: "Server");

            migrationBuilder.DropForeignKey(
                name: "FK_StatusModule_Server_ServerId",
                table: "StatusModule");

            migrationBuilder.DropForeignKey(
                name: "FK_statusModuleHistory_Server_ServerId",
                table: "statusModuleHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_statusModuleHistory",
                table: "statusModuleHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StatusModule",
                table: "StatusModule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Server",
                table: "Server");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InfoServer",
                table: "InfoServer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IdracLog",
                table: "IdracLog");

            migrationBuilder.RenameTable(
                name: "statusModuleHistory",
                newName: "StatusModuleHistories");

            migrationBuilder.RenameTable(
                name: "StatusModule",
                newName: "StatusModules");

            migrationBuilder.RenameTable(
                name: "Server",
                newName: "Servers");

            migrationBuilder.RenameTable(
                name: "InfoServer",
                newName: "InfoServers");

            migrationBuilder.RenameTable(
                name: "IdracLog",
                newName: "IdracLogs");

            migrationBuilder.RenameIndex(
                name: "IX_statusModuleHistory_ServerId",
                table: "StatusModuleHistories",
                newName: "IX_StatusModuleHistories_ServerId");

            migrationBuilder.RenameIndex(
                name: "IX_StatusModule_ServerId",
                table: "StatusModules",
                newName: "IX_StatusModules_ServerId");

            migrationBuilder.RenameIndex(
                name: "IX_Server_DonVi_Id",
                table: "Servers",
                newName: "IX_Servers_DonVi_Id");

            migrationBuilder.RenameIndex(
                name: "IX_InfoServer_ServerId",
                table: "InfoServers",
                newName: "IX_InfoServers_ServerId");

            migrationBuilder.RenameIndex(
                name: "IX_IdracLog_ServerId1",
                table: "IdracLogs",
                newName: "IX_IdracLogs_ServerId1");

            migrationBuilder.RenameIndex(
                name: "IX_IdracLog_ServerId",
                table: "IdracLogs",
                newName: "IX_IdracLogs_ServerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StatusModuleHistories",
                table: "StatusModuleHistories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StatusModules",
                table: "StatusModules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Servers",
                table: "Servers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InfoServers",
                table: "InfoServers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdracLogs",
                table: "IdracLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IdracLogs_Servers_ServerId",
                table: "IdracLogs",
                column: "ServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdracLogs_Servers_ServerId1",
                table: "IdracLogs",
                column: "ServerId1",
                principalTable: "Servers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InfoServers_Servers_ServerId",
                table: "InfoServers",
                column: "ServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Servers_DonVi_DonVi_Id",
                table: "Servers",
                column: "DonVi_Id",
                principalTable: "DonVi",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusModuleHistories_Servers_ServerId",
                table: "StatusModuleHistories",
                column: "ServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StatusModules_Servers_ServerId",
                table: "StatusModules",
                column: "ServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IdracLogs_Servers_ServerId",
                table: "IdracLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_IdracLogs_Servers_ServerId1",
                table: "IdracLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_InfoServers_Servers_ServerId",
                table: "InfoServers");

            migrationBuilder.DropForeignKey(
                name: "FK_Servers_DonVi_DonVi_Id",
                table: "Servers");

            migrationBuilder.DropForeignKey(
                name: "FK_StatusModuleHistories_Servers_ServerId",
                table: "StatusModuleHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_StatusModules_Servers_ServerId",
                table: "StatusModules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StatusModules",
                table: "StatusModules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StatusModuleHistories",
                table: "StatusModuleHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Servers",
                table: "Servers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InfoServers",
                table: "InfoServers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IdracLogs",
                table: "IdracLogs");

            migrationBuilder.RenameTable(
                name: "StatusModules",
                newName: "StatusModule");

            migrationBuilder.RenameTable(
                name: "StatusModuleHistories",
                newName: "statusModuleHistory");

            migrationBuilder.RenameTable(
                name: "Servers",
                newName: "Server");

            migrationBuilder.RenameTable(
                name: "InfoServers",
                newName: "InfoServer");

            migrationBuilder.RenameTable(
                name: "IdracLogs",
                newName: "IdracLog");

            migrationBuilder.RenameIndex(
                name: "IX_StatusModules_ServerId",
                table: "StatusModule",
                newName: "IX_StatusModule_ServerId");

            migrationBuilder.RenameIndex(
                name: "IX_StatusModuleHistories_ServerId",
                table: "statusModuleHistory",
                newName: "IX_statusModuleHistory_ServerId");

            migrationBuilder.RenameIndex(
                name: "IX_Servers_DonVi_Id",
                table: "Server",
                newName: "IX_Server_DonVi_Id");

            migrationBuilder.RenameIndex(
                name: "IX_InfoServers_ServerId",
                table: "InfoServer",
                newName: "IX_InfoServer_ServerId");

            migrationBuilder.RenameIndex(
                name: "IX_IdracLogs_ServerId1",
                table: "IdracLog",
                newName: "IX_IdracLog_ServerId1");

            migrationBuilder.RenameIndex(
                name: "IX_IdracLogs_ServerId",
                table: "IdracLog",
                newName: "IX_IdracLog_ServerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StatusModule",
                table: "StatusModule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_statusModuleHistory",
                table: "statusModuleHistory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Server",
                table: "Server",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InfoServer",
                table: "InfoServer",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdracLog",
                table: "IdracLog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IdracLog_Server_ServerId",
                table: "IdracLog",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdracLog_Server_ServerId1",
                table: "IdracLog",
                column: "ServerId1",
                principalTable: "Server",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InfoServer_Server_ServerId",
                table: "InfoServer",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Server_DonVi_DonVi_Id",
                table: "Server",
                column: "DonVi_Id",
                principalTable: "DonVi",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusModule_Server_ServerId",
                table: "StatusModule",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_statusModuleHistory_Server_ServerId",
                table: "statusModuleHistory",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
