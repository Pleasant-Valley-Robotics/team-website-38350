# Team website

Your team's own website, written in C# with ASP.NET Core MVC (.NET 10). It is part
of Pleasant Valley FIRST Robotics and runs on the program's robotics server.

| | Address | Who can see it |
|---|---|---|
| **Preview** | `https://dev-38350.firstpvrobotics.com` | Mentors (password) |
| **Live site** | `https://38350.firstpvrobotics.com` | Everyone |

## Run it on your computer

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) and
[Visual Studio Code](https://code.visualstudio.com) with the C# Dev Kit extension. Then, in a terminal:

```bash
dotnet watch --project src/TeamSite.Web     # opens http://localhost:5300 and reloads when you save
dotnet test                                  # runs the tests
```

## How a change gets to the website

1. **Make a branch** and edit the code.
2. **Run `dotnet test`.** Fix anything red before you push.
3. **Open a pull request** into `main`. GitHub builds the site, runs the tests and starts the container. A red ✗ means something is broken: click **Details** to see why. You can't merge until it's green.
4. **Merge the pull request.** Within about a minute the robotics server pulls `main` and updates the **preview**. Look at it with a mentor.
5. **Publish it live** by creating a release tag (a mentor or team lead does this):
   - On GitHub: **Releases → Draft a new release → Choose a tag**, type the next version (for example `v2.0.1`), click **Create new tag**, then **Publish release**.
   - Or in a terminal: `git tag v2.0.1 && git push origin v2.0.1`

   Within about a minute the live site updates. Tags must start with `v`; the newest one is what's live.

**If a release is broken**, the server sees that `/healthz` doesn't answer and keeps the previous version running. To see what happened, open `https://dev-38350.firstpvrobotics.com/_platform/` (mentor password).

## How it's put together (MVC)

| File or folder | What it does |
|---|---|
| `src/TeamSite.Web/Data/TeamInfo.cs` | **Everything the site says about your team.** Start here |
| `Models/Team.cs` | The shape of that data: what a `Team`, a `Card` and a `SocialLink` are |
| `Controllers/HomeController.cs` | One method per page. `[Route("")]` above `Index()` makes it the home page |
| `Views/Home/Index.cshtml` | The HTML for the home page (Razor: HTML plus `@` for C#) |
| `Views/Shared/_Layout.cshtml` | The frame around every page: menu and footer |
| `wwwroot/` | Files sent as-is: `css/site.css`, `images/`, `fonts/` |
| `Program.cs` | Where the site starts |
| `tests/TeamSite.Web.Tests/` | Tests GitHub runs on every pull request |

### Add a page (for example, "Our Robot")

1. In `HomeController.cs`, add:
   ```csharp
   [Route("robot")]
   public IActionResult Robot() => View();
   ```
2. Create `Views/Home/Robot.cshtml` with your HTML. Put `@{ ViewData["Title"] = "Our Robot"; }` at the top.
3. Add `("Our Robot", "/robot"),` to `Nav` in `Data/TeamInfo.cs`.
4. Run `dotnet test`: the "every menu link works" test checks your new page answers.

**Please don't post students' full names or anything a parent wouldn't want
public.** First names and team photos are fine when your team agrees.

## Files the server needs (leave these alone unless you know why)

- `Dockerfile`: compiles the site and runs it.
- `compose.yaml`: how the server runs the container.
- `robotics.yml`: which address goes to the container. `public: true` opens the live site to everyone.
- `.github/workflows/check.yml`: the checks on every pull request.
