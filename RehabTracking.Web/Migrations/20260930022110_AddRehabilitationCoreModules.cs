using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RehabTracking.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRehabilitationCoreModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    BadgeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    XPBonus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.BadgeId);
                });

            migrationBuilder.CreateTable(
                name: "ElectronicHealthRecords",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorInChargeId = table.Column<int>(type: "int", nullable: true),
                    InitialDiagnosis = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AffectedAnatomy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosticImagingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedicationScheduleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contraindications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TreatmentGoals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicHealthRecords", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_ElectronicHealthRecords_PatientProfiles_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PatientProfiles",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ElectronicHealthRecords_Users_DoctorInChargeId",
                        column: x => x.DoctorInChargeId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    ExerciseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecoveryPhase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    RecommendedPainMax = table.Column<int>(type: "int", nullable: false),
                    DefaultDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    DefaultSets = table.Column<int>(type: "int", nullable: false),
                    DefaultReps = table.Column<int>(type: "int", nullable: false),
                    HoldSeconds = table.Column<int>(type: "int", nullable: false),
                    RestSeconds = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepInstructionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommonMistakesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedFlagWarnings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TherapeuticBenefits = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.ExerciseId);
                });

            migrationBuilder.CreateTable(
                name: "GamificationProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TotalXP = table.Column<int>(type: "int", nullable: false),
                    CurrentTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    LongestStreak = table.Column<int>(type: "int", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StreakShieldCount = table.Column<int>(type: "int", nullable: false),
                    IsAnonymousLeaderboard = table.Column<bool>(type: "bit", nullable: false),
                    AnonymousDisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationProfiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_GamificationProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionArticles",
                columns: table => new
                {
                    ArticleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ComorbidityTags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorDoctor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionArticles", x => x.ArticleId);
                });

            migrationBuilder.CreateTable(
                name: "UserBadges",
                columns: table => new
                {
                    UserBadgeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BadgeId = table.Column<int>(type: "int", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBadges", x => x.UserBadgeId);
                    table.ForeignKey(
                        name: "FK_UserBadges_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "BadgeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBadges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: true),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    PainScoreVAS = table.Column<int>(type: "int", nullable: false),
                    ROMMeasurement = table.Column<double>(type: "float", nullable: true),
                    FatigueLevel = table.Column<int>(type: "int", nullable: false),
                    CompletionRate = table.Column<double>(type: "float", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    PatientNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MediaUrlsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoctorFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_RecoveryLogs_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecoveryLogs_PatientProfiles_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PatientProfiles",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecoveryLogs_Users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Badges_Code",
                table: "Badges",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicHealthRecords_DoctorInChargeId",
                table: "ElectronicHealthRecords",
                column: "DoctorInChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicHealthRecords_PatientId",
                table: "ElectronicHealthRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_GamificationProfiles_UserId",
                table: "GamificationProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_DoctorId",
                table: "RecoveryLogs",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_ExerciseId",
                table: "RecoveryLogs",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_PatientId",
                table: "RecoveryLogs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_BadgeId",
                table: "UserBadges",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId",
                table: "UserBadges",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectronicHealthRecords");

            migrationBuilder.DropTable(
                name: "GamificationProfiles");

            migrationBuilder.DropTable(
                name: "NutritionArticles");

            migrationBuilder.DropTable(
                name: "RecoveryLogs");

            migrationBuilder.DropTable(
                name: "UserBadges");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "Badges");
        }
    }
}
