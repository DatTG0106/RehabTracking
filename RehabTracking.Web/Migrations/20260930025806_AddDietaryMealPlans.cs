using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RehabTracking.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDietaryMealPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DietaryMealPlans",
                columns: table => new
                {
                    MealPlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TargetCondition = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CaloriesTarget = table.Column<int>(type: "int", nullable: false),
                    ProteinGrams = table.Column<int>(type: "int", nullable: false),
                    CalciumMg = table.Column<int>(type: "int", nullable: false),
                    BreakfastMenu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LunchMenu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DinnerMenu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnacksMenu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorDoctor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietaryMealPlans", x => x.MealPlanId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DietaryMealPlans");
        }
    }
}
