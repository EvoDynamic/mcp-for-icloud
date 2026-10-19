# iCloud MCP — iCloud Mail & Calendar for Claude

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

### Prerequisites
- Windows (credentials are read from Credential Manager). macOS works too; see [macOS](#macos).
- [.NET 10 SDK](https://dotnet.microsoft.com/download).
- Claude Desktop.

### 0. Build it

```bash
git clone https://github.com/brianseim/icloud-mcp.git
```

```bash
dotnet publish icloud-mcp/src/ICloudMcp/ICloudMcp.csproj -c Release -o icloud-mcp/publish
```

### 1. Create an app-specific password
1. Go to <https://account.apple.com> → **Sign-In and Security** → **App-Specific Passwords**.
2. Generate one named `iCloud MCP`. Keep the page open.

### 2. Store it in Windows Credential Manager
In a terminal, run this and paste the password when prompted (it isn't echoed or saved to any file):

```bash
cmdkey /generic:icloud-mcp /user:you@icloud.com /pass
```

Use the Apple ID you sign in to iCloud with as `/user`.

### 3. Create your config
Copy `config.example.json` to `%APPDATA%\icloud-mcp\config.json` and fill in your Apple ID, every alias you receive mail at, time zone, working hours and VIPs.

If your Apple ID is not an @icloud.com/@me.com address (e.g. it's a Gmail address), also set `MailUser` to your @icloud.com address — that's the IMAP login.

### 4. Register it with Claude Desktop
Open Claude Desktop → **Settings → Developer → Edit Config** and add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "icloud-mcp": {
      "command": "C:\\path\\to\\icloud-mcp\\publish\\icloud-mcp.exe"
    }
  }
}
```

Use the full path to your `publish` folder. (Merge into the existing `mcpServers` object if you already have one.) Fully quit and reopen Claude Desktop.

### 5. Give Claude your working rules
Create a Claude project for your assistant, paste [`docs/example-project-instructions.md`](docs/example-project-instructions.md) into its instructions, and edit the priorities section. In Claude's tool settings, leave the write tools on "ask before use".

### 6. Try it
In a chat in that project:
- "What needs my attention in email today?"
- "Check my spam for anything real from the last two weeks."
- "Draft a reply to the latest email from Dana saying Thursday works."
- "Find me a free hour next Tuesday afternoon and hold it for proposal review."

## Adding another computer

Each machine runs its own copy of the server; they all talk to the same iCloud account.

1. Install the [prerequisites](#prerequisites), then clone and build ([step 0](#0-build-it)).
2. Create a **separate app-specific password** for this machine (e.g. `iCloud MCP – laptop`), so you can revoke one device without breaking the others, and store it with `cmdkey` as in [step 2](#2-store-it-in-windows-credential-manager).
3. Copy `%APPDATA%\icloud-mcp\config.json` from your existing machine instead of writing a new one.
4. Register the server with Claude Desktop ([step 4](#4-register-it-with-claude-desktop)) using this machine's `publish` path, then fully quit Claude Desktop from the system tray and reopen it.
5. If you sign in to the same Claude account, your project and its instructions are already there. Ask it "List my calendars" to confirm the connection.
6. If you'll change code on this machine, enable the [pre-push hook](#contributing-workflow).

### macOS

Credential Manager is Windows-only, so pass the login through environment variables in `claude_desktop_config.json` instead.
This stores the app-specific password in plain text in that file; it's limited to mail, calendar and contacts and can be revoked at any time.

```json
"icloud-mcp": {
  "command": "/Users/you/source/icloud-mcp/publish/icloud-mcp",
  "env": {
    "ICLOUD_MCP_USER": "you@icloud.com",
    "ICLOUD_MCP_APP_PASSWORD": "xxxx-xxxx-xxxx-xxxx"
  }
}
```

The config file lives at `~/.config/icloud-mcp/config.json`.

## Updating after code changes

Fully quit Claude Desktop first, or the running server will lock the files. Then, from the repo root:

```bash
git pull
```

```bash
dotnet publish src/ICloudMcp/ICloudMcp.csproj -c Release -o publish
```

Reopen Claude Desktop to load the new version.

### Upgrading from AI VA

This project used to be called AI VA. After updating:
- In `claude_desktop_config.json`, rename the `ai-va` entry to `icloud-mcp` and point it at `publish\icloud-mcp.exe` (the old `AiVa.Mcp.exe` is no longer built; delete it from `publish`).
- Your existing setup keeps working without other changes: the old `AiVa-iCloud` credential, `%APPDATA%\AiVa\config.json` and `AIVA_*` environment variables are still read when the new ones don't exist.
- To move to the new names, run the `cmdkey` command from [step 2](#2-store-it-in-windows-credential-manager), move the config to `%APPDATA%\icloud-mcp\config.json`, then remove the old credential:

```bash
cmdkey /delete:AiVa-iCloud
```

## Contributing workflow

`main` is protected on GitHub and only changes through pull requests.
A versioned pre-push hook also stops direct pushes to `main` locally, before GitHub would reject them.
Enable it once per clone:

```bash
git config core.hooksPath .githooks
```

Then work on a branch, push it, and open a PR with `gh pr create`; merge with `gh pr merge` or on GitHub.

## Notes and limits
- Local MCP servers work in **Claude Desktop** (chats and projects). claude.ai in a browser or on mobile can't reach a local server; that would need this hosted as a remote MCP server with auth.
- Recurring events: titles/locations/notes can be edited (whole series); time changes are left to the Calendar app.
- Creating events with invitees isn't supported yet; Claude can draft the invite email instead.
- Priority scores are hints with reasons, not verdicts. Tune them in `config.json` → `Priority`.
- Overrides: `ICLOUD_MCP_CONFIG` (config path), `ICLOUD_MCP_USER` / `ICLOUD_MCP_APP_PASSWORD` (if you'd rather not use Credential Manager).
