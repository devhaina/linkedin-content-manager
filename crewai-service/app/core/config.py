"""Strongly-typed settings loaded from the environment / .env file.

Never logged, never returned in an API response — see app/api/errors.py for
the exception handler that keeps secrets out of error payloads.
"""

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    openai_api_key: str = Field(..., alias="OPENAI_API_KEY")
    serper_api_key: str | None = Field(default=None, alias="SERPER_API_KEY")

    # Shared secret the .NET backend must send as `X-Service-Api-Key`. When unset, the
    # service is open — fine for local dev, never for anything reachable outside localhost.
    service_api_key: str | None = Field(default=None, alias="CREWAI_SERVICE_API_KEY")

    host: str = Field(default="0.0.0.0", alias="CREWAI_SERVICE_HOST")
    port: int = Field(default=8000, alias="CREWAI_SERVICE_PORT")

    openai_model: str = Field(default="gpt-4o", alias="OPENAI_MODEL")


_settings: Settings | None = None


def get_settings() -> Settings:
    """Cached settings accessor — constructing `Settings()` is what actually validates
    that OPENAI_API_KEY is present, so the first call fails fast and loudly if it's missing.
    """
    global _settings
    if _settings is None:
        _settings = Settings()  # type: ignore[call-arg]
    return _settings
