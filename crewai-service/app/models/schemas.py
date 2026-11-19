"""Pydantic schemas: per-task structured outputs (enforced via CrewAI's
`output_pydantic`, not regex-parsed free text) plus the request/response
envelope the FastAPI layer exposes to the .NET backend.
"""

from __future__ import annotations

from enum import Enum

from pydantic import BaseModel, Field


class WriterPersona(str, Enum):
    """Mirrors LinkedInContentManager.Domain.Enums.WriterPersona exactly (same names)."""

    EXECUTIVE_THOUGHT_LEADERSHIP = "ExecutiveThoughtLeadership"
    PRACTITIONER = "Practitioner"
    CONVERSATIONAL_APPROACHABLE = "ConversationalApproachable"
    FORMAL_AUTHORITATIVE = "FormalAuthoritative"


PERSONA_VOICE_GUIDE: dict[WriterPersona, str] = {
    WriterPersona.EXECUTIVE_THOUGHT_LEADERSHIP: "First-person, strategic. Executive / thought leadership.",
    WriterPersona.PRACTITIONER: "Hands-on, technical, example-driven.",
    WriterPersona.CONVERSATIONAL_APPROACHABLE: "Short sentences, warm. Conversational and approachable.",
    WriterPersona.FORMAL_AUTHORITATIVE: "Polished, measured. Formal and authoritative.",
}


class GenerateContentRequest(BaseModel):
    topic: str = Field(..., min_length=3, max_length=500)
    niche: str = Field(..., min_length=2, max_length=100, description="Niche name, e.g. 'Fintech Security'.")
    persona: WriterPersona
    correlation_id: str | None = Field(default=None, description="Propagated from the .NET caller for log correlation.")


class ResearchBriefOutput(BaseModel):
    trending_content_angles: list[str] = Field(..., min_length=3, max_length=5)
    trending_hashtags: list[str]
    trending_hooks: list[str]
    source_urls: list[str] = Field(..., description="Every URL backing a statistic/claim used anywhere downstream.")
    summary: str = Field(..., description="Free-text research brief / industry developments narrative.")


class DraftOutput(BaseModel):
    draft_content: str
    word_count: int
    suggested_hashtags: list[str]


class ContentReviewOutput(BaseModel):
    score: int = Field(..., ge=0, le=10)
    hook_evaluation: str
    storytelling_evaluation: str
    engagement_potential: str
    cta_effectiveness: str
    tone_readability_assessment: str = Field(..., description="Must explicitly address fit with the selected persona.")
    guardrail_violations: list[str] = Field(default_factory=list)
    recommendations: list[str]


class OptimizationOutput(BaseModel):
    optimized_content: str
    word_count: int
    optimized_hashtags: list[str]


class SchedulingOutput(BaseModel):
    recommended_day: str
    recommended_time: str
    audience_timezone: str
    final_post: str = Field(..., description="Copy-paste-ready final content, hashtags included.")
    hashtag_strategy: str
    first_hour_engagement_plan: str


class ExecutionStatus(str, Enum):
    COMPLETED = "Completed"
    FAILED = "Failed"


class ExecutionResponse(BaseModel):
    execution_id: str
    status: ExecutionStatus
    research: ResearchBriefOutput | None = None
    writing: DraftOutput | None = None
    critique: ContentReviewOutput | None = None
    optimization: OptimizationOutput | None = None
    scheduling: SchedulingOutput | None = None
    final_publishing_brief: str | None = None
    error: str | None = Field(default=None, description="Sanitized error summary — never a raw stack trace or secret.")
