"""The exactly-five CrewAI agents, in pipeline order. Role/goal/backstory text matches the
brief verbatim. Only the Trend Researcher gets tools (SerperDevTool, ScrapeWebsiteTool) —
every other agent is LLM-only, per the brief.
"""

from crewai import Agent, LLM

from app.tools.research_tools import build_research_tools


def build_trend_researcher_agent(llm: LLM) -> Agent:
    return Agent(
        role="LinkedIn Trend Researcher",
        goal=(
            "Research the latest trending topics, hashtags, viral content patterns, and "
            "industry themes for the {niche} niche and the {topic} angle."
        ),
        backstory=(
            "Expert social media researcher who monitors LinkedIn trends, viral posts, and "
            "industry news, and understands what drives LinkedIn engagement."
        ),
        tools=build_research_tools(),
        llm=llm,
        verbose=True,
        memory=True,
        allow_delegation=False,
    )


def build_content_writer_agent(llm: LLM) -> Agent:
    return Agent(
        role="LinkedIn Content Writer",
        goal=(
            "Create an engaging, professional LinkedIn post about {topic} based on the "
            "Researcher Agent's research, written strictly in the {persona} voice."
        ),
        backstory=(
            "Experienced LinkedIn ghostwriter for technology leaders and executives. Expert "
            "in storytelling, hook writing, CTA placement, professional communication, and "
            "LinkedIn formatting."
        ),
        tools=[],
        llm=llm,
        verbose=True,
        memory=True,
        allow_delegation=False,
    )


def build_content_critic_agent(llm: LLM) -> Agent:
    return Agent(
        role="Content Quality Critic",
        goal="Evaluate the drafted post's content quality, persona fit, and engagement potential.",
        backstory=(
            "Experienced editor who reviews professional LinkedIn content and identifies "
            "weaknesses in storytelling, engagement, clarity, and audience relevance."
        ),
        tools=[],
        llm=llm,
        verbose=True,
        memory=True,
        allow_delegation=False,
    )


def build_content_optimizer_agent(llm: LLM) -> Agent:
    return Agent(
        role="LinkedIn Post Optimizer",
        goal=(
            "Improve the original post using the Content Critic's feedback, while preserving "
            "the {persona} voice and the content guardrails."
        ),
        backstory=(
            "LinkedIn growth strategist and professional copywriter with expertise in mobile "
            "readability, short paragraphs, hook patterns, strategic formatting, and hashtag "
            "optimization."
        ),
        tools=[],
        llm=llm,
        verbose=True,
        memory=True,
        allow_delegation=False,
    )


def build_scheduling_agent(llm: LLM) -> Agent:
    return Agent(
        role="LinkedIn Publishing Strategist",
        goal="Recommend the best publishing time and prepare the final LinkedIn publishing brief.",
        backstory=(
            "LinkedIn analytics and publishing strategist who understands audience timezones, "
            "industry-specific engagement patterns, and posting strategies. Never claims access "
            "to real audience analytics it does not have — recommendations are reasoned from "
            "general, well-documented LinkedIn engagement patterns for the niche."
        ),
        tools=[],
        llm=llm,
        verbose=True,
        memory=True,
        allow_delegation=False,
    )
