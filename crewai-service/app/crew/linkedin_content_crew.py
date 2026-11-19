"""Assembles the exactly-five agents and exactly-five tasks into one
`Process.sequential` Crew, per the brief's CrewAI configuration requirements.
"""

from crewai import LLM, Crew, Process, Task

from app.agents.definitions import (
    build_content_critic_agent,
    build_content_optimizer_agent,
    build_content_writer_agent,
    build_scheduling_agent,
    build_trend_researcher_agent,
)
from app.core.config import get_settings
from app.tasks.definitions import (
    build_critique_task,
    build_optimization_task,
    build_research_task,
    build_scheduling_task,
    build_writing_task,
)


class LinkedInContentCrew:
    """One instance = one five-agent pipeline, ready for a single `kickoff`."""

    def __init__(self) -> None:
        settings = get_settings()
        llm = LLM(model=f"openai/{settings.openai_model}", api_key=settings.openai_api_key, temperature=0.4)

        researcher = build_trend_researcher_agent(llm)
        writer = build_content_writer_agent(llm)
        critic = build_content_critic_agent(llm)
        optimizer = build_content_optimizer_agent(llm)
        scheduler = build_scheduling_agent(llm)

        research_task = build_research_task(researcher)
        writing_task = build_writing_task(writer, research_task)
        critique_task = build_critique_task(critic, writing_task)
        optimization_task = build_optimization_task(optimizer, writing_task, critique_task)
        scheduling_task = build_scheduling_task(scheduler, optimization_task)

        self.tasks: list[Task] = [research_task, writing_task, critique_task, optimization_task, scheduling_task]

        self.crew = Crew(
            agents=[researcher, writer, critic, optimizer, scheduler],
            tasks=self.tasks,
            process=Process.sequential,
            verbose=True,
            memory=True,
        )

    def kickoff(self, *, topic: str, niche: str, persona: str):
        """Runs the real five-agent pipeline synchronously. Raises on any agent/task failure —
        callers must not translate an exception here into a fabricated success response.
        """
        return self.crew.kickoff(inputs={"topic": topic, "niche": niche, "persona": persona})
