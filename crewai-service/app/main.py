"""FastAPI entrypoint for the CrewAI microservice.

Run with: uvicorn app.main:app --host 0.0.0.0 --port 8000
"""

import logging

from fastapi import FastAPI, Request, status
from fastapi.responses import JSONResponse

from app.api.routes import router
from app.core.config import get_settings

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
logger = logging.getLogger("crewai_service")

app = FastAPI(
    title="LinkedIn Content Manager — CrewAI Service",
    description="Runs the five-agent sequential content pipeline (Research → Writing → Critique → Optimization → Scheduling).",
    version="1.0.0",
)

app.include_router(router)


@app.get("/health")
async def health() -> dict:
    """Liveness/readiness probe. Confirms OPENAI_API_KEY is configured without ever echoing it."""
    settings = get_settings()
    return {
        "status": "healthy",
        "openai_configured": bool(settings.openai_api_key),
        "serper_configured": bool(settings.serper_api_key),
    }


@app.exception_handler(Exception)
async def unhandled_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    """Last-resort handler: logs the full exception server-side, returns a generic,
    secret-free message to the caller. Never leak a stack trace or API key in a response.
    """
    logger.exception("unhandled_exception", extra={"path": str(request.url)})
    return JSONResponse(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        content={"detail": "An internal error occurred while processing the request."},
    )
