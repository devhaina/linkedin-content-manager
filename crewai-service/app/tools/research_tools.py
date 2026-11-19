"""Real CrewAI tools for the Trend Researcher agent — no simulated/stubbed tools.

SerperDevTool reads SERPER_API_KEY from the environment itself (crewai_tools convention),
so the only thing this module does is fail fast with a clear message if it's missing,
rather than letting the agent silently get an empty/broken tool.
"""

import os

from crewai_tools import ScrapeWebsiteTool, SerperDevTool

from app.core.config import get_settings


def build_research_tools() -> list:
    settings = get_settings()
    if not settings.serper_api_key:
        raise RuntimeError(
            "SERPER_API_KEY is not configured. The Trend Researcher agent requires it for "
            "SerperDevTool and cannot run with a simulated/empty tool."
        )

    # SerperDevTool reads SERPER_API_KEY from os.environ; pydantic-settings loaded it from
    # .env into this process's config but not necessarily into os.environ, so set it explicitly.
    os.environ.setdefault("SERPER_API_KEY", settings.serper_api_key)

    return [SerperDevTool(), ScrapeWebsiteTool()]
