using Microsoft.EntityFrameworkCore.Migrations;

namespace NetBoard.Model.Migrations
{
    public partial class shadowbanned : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_meta_posts",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_g_posts",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_diy_posts",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_meta_posts");

            migrationBuilder.DropColumn(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_g_posts");

            migrationBuilder.DropColumn(
                name: "shadow_banned",
                schema: "netboard",
                table: "net_diy_posts");
        }
    }
}
