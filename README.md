# Team website

This is your team's own website. It is part of Pleasant Valley FIRST Robotics
and is hosted on the program's robotics server.

| | Address | Who can see it |
|---|---|---|
| **Preview** | `https://dev-38350.firstpvrobotics.com` | Mentors (password) |
| **Live site** | `https://38350.firstpvrobotics.com` | Everyone |

## How a change gets to the website

1. **Make a branch** and edit files (see "What to edit" below).
2. **Open a pull request** into `main`. GitHub checks that the site builds; a red ✗ means something is broken, so click "Details" to see why.
3. **Merge the pull request.** Within about a minute the robotics server pulls `main` and updates the **preview**. Ask a mentor to look at it with you.
4. **Publish it live** by creating a release tag. A mentor or team lead does this:
   - On GitHub: **Releases → Draft a new release → Choose a tag**, type the next version (for example `v1.0.1`), click **Create new tag**, then **Publish release**.
   - Or from a terminal: `git tag v1.0.1 && git push origin v1.0.1`

   Within about a minute the live site updates. Tags must start with `v`, and the newest one is what goes live.

**If a release is broken**, the server notices it isn't healthy and keeps the previous version running. To see what happened, open `https://dev-38350.firstpvrobotics.com/_platform/` (mentor password).

## What to edit

| What | Where |
|---|---|
| Team name, tagline, socials, highlights, outreach | `src/content/team.md` (the part between the `---` lines) |
| Your team's story | `src/content/team.md` (below the second `---`) |
| Logo and photos | `public/images/` (then update the path in `team.md`) |
| Colors and fonts | `src/styles/global.css` (`accent` in `team.md` sets the team color) |
| A new page | Add a file in `src/pages/`, e.g. `robot.astro` becomes `/robot`, then add it to `nav` in `src/layouts/Base.astro` |

**Please don't post students' full names, schools' private info or anything a parent
wouldn't want public.** First names and team photos are fine when your team agrees.

## Previewing on your own computer

You need [Node.js](https://nodejs.org) 22 or newer.

```bash
npm install        # first time only
npm run dev        # open http://localhost:4321 ; it updates as you save
```

## Files the server needs (leave these alone unless you know why)

- `Dockerfile` and `docker/nginx.conf`: build the site and serve it.
- `compose.yaml`: how the server runs the container.
- `robotics.yml`: which address goes to which container. `public: true` makes the live site open to everyone.
- `.github/workflows/check.yml`: the build check on pull requests.
