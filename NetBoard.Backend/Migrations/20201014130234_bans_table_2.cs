using Microsoft.EntityFrameworkCore.Migrations;

namespace NetBoard.Model.Migrations
{
    public partial class bans_table_2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ip",
                schema: "netboard",
                table: "net_bans",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ip",
                schema: "netboard",
                table: "net_bans");
        }
    }
}
