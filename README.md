# AI VA — iCloud Mail & Calendar MCP server

A local [MCP](https://modelcontextprotocol.io) server that lets Claude triage your iCloud Mail (including custom-domain aliases), rescue misfiled Junk, save reply drafts, and manage your iCloud Calendar.

It talks to iCloud directly over IMAP (`imap.mail.me.com`) and CalDAV (`caldav.icloud.com`) using an **app-specific password**, and runs on your PC as a child process of Claude Desktop. Nothing is hosted anywhere.

## What it can do

| Tool | What it does | Changes anything? |
| --- | --- | --- |
| `mail_triage` | Pre-ranks recent inbox mail with reasons (VIPs, people you've emailed, alias weights, urgent keywords, bulk mail) | No |
| `mail_review_spam` | Scores Junk by how likely it's legitimate (known correspondents, VIPs, trusted domains, thread replies, SPF/DKIM/DMARC) | No |
| `mail_search`, `mail_read`, `mail_list_folders` | Find and read mail without marking it read | No |
| `mail_create_draft` | Saves a draft to iCloud Drafts, threaded and from the right alias | Adds a draft |
| `mail_mark_not_spam` | Moves Junk → Inbox and tags it Not Junk | Yes |
| `mail_move`, `mail_set_flags` | Archive/move (never to Trash), read/unread, flag | Yes |
| `calendar_list_calendars`, `calendar_list_events`, `calendar_find_free_time` | Read your schedule, expand recurring events, find open slots in working hours | No |
| `calendar_create_event`, `calendar_update_event` | Create/edit events (no invitations are sent); new events can repeat daily/weekly/monthly/yearly or follow an RRULE | Yes |
| `calendar_delete_event` | Delete an event (whole series if recurring) | Yes (marked destructive) |

**There is deliberately no send tool and no delete-mail tool.** Drafts land in iCloud Drafts and you send them from Mail on any device.

## Setup (about 10 minutes)

### 1. Create an app-specific password
1. Go to <https://account.apple.com> → **Sign-In and Security** → **App-Specific Passwords**.
2. Generate one named `AI VA`. Keep the page open.

### 2. Store it in Windows Credential Manager
In a terminal, run this and paste the password when prompted (it isn't echoed or saved to any file):

```bash
cmdkey /generic:AiVa-iCloud /user:you@icloud.com /pass
```

Use the Apple ID you sign in to iCloud with as `/user`.

### 3. Create your config
Copy `config.example.json` to `%APPDATA%\AiVa\config.json` and fill in your Apple ID, every alias you receive mail at, time zone, working hours and VIPs.

If your Apple ID is not an @icloud.com/@me.com address (e.g. it's a Gmail address), also set `MailUser` to your @icloud.com address — that's the IMAP login.

### 4. Register it with Claude Desktop
Open Claude Desktop → **Settings → Developer → Edit Config** and add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "ai-va": {
      "command": "D:\\source\\ai-va-mcp\\publish\\AiVa.Mcp.exe"
    }
  }
}
```

(Merge into the existing `mcpServers` object if you already have one.) Fully quit and reopen Claude Desktop.

### 5. Point the AI VA project at it
Paste [`docs/ai-va-project-instructions.md`](docs/ai-va-project-instructions.md) into the **AI VA** project's instructions and edit the priorities section. In Claude's tool settings, leave the write tools on "ask before use".

### 6. Try it
In an AI VA chat:
- "What needs my attention in email today?"
- "Check my spam for anything real from the last two weeks."
- "Draft a reply to the latest email from Dana saying Thursday works."
- "Find me a free hour next Tuesday afternoon and hold it for proposal review."

## Updating after code changes

```bash
dotnet publish D:\source\ai-va-mcp\src\AiVa.Mcp\AiVa.Mcp.csproj -c Release -o D:\source\ai-va-mcp\publish
```

Quit Claude Desktop first, or the running exe will be locked.

## Contributing workflow

`main` only changes through pull requests.
GitHub can't enforce branch protection on a free private repo, so a versioned pre-push hook blocks direct pushes to `main` instead.
Enable it once per clone:

```bash
git config core.hooksPath .githooks
```

Then work on a branch, push it, and open a PR with `gh pr create`; merge with `gh pr merge` or on GitHub.
The hook is a guard rail, not a lock: `git push --no-verify` skips it.

## Notes and limits
- Local MCP servers work in **Claude Desktop** (chats and projects). claude.ai in a browser or on mobile can't reach a local server; that would need this hosted as a remote MCP server with auth.
- Recurring events: titles/locations/notes can be edited (whole series); time changes are left to the Calendar app.
- Creating events with invitees isn't supported yet; Claude can draft the invite email instead.
- Priority scores are hints with reasons, not verdicts. Tune them in `config.json` → `Priority`.
- Overrides: `AIVA_CONFIG` (config path), `AIVA_ICLOUD_USER` / `AIVA_ICLOUD_APP_PASSWORD` (if you'd rather not use Credential Manager).
