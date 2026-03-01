using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HoleriteSign.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_data = table.Column<string>(type: "jsonb", nullable: true),
                    actor_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actor_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    actor_user_agent = table.Column<string>(type: "text", nullable: true),
                    prev_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    entry_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    chain_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.CheckConstraint("chk_actor_type", "actor_type IN ('admin', 'employee', 'system')");
                });

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    max_documents = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    max_employees = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    price_monthly = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "admin"),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admins", x => x.id);
                    table.CheckConstraint("chk_admin_role", "role IN ('admin', 'superadmin')");
                    table.ForeignKey(
                        name: "FK_admins_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    whatsapp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cpf_encrypted = table.Column<byte[]>(type: "bytea", nullable: true),
                    cpf_last4 = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: true),
                    birth_date_encrypted = table.Column<byte[]>(type: "bytea", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.CheckConstraint("chk_contact", "email IS NOT NULL OR whatsapp IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_employees_admins_admin_id",
                        column: x => x.admin_id,
                        principalTable: "admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pay_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pay_periods", x => x.id);
                    table.CheckConstraint("chk_month", "month BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_pay_periods_admins_admin_id",
                        column: x => x.admin_id,
                        principalTable: "admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pay_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_filename = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_file_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    signed_file_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    signed_file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "uploaded"),
                    signing_token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    token_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.CheckConstraint("chk_document_status", "status IN ('uploaded', 'sent', 'signed', 'expired')");
                    table.ForeignKey(
                        name: "FK_documents_admins_admin_id",
                        column: x => x.admin_id,
                        principalTable: "admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_pay_periods_pay_period_id",
                        column: x => x.pay_period_id,
                        principalTable: "pay_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notifications_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "signatures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_file_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    photo_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    photo_mime_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    signer_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    signer_user_agent = table.Column<string>(type: "text", nullable: false),
                    signer_geolocation = table.Column<string>(type: "jsonb", nullable: true),
                    signer_device_info = table.Column<string>(type: "jsonb", nullable: true),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    consent_given = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    consent_text = table.Column<string>(type: "text", nullable: false),
                    verification_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    verification_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signatures", x => x.id);
                    table.CheckConstraint("chk_verification_method", "verification_method IN ('otp', 'cpf', 'dob')");
                    table.ForeignKey(
                        name: "FK_signatures_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_signatures_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "signing_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    otp_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    otp_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    attempt_window_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signing_verifications", x => x.id);
                    table.CheckConstraint("chk_verification_method", "method IN ('otp', 'cpf', 'dob')");
                    table.ForeignKey(
                        name: "FK_signing_verifications_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_signing_verifications_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "plans",
                columns: new[] { "id", "created_at", "display_name", "is_active", "max_documents", "max_employees", "name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plano Gratuito", true, 10, 5, "free" });

            migrationBuilder.InsertData(
                table: "plans",
                columns: new[] { "id", "created_at", "display_name", "is_active", "max_documents", "max_employees", "name", "price_monthly" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plano Básico", true, 50, 25, "basic", 49.90m },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plano Profissional", true, 200, 100, "pro", 99.90m },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plano Empresarial", true, -1, -1, "enterprise", 199.90m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_admins_email",
                table: "admins",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admins_plan_id",
                table: "admins",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_created",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_audit_document",
                table: "audit_logs",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_employee",
                table: "audit_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_event",
                table: "audit_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_documents_employee",
                table: "documents",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "idx_documents_period",
                table: "documents",
                column: "pay_period_id");

            migrationBuilder.CreateIndex(
                name: "idx_documents_status",
                table: "documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_documents_token",
                table: "documents",
                column: "signing_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_admin_id",
                table: "documents",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_employee_id_pay_period_id",
                table: "documents",
                columns: new[] { "employee_id", "pay_period_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_employees_admin",
                table: "employees",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_document",
                table: "notifications",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_employee_id",
                table: "notifications",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_pay_periods_admin_id_year_month",
                table: "pay_periods",
                columns: new[] { "admin_id", "year", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signatures_document_id",
                table: "signatures",
                column: "document_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signatures_employee_id",
                table: "signatures",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "idx_signing_verifications_active",
                table: "signing_verifications",
                column: "document_id",
                unique: true,
                filter: "verified = false");

            migrationBuilder.CreateIndex(
                name: "IX_signing_verifications_employee_id",
                table: "signing_verifications",
                column: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "signatures");

            migrationBuilder.DropTable(
                name: "signing_verifications");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "pay_periods");

            migrationBuilder.DropTable(
                name: "admins");

            migrationBuilder.DropTable(
                name: "plans");
        }
    }
}
