"""Shared-secret auth between the .NET backend and this service, plus correlation-id handling."""

import uuid

from fastapi import Header, HTTPException, status

from app.core.config import get_settings


async def require_service_api_key(x_service_api_key: str | None = Header(default=None)) -> None:
    settings = get_settings()
    if not settings.service_api_key:
        return  # unset/empty: open for local dev only — see CREWAI_SERVICE_API_KEY in .env.example
    if x_service_api_key != settings.service_api_key:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid or missing service API key.")


def get_or_create_correlation_id(x_correlation_id: str | None = Header(default=None)) -> str:
    return x_correlation_id or str(uuid.uuid4())
