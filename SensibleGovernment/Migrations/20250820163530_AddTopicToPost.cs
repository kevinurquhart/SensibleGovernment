using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SensibleGovernment.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Topic",
                table: "Posts");
        }
    }
}
