using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "UserHandle",
                table: "Users",
                type: "varbinary(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PasskeyCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "varbinary(1023)", maxLength: 1023, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "longblob", nullable: false),
                    SignCount = table.Column<uint>(type: "int unsigned", nullable: false),
                    Transports = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    IsBackupEligible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AaGuid = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasskeyCredentials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserHandle",
                table: "Users",
                column: "UserHandle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCredentials_CredentialId",
                table: "PasskeyCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyCredentials_UserId",
                table: "PasskeyCredentials",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasskeyCredentials");

            migrationBuilder.DropIndex(
                name: "IX_Users_UserHandle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserHandle",
                table: "Users");
        }
    }
}
