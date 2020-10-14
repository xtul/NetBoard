using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace NetBoard.Model.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "netboard");

            migrationBuilder.CreateTable(
                name: "net_diy_posts",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image = table.Column<string>(nullable: true),
                    content = table.Column<string>(maxLength: 4000, nullable: false),
                    name = table.Column<string>(maxLength: 32, nullable: true, defaultValue: "Anonymous"),
                    password = table.Column<string>(maxLength: 64, nullable: true),
                    posted_on = table.Column<DateTime>(nullable: false),
                    spoiler_image = table.Column<bool>(nullable: true, defaultValue: false),
                    subject = table.Column<string>(maxLength: 64, nullable: true),
                    archived = table.Column<bool>(nullable: false, defaultValue: false),
                    poster_level = table.Column<int>(nullable: false),
                    thread = table.Column<int>(nullable: true),
                    sticky = table.Column<bool>(nullable: false, defaultValue: false),
                    last_post_date = table.Column<DateTime>(nullable: false),
                    poster_ip = table.Column<string>(nullable: true),
                    past_limits = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diy_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_frontpage_data",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    about = table.Column<string>(nullable: true),
                    news = table.Column<string>(nullable: true),
                    boards_json = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_frontpage_data", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_g_posts",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image = table.Column<string>(nullable: true),
                    content = table.Column<string>(maxLength: 4000, nullable: false),
                    name = table.Column<string>(maxLength: 32, nullable: true, defaultValue: "Anonymous"),
                    password = table.Column<string>(maxLength: 64, nullable: true),
                    posted_on = table.Column<DateTime>(nullable: false),
                    spoiler_image = table.Column<bool>(nullable: true, defaultValue: false),
                    subject = table.Column<string>(maxLength: 64, nullable: true),
                    archived = table.Column<bool>(nullable: false, defaultValue: false),
                    poster_level = table.Column<int>(nullable: false),
                    thread = table.Column<int>(nullable: true),
                    sticky = table.Column<bool>(nullable: false, defaultValue: false),
                    last_post_date = table.Column<DateTime>(nullable: false),
                    poster_ip = table.Column<string>(nullable: true),
                    past_limits = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_g_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_image_queue",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(nullable: true),
                    expires_on = table.Column<DateTime>(nullable: false),
                    filename = table.Column<string>(nullable: true),
                    assigned_post = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_image_queue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_marked_for_deletion",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    utc_deleted_on = table.Column<DateTime>(nullable: false),
                    post_id = table.Column<int>(nullable: false),
                    board = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_marked_for_deletion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_meta_posts",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image = table.Column<string>(nullable: true),
                    content = table.Column<string>(maxLength: 4000, nullable: false),
                    name = table.Column<string>(maxLength: 32, nullable: true, defaultValue: "Anonymous"),
                    password = table.Column<string>(maxLength: 64, nullable: true),
                    posted_on = table.Column<DateTime>(nullable: false),
                    spoiler_image = table.Column<bool>(nullable: true, defaultValue: false),
                    subject = table.Column<string>(maxLength: 64, nullable: true),
                    archived = table.Column<bool>(nullable: false, defaultValue: false),
                    poster_level = table.Column<int>(nullable: false),
                    thread = table.Column<int>(nullable: true),
                    sticky = table.Column<bool>(nullable: false, defaultValue: false),
                    last_post_date = table.Column<DateTime>(nullable: false),
                    poster_ip = table.Column<string>(nullable: true),
                    past_limits = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meta_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_reports",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateTime>(nullable: false),
                    reporting_ip = table.Column<string>(nullable: true),
                    post_board = table.Column<string>(nullable: true),
                    post_id = table.Column<int>(nullable: false),
                    reason = table.Column<string>(maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_roles",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_sages",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    board = table.Column<string>(nullable: true),
                    topic_id = table.Column<int>(nullable: false),
                    saged_on = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_users",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_name = table.Column<string>(maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(maxLength: 256, nullable: true),
                    email = table.Column<string>(maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(nullable: false),
                    password_hash = table.Column<string>(nullable: true),
                    security_stamp = table.Column<string>(nullable: true),
                    concurrency_stamp = table.Column<string>(nullable: true),
                    phone_number = table.Column<string>(nullable: true),
                    phone_number_confirmed = table.Column<bool>(nullable: false),
                    two_factor_enabled = table.Column<bool>(nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(nullable: true),
                    lockout_enabled = table.Column<bool>(nullable: false),
                    access_failed_count = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "net_roleclaims",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(nullable: false),
                    claim_type = table.Column<string>(nullable: true),
                    claim_value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_asp_net_roles_identity_role_int_id",
                        column: x => x.role_id,
                        principalSchema: "netboard",
                        principalTable: "net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "net_userclaims",
                schema: "netboard",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(nullable: false),
                    claim_type = table.Column<string>(nullable: true),
                    claim_value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_asp_net_users_application_user_id",
                        column: x => x.user_id,
                        principalSchema: "netboard",
                        principalTable: "net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "net_userlogins",
                schema: "netboard",
                columns: table => new
                {
                    login_provider = table.Column<string>(nullable: false),
                    provider_key = table.Column<string>(nullable: false),
                    provider_display_name = table.Column<string>(nullable: true),
                    user_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_asp_net_users_application_user_id",
                        column: x => x.user_id,
                        principalSchema: "netboard",
                        principalTable: "net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "net_userroles",
                schema: "netboard",
                columns: table => new
                {
                    user_id = table.Column<int>(nullable: false),
                    role_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_asp_net_roles_identity_role_int_id",
                        column: x => x.role_id,
                        principalSchema: "netboard",
                        principalTable: "net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_asp_net_users_application_user_id",
                        column: x => x.user_id,
                        principalSchema: "netboard",
                        principalTable: "net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "net_usertokens",
                schema: "netboard",
                columns: table => new
                {
                    user_id = table.Column<int>(nullable: false),
                    login_provider = table.Column<string>(nullable: false),
                    name = table.Column<string>(nullable: false),
                    value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_asp_net_users_application_user_id",
                        column: x => x.user_id,
                        principalSchema: "netboard",
                        principalTable: "net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                schema: "netboard",
                table: "net_roleclaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "netboard",
                table: "net_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                schema: "netboard",
                table: "net_userclaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                schema: "netboard",
                table: "net_userlogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "netboard",
                table: "net_userroles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "netboard",
                table: "net_users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "netboard",
                table: "net_users",
                column: "normalized_user_name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "net_diy_posts",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_frontpage_data",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_g_posts",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_image_queue",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_marked_for_deletion",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_meta_posts",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_reports",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_roleclaims",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_sages",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_userclaims",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_userlogins",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_userroles",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_usertokens",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_roles",
                schema: "netboard");

            migrationBuilder.DropTable(
                name: "net_users",
                schema: "netboard");
        }
    }
}
