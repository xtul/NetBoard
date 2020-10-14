using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace NetBoard.Model.Migrations
{
    public partial class bans_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_meta_posts",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_g_posts",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_diy_posts",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.CreateTable(
                name: "net_bans",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reason = table.Column<string>(nullable: true),
                    expires_on = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bans", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "net_bans",
                schema: "netboard");

            migrationBuilder.AlterColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_meta_posts",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_g_posts",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_diy_posts",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);
        }
    }
}
