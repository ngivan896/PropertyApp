using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    owner_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    address_line = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    unit_label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    property_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_properties", x => x.id);
                    table.ForeignKey(
                        name: "FK_properties_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    owner_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_records_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_records_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    owner_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    assigned_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    property_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_tickets", x => x.id);
                    table.ForeignKey(
                        name: "FK_repair_tickets_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_repair_tickets_users_assigned_user_id",
                        column: x => x.assigned_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_tickets_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_owner_id",
                table: "payment_records",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_property_id",
                table: "payment_records",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_properties_owner_id",
                table: "properties",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_tickets_assigned_user_id",
                table: "repair_tickets",
                column: "assigned_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_tickets_owner_id",
                table: "repair_tickets",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_tickets_property_id",
                table: "repair_tickets",
                column: "property_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_records");

            migrationBuilder.DropTable(
                name: "repair_tickets");

            migrationBuilder.DropTable(
                name: "properties");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
