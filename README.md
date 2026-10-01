<div align="center">
  <img
    alt="Starborne Troopers"
    width="800"
    height="300"
    src="https://github.com/Space-Marines-ss14/Starship-Troopers-SS14/blob/00ce9e510c23f289cabea166e5b2b8464949d8ed/starship-troopers.webp?raw=true"
  >
</div>

# Starborne Troopers
A fan-run Space Station 14 server about military sci-fi, bugs and planetary drops.

Starborne Troopers is a commercial project. The game is free to play, and the project may earn revenue through donations, sponsorship and other paid offerings. Current options are announced in our Discord.

Revenue is used for:
- server hosting and infrastructure;
- paying for development, art and other work on the project;
- development tools and resources;
- community events and content creation.

## Links
Discord: https://discord.gg/abmMbHupqj  
Patreon: https://www.patreon.com/cw/starborne_troopers  
Boosty: https://boosty.to/corvaxcolonialmarines

Upstream project (Space Station 14):
- Website: https://spacestation14.com/
- Documentation: https://docs.spacestation14.com/
- Repository: https://github.com/space-wizards/space-station-14

## Documentation
The Space Station 14 docs site (https://docs.spacestation14.com/) covers the game's content, engine, game design and more.

For asset attribution formats, see:
- Robust Generic Attribution: https://docs.spacestation14.com/en/specifications/robust-generic-attribution.html
- Robust Station Image: https://docs.spacestation14.com/en/specifications/robust-station-image.html

## License
This repository contains material under several licenses. In short:

| What | Where | License |
|---|---|---|
| Starborne Troopers code | `_ST14` directories | All rights reserved, viewing only |
| Space Station 14 code and our changes to it | everything outside `_ST14` | MIT (see `LICENSE-MIT.TXT`) |
| Original Starborne Troopers assets | `_ST14` directories | CC BY-NC-ND 4.0, authors keep their rights |
| Other assets | next to each asset | as specified in `meta.json` / `attributions.yml` |

The full terms, including the rules for contributions, are in [LICENSE.TXT](LICENSE.TXT). If this summary and LICENSE.TXT differ, LICENSE.TXT applies.

Starborne Troopers is a fan project and is not affiliated with, endorsed by or sponsored by the Space Wizards Federation.

## Code of Conduct
Starborne Troopers' team and community are volunteers working on every aspect of the project, including development, art, moderation and hosting.

Diversity is one of our strengths, but it can also lead to communication issues and unhappiness. To that end, we ask everyone to follow a few ground rules. This code applies equally to everyone, from commenters to contributors to staff.

This isn't an exhaustive list of things you can't do. Take it in the spirit in which it's intended: a guide to make it easier to enrich all of us and the communities in which we participate.

This code applies to this GitHub repository and the spaces managed by the Starborne Troopers project. Our Discord and game servers have their own rules, which are in spirit equal to this code.

If you believe someone is violating the code of conduct, please contact a maintainer or staff member through Discord (https://discord.gg/abmMbHupqj).

- Be friendly and patient.
- Be welcoming. We strive to be a community that welcomes and supports people of all backgrounds and identities.
- Be considerate. Your work will be used by other people, and you in turn will depend on the work of others. Any decision you take will affect users and contributors, and you should take those consequences into account. We have contributors of all skill levels, some making their first attempt at a new field with this project, so keep that in mind when discussing someone's work.
- Be respectful. Not all of us will agree all the time, but disagreement is no excuse for poor behavior and poor manners. A community where people feel uncomfortable or threatened is not a productive one. Assume contributions to the project, even those that are not accepted, are made in good faith.
- Be careful in the words that you choose. Do not insult or put down other participants. Harassment and other exclusionary behavior aren't acceptable. This includes, but is not limited to:
  - Violent threats or language directed against another person.
  - Discriminatory jokes and language.
  - Posting sexually explicit or violent material.
  - Posting (or threatening to post) other people's personally identifying information ("doxing").
  - Personal insults, especially those using racist or sexist terms.
  - Unwelcome sexual attention.
  - Advocating for, or encouraging, any of the above behavior.
  - Repeated harassment of others. In general, if someone asks you to stop, then stop.
- When we disagree, try to understand why. Disagreements, both social and technical, happen all the time. Resolve them constructively. Different people have different perspectives, and being unable to understand why someone holds a viewpoint doesn't mean that they're wrong. Instead of blaming each other, focus on resolving issues and learning from mistakes.

### On Community Moderation
Breaking the Code of Conduct in this repository may result in moderation actions by project maintainers. Your content may be edited or deleted, and you may be temporarily or permanently blocked from the repository.

Disagreeing with content or with each other is fine and appreciated, as long as it's done respectfully and constructively. Maintainers may act against content that is offensive, off-topic, hostile, or created only to provoke discussion.

If you believe an action was applied to you incorrectly, contact staff via Discord (https://discord.gg/abmMbHupqj).

### Attribution
This Code of Conduct is adapted from the Space Station 14 Code of Conduct, which is an edited version of the Django Code of Conduct (https://www.djangoproject.com/conduct/), licensed under CC BY 3.0. Original text courtesy of the Speak Up! project (http://web.archive.org/web/20141109123859/http://speakup.io/coc.html).

## Contributing
Thanks for contributing to Starborne Troopers!

- Put new Starborne Troopers code and assets in `_ST14` directories.
- Follow the Space Station 14 codebase conventions (https://docs.spacestation14.com/en/general-development/codebase-info/codebase-organization.html) and PR guidelines (https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html).
- For every new asset, fill in `meta.json` / `attributions.yml` with the author and license. Original assets use `CC-BY-NC-ND-4.0`; assets based on other works keep the license of the original.
- By submitting a contribution, you agree to the contribution terms in section 4 of [LICENSE.TXT](LICENSE.TXT).

### AI-generated contributions
We do not accept low-effort or wholesale AI-generated contributions, including:
- code (including yaml) generated by tools like GitHub Copilot, ChatGPT or similar;
- AI-created artwork, sound files or other assets;
- auto-generated documentation, issue reports or pull request descriptions.

Simple tools like single-line code completion are fine.

### Reporting a Security Vulnerability
Report security vulnerabilities privately to a maintainer through Discord (https://discord.gg/abmMbHupqj). Do not disclose the vulnerability publicly until we give you permission.

## Building
1. Clone this repo:
```
git clone https://github.com/Starborne-Troopers-SS14/Starborne-Troopers-SS14.git
```
2. Go to the project folder and run RUN_THIS.py to initialize the submodules and load the engine:
```
cd Starborne-Troopers-SS14
python RUN_THIS.py
```
3. Build the solution:
```
dotnet build
```

More detailed instructions: https://docs.spacestation14.com/en/general-development/setup.html