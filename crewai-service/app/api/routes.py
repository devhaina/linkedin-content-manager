"""The one real endpoint the .NET backend calls: run the five-agent pipeline synchronously
and return every stage's structured output plus the final publishing brief.
"""

import logging
import uuid

from fastapi import APIRouter, Depends, HTTPException, status

from app.api.dependencies import get_or_create_correlation_id, require_service_api_key
from app.crew.linkedin_content_crew import LinkedInContentCrew
from app.models.schemas import (
    ContentReviewOutput,
    DraftOutput,
    ExecutionResponse,
    ExecutionStatus,
    GenerateContentRequest,
    OptimizationOutput,
    PERSONA_VOICE_GUIDE,
    ResearchBriefOutput,
    SchedulingOutput,
)

logger = logging.getLogger("crewai_service")

router = APIRouter(prefix="/api/v1", tags=["content"])


@router.post("/execute", response_model=ExecutionResponse, dependencies=[Depends(require_service_api_key)])
def execute_pipeline(
    request: GenerateContentRequest,
    correlation_id: str = Depends(get_or_create_correlation_id),
) -> ExecutionResponse:
    # Deliberately a sync `def`: FastAPI runs sync route handlers in a worker thread with
    # no event loop bound to that thread. CrewAI's Crew.kickoff() refuses to run
    # synchronously from inside a running event loop (it demands kickoff_async there), so
    # this must NOT be `async def` even though every other route in this file is.
    execution_id = str(uuid.uuid4())
    logger.info(
        "execution_started",
        extra={"execution_id": execution_id, "correlation_id": correlation_id, "niche": request.niche},
    )

    try:
        crew = LinkedInContentCrew()
        result = crew.kickoff(
            topic=request.topic,
            niche=request.niche,
            persona=f"{request.persona.value} ({PERSONA_VOICE_GUIDE[request.persona]})",
        )
    except Exception as exc:  # noqa: BLE001 — deliberately broad: any agent/task failure must surface, never be swallowed into a fake success
        logger.exception("execution_failed", extra={"execution_id": execution_id, "correlation_id": correlation_id})
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"CrewAI pipeline execution failed (execution_id={execution_id}): {_sanitize(str(exc))}",
        ) from exc

    outputs_by_index = {i: task_output for i, task_output in enumerate(result.tasks_output)}

    research = _pydantic_or_none(outputs_by_index.get(0), ResearchBriefOutput)
    writing = _pydantic_or_none(outputs_by_index.get(1), DraftOutput)
    critique = _pydantic_or_none(outputs_by_index.get(2), ContentReviewOutput)
    optimization = _pydantic_or_none(outputs_by_index.get(3), OptimizationOutput)
    scheduling = _pydantic_or_none(outputs_by_index.get(4), SchedulingOutput)

    logger.info("execution_completed", extra={"execution_id": execution_id, "correlation_id": correlation_id})

    return ExecutionResponse(
        execution_id=execution_id,
        status=ExecutionStatus.COMPLETED,
        research=research,
        writing=writing,
        critique=critique,
        optimization=optimization,
        scheduling=scheduling,
        final_publishing_brief=scheduling.final_post if scheduling else None,
    )


def _pydantic_or_none(task_output, model_type):
    if task_output is None or task_output.pydantic is None:
        return None
    parsed = task_output.pydantic
    return parsed if isinstance(parsed, model_type) else None


def _sanitize(message: str) -> str:
    """Strips anything that looks like an API key before it can reach an HTTP response."""
    import re

    return re.sub(r"sk-[A-Za-z0-9_\-]{10,}", "[redacted]", message)
