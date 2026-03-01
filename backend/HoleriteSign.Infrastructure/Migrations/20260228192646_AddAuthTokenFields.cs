using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoleriteSign.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthTokenFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "email_verification_expires_at",
                table: "admins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email_verification_token",
                table: "admins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_expires_at",
                table: "admins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_token",
                table: "admins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token",
                table: "admins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "refresh_token_expires_at",
                table: "admins",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_verification_expires_at",
                table: "admins");

            migrationBuilder.DropColumn(
                name: "email_verification_token",
                table: "admins");

            migrationBuilder.DropColumn(
                name: "password_reset_expires_at",
                table: "admins");

            migrationBuilder.DropColumn(
                name: "password_reset_token",
                table: "admins");

            migrationBuilder.DropColumn(
                name: "refresh_token",
                table: "admins");

            migrationBuilder.DropColumn(
                name: "refresh_token_expires_at",
                table: "admins");
        }
    }
}
