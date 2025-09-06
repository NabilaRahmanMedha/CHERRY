using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cherry.AuthApi.Migrations
{
	public partial class AddCycleEntries : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "CycleEntries",
				columns: table => new
				{
					Id = table.Column<int>(type: "int", nullable: false)
						.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
					UserId = table.Column<string>(type: "varchar(255)", nullable: false)
						.Annotation("MySql:CharSet", "utf8mb4"),
					StartDate = table.Column<DateOnly>(type: "date", nullable: false),
					EndDate = table.Column<DateOnly>(type: "date", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CycleEntries", x => x.Id);
					table.ForeignKey(
						name: "FK_CycleEntries_AspNetUsers_UserId",
						column: x => x.UserId,
						principalTable: "AspNetUsers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				})
				.Annotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.CreateIndex(
				name: "IX_CycleEntries_UserId",
				table: "CycleEntries",
				column: "UserId");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "CycleEntries");
		}
	}
}


