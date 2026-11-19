using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LinkedInContentManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hashtags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hashtags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "niches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RefreshTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NicheId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultPersona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaigns_niches_NicheId",
                        column: x => x.NicheId,
                        principalTable: "niches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_campaigns_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "content_topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PersonaOverride = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_topics_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalExecutionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorDetails = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agent_executions_content_topics_ContentTopicId",
                        column: x => x.ContentTopicId,
                        principalTable: "content_topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_execution_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Agent = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TaskName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    TokenUsage = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_execution_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agent_execution_logs_agent_executions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "agent_executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "linkedin_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DraftContent = table.Column<string>(type: "text", nullable: false),
                    DraftWordCount = table.Column<int>(type: "integer", nullable: false),
                    OptimizedContent = table.Column<string>(type: "text", nullable: true),
                    OptimizedWordCount = table.Column<int>(type: "integer", nullable: true),
                    Stage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linkedin_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_linkedin_posts_agent_executions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "agent_executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "research_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrendingContentAngles = table.Column<string>(type: "text", nullable: false),
                    TrendingHashtags = table.Column<string>(type: "text", nullable: false),
                    TrendingHooks = table.Column<string>(type: "text", nullable: false),
                    SourceUrls = table.Column<string>(type: "text", nullable: false),
                    RawOutput = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_research_results_agent_executions_AgentExecutionId",
                        column: x => x.AgentExecutionId,
                        principalTable: "agent_executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedInPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_approvals_linkedin_posts_LinkedInPostId",
                        column: x => x.LinkedInPostId,
                        principalTable: "linkedin_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedInPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    HookEvaluation = table.Column<string>(type: "text", nullable: false),
                    StorytellingEvaluation = table.Column<string>(type: "text", nullable: false),
                    EngagementPotential = table.Column<string>(type: "text", nullable: false),
                    CtaEffectiveness = table.Column<string>(type: "text", nullable: false),
                    ToneReadabilityAssessment = table.Column<string>(type: "text", nullable: false),
                    GuardrailViolations = table.Column<string>(type: "text", nullable: false),
                    Recommendations = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_reviews_linkedin_posts_LinkedInPostId",
                        column: x => x.LinkedInPostId,
                        principalTable: "linkedin_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "linkedin_post_hashtags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedInPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    HashtagId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linkedin_post_hashtags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_linkedin_post_hashtags_hashtags_HashtagId",
                        column: x => x.HashtagId,
                        principalTable: "hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linkedin_post_hashtags_linkedin_posts_LinkedInPostId",
                        column: x => x.LinkedInPostId,
                        principalTable: "linkedin_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "publishing_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedInPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendedDay = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecommendedTime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AudienceTimezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HashtagStrategy = table.Column<string>(type: "text", nullable: false),
                    FirstHourEngagementPlan = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LinkedInExternalPostId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublishErrorDetails = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_publishing_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_publishing_schedules_linkedin_posts_LinkedInPostId",
                        column: x => x.LinkedInPostId,
                        principalTable: "linkedin_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "niches",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "Name", "Slug", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Financial technology: payments, banking infrastructure, lending, embedded finance.", true, "Fintech", "fintech", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Security and fraud prevention specific to financial technology platforms.", true, "Fintech Security", "fintech-security", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Blockchain protocols, decentralized infrastructure, and crypto-native tooling.", true, "Crypto", "crypto", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Digital asset markets, exchanges, tokens, and adoption trends.", true, "Cryptocurrency", "cryptocurrency", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "New developments in AI models, tooling, and enterprise adoption.", true, "Emerging AI Developments", "emerging-ai", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_execution_logs_Agent",
                table: "agent_execution_logs",
                column: "Agent");

            migrationBuilder.CreateIndex(
                name: "IX_agent_execution_logs_AgentExecutionId",
                table: "agent_execution_logs",
                column: "AgentExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_executions_ContentTopicId",
                table: "agent_executions",
                column: "ContentTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_executions_CorrelationId",
                table: "agent_executions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_executions_ExternalExecutionId",
                table: "agent_executions",
                column: "ExternalExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agent_executions_Status",
                table: "agent_executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_NicheId",
                table: "campaigns",
                column: "NicheId");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_OwnerUserId",
                table: "campaigns",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_Status",
                table: "campaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_content_approvals_LinkedInPostId",
                table: "content_approvals",
                column: "LinkedInPostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_approvals_Status",
                table: "content_approvals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_content_reviews_LinkedInPostId",
                table: "content_reviews",
                column: "LinkedInPostId");

            migrationBuilder.CreateIndex(
                name: "IX_content_reviews_Score",
                table: "content_reviews",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_content_topics_CampaignId",
                table: "content_topics",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_hashtags_Text",
                table: "hashtags",
                column: "Text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linkedin_post_hashtags_HashtagId",
                table: "linkedin_post_hashtags",
                column: "HashtagId");

            migrationBuilder.CreateIndex(
                name: "IX_linkedin_post_hashtags_LinkedInPostId_HashtagId",
                table: "linkedin_post_hashtags",
                columns: new[] { "LinkedInPostId", "HashtagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linkedin_posts_AgentExecutionId",
                table: "linkedin_posts",
                column: "AgentExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linkedin_posts_Stage",
                table: "linkedin_posts",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_niches_Slug",
                table: "niches",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_publishing_schedules_LinkedInExternalPostId",
                table: "publishing_schedules",
                column: "LinkedInExternalPostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_publishing_schedules_LinkedInPostId",
                table: "publishing_schedules",
                column: "LinkedInPostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_publishing_schedules_Status",
                table: "publishing_schedules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_research_results_AgentExecutionId",
                table: "research_results",
                column: "AgentExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_execution_logs");

            migrationBuilder.DropTable(
                name: "content_approvals");

            migrationBuilder.DropTable(
                name: "content_reviews");

            migrationBuilder.DropTable(
                name: "linkedin_post_hashtags");

            migrationBuilder.DropTable(
                name: "publishing_schedules");

            migrationBuilder.DropTable(
                name: "research_results");

            migrationBuilder.DropTable(
                name: "hashtags");

            migrationBuilder.DropTable(
                name: "linkedin_posts");

            migrationBuilder.DropTable(
                name: "agent_executions");

            migrationBuilder.DropTable(
                name: "content_topics");

            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropTable(
                name: "niches");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
