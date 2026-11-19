"""The exactly-five sequential CrewAI tasks. Each task's `context` list wires it to the
prior task's output, per the brief's "every task's output must automatically feed into the
next task's context." Guardrails are embedded directly in task descriptions for Research,
Writing, and Optimization, and checked explicitly by the Critique task.
"""

from crewai import Agent, Task

from app.models.schemas import (
    ContentReviewOutput,
    DraftOutput,
    OptimizationOutput,
    ResearchBriefOutput,
    SchedulingOutput,
)

GUARDRAILS_TEXT = (
    "Content guardrails you MUST follow:\n"
    "1. Do not state any statistic or factual claim unless it traces back to a specific URL "
    "you found during research. If you cannot source a claim, omit it or phrase it as a "
    "general industry observation rather than a cited fact.\n"
    "2. Do not mention competitors or name any specific companies.\n"
    "3. Avoid controversial or political topics entirely — stay focused on the business/"
    "technology angle."
)


def build_research_task(agent: Agent) -> Task:
    return Task(
        description=(
            "Research the latest LinkedIn trends, viral content patterns, and hot topics for "
            "the niche '{niche}', focused on the angle '{topic}'.\n\n"
            "Identify:\n"
            "- 3-5 trending content angles\n"
            "- Relevant hashtags\n"
            "- Trending hooks\n"
            "- Industry developments\n"
            "- Research source URLs (every URL you cite for a statistic or claim)\n\n"
            f"{GUARDRAILS_TEXT}"
        ),
        expected_output=(
            "A structured research brief with 3-5 trending content angles, relevant hashtags, "
            "high-performing content hooks, and a list of source URLs backing every factual "
            "claim found."
        ),
        agent=agent,
        output_pydantic=ResearchBriefOutput,
    )


def build_writing_task(agent: Agent, research_task: Task) -> Task:
    return Task(
        description=(
            "Using the Research Task output, write a compelling LinkedIn post about '{topic}' "
            "in the {persona} voice.\n\n"
            "Requirements:\n"
            "- Strong first two lines (the hook)\n"
            "- Storytelling or value-driven body\n"
            "- 150-300 words\n"
            "- Clear call to action\n"
            "- Relevant hashtags\n\n"
            f"{GUARDRAILS_TEXT}"
        ),
        expected_output=(
            "A complete LinkedIn draft: strong opening hook, storytelling/value-driven body, "
            "clear CTA, 150-300 words, with suggested hashtags."
        ),
        agent=agent,
        context=[research_task],
        output_pydantic=DraftOutput,
    )


def build_critique_task(agent: Agent, writing_task: Task) -> Task:
    return Task(
        description=(
            "Evaluate the original draft from the Writing Task.\n\n"
            "Provide:\n"
            "- Overall score out of 10\n"
            "- Hook strength evaluation\n"
            "- Storytelling quality evaluation\n"
            "- Engagement potential\n"
            "- CTA effectiveness\n"
            "- Tone and readability assessment, explicitly including fit with the {persona} "
            "persona\n"
            "- Any content guardrail violations (unsourced claims, competitor/company "
            "mentions, political/controversial content) — list each one explicitly, or an "
            "empty list if none\n"
            "- Specific improvement recommendations"
        ),
        expected_output=(
            "A detailed content critique: score out of 10, hook/storytelling/engagement/CTA "
            "evaluation, tone-and-persona-fit assessment, an explicit guardrail-violations "
            "list, and specific recommendations."
        ),
        agent=agent,
        context=[writing_task],
        output_pydantic=ContentReviewOutput,
    )


def build_optimization_task(agent: Agent, writing_task: Task, critique_task: Task) -> Task:
    return Task(
        description=(
            "Using the original draft and the Critic's feedback, improve the post's hook, "
            "copywriting, structure, readability, formatting, CTA, and hashtags — while "
            "preserving the {persona} voice.\n\n"
            f"{GUARDRAILS_TEXT}"
        ),
        expected_output=(
            "The final, publish-ready LinkedIn post: improved hook, refined body, strong CTA, "
            "optimized hashtags, better mobile readability."
        ),
        agent=agent,
        context=[writing_task, critique_task],
        output_pydantic=OptimizationOutput,
    )


def build_scheduling_task(agent: Agent, optimization_task: Task) -> Task:
    return Task(
        description=(
            "Analyze the final optimized post and the target audience for the '{niche}' "
            "niche. Recommend the best publishing day and time, and prepare the final "
            "publishing brief.\n\n"
            "You do not have access to real LinkedIn analytics for this account — reason from "
            "general, well-documented LinkedIn engagement patterns for this niche and say so "
            "plainly rather than implying you checked real analytics."
        ),
        expected_output=(
            "A complete publishing brief: recommended day, recommended time, audience "
            "timezone, the final copy-paste-ready post, hashtag strategy, and first-hour "
            "engagement recommendations."
        ),
        agent=agent,
        context=[optimization_task],
        output_pydantic=SchedulingOutput,
    )
