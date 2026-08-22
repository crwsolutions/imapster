# Folder-sync reconciliation: fix UpsertFolderAsync crash + prune stale local folders

- Status: Approved
- Plan file: `.alta/plans/2026-08-22-folder-sync-reconciliation.md`
- Created: 2026-08-22
- Task: Fix "Connection failed: Folder with ID 'Sent Messages' not found for account 1" by fixing the upsert bug and reconciling the local Folders table with the IMAP server (add missing, remove stale).
- Git: not ignored; commit this plan with the related implementation work

## Objective
- Make `Connect`/`Refresh` reconcile the local `Folders` table with the IMAP server: new remote folders are added; local folders that no longer exist on the server are removed (including their local emails).
- Non-goals: no UI/XAML changes, no email-sync logic changes, no new dependencies, no schema migration.

## Context and evidence
- Error (user-provided stack trace): `KeyNotFoundException: Folder with ID 'Sent Messages' not found for account 1.` at `FolderRepository.GetFolderByIdAsync` (FolderRepository.cs:31) → `UpsertFolderAsync` (FolderRepository.cs:51) → `ImapSyncService.FoldersAsync` (ImapSyncService.cs:153), raised during `MainViewModel.Connect` → `SyncFoldersAsync`.
- **Root cause of the crash**: `UpsertFolderAsync` (FolderRepository.cs:49-60) treats `GetFolderByIdAsync` as nullable, but that method **throws** `KeyNotFoundException` when the folder is absent (line 31). So the upsert can never insert a new folder; any remote folder that is not yet in the local DB (here: 'Sent Messages') aborts the entire folder sync — and therefore the whole Connect.
- The crash direction is the *inverse* of the reported hypothesis: 'Sent Messages' exists on the server but not locally. The hypothesis (local folder stale, gone from server) is real but a **missing feature**: `FoldersAsync` (ImapSyncService.cs:134-169) only upserts and never deletes local folders that disappeared from the server. Both are in scope.
- `Folders` schema: `PRIMARY KEY (AccountId, Id)` (Database.cs:69-79) → a SQLite `INSERT ... ON CONFLICT (AccountId, Id)` upsert is safe and atomic.
- `Emails` has FK `(AccountId, FolderId) REFERENCES Folders(AccountId, Id)` (Database.cs:101), but `PRAGMA foreign_keys=ON` is never set (grep: no occurrences), so deleting a folder alone leaves orphaned `Emails` rows. `EmailRepository.BulkDeleteEmailsAsync(accountId, folderId)` (EmailRepository.cs:117) already exists (used by `EmptyFolderAsync`) and is the right tool.
- `ImapClient.GetFolderAsync` returns **null** for folders that don't exist on the server (evidenced by null-checks in `EmptyFolderAsync`/`GetMessageAsync`); `EmailsAsync` (ImapSyncService.cs:66-67) would NRE if it hit that case.
- `MainViewModel.Connect` (MainViewModel.cs:282-320) and `Refresh` (MainViewModel.cs:427-463) sync emails of `SelectedFolder` *before* reloading the folder list; if that folder disappeared from the server, `EmailsAsync` throws and the whole operation fails with a confusing message.
- `MoveFolderPopupViewModel.LoadFoldersAsync` and `LoadFoldersFromLocalAsync` read from the local repository, so they automatically show the reconciled list — no changes needed.
- No unit tests exist (AGENTS.md: manual QA is the norm); verification is build + manual scenarios.

## Assumptions and open decisions
- **Resolved (user confirmed 2026-08-22):** local emails (including stored AI summaries) of pruned folders are deleted together with the folder.
- Assumption: local emails (including stored AI summaries) of pruned folders are intentionally discarded — they belong to folders that no longer exist on the server. (User explicitly asked for local folder removal; keeping orphans contradicts the schema FK intent.)
- Assumption: exact string match on folder `Id` (the IMAP `FullName`) is the correct reconciliation key — the same identity the app uses everywhere else.
- Decision: pruning happens automatically during Connect/Refresh, without a confirmation dialog (matches the "sync" semantics the user described).

## Design notes
- Chosen: SQLite-native upsert in `UpsertFolderAsync` — one atomic `ExecuteAsync`, single round-trip, removes the throwing path entirely. Rejected: get-then-insert via a new nullable `TryGetFolderByIdAsync` — works, but two round-trips and a duplicate query path.
- Chosen: reconciliation inside `ImapSyncService.FoldersAsync` — it owns both server state and repository access and is the natural single place (used by both Connect and Refresh). Rejected: doing it in `MainViewModel` (would duplicate in Connect/Refresh).
- Defensive: skip pruning if the remote folder list comes back **empty** (log a warning). An empty LIST during a transient server glitch would otherwise wipe the account's local folders.
- `GetFolderByIdAsync` keeps its throwing behavior and stays on `IFolderRepository` (public API; removing it is out of scope). After the fix it has no in-app callers.
- No interface or schema changes; `FoldersAsync` signature (`Task`) unchanged.

## Risks and challenges
- Data loss: pruning deletes local emails/AI summaries of folders gone from the server. Accepted per user request; call it out in the commit message.
- Reconciliation is exact `FullName` matching; a server-side *rename* is seen as delete + re-add (local emails of the old copy are lost). Existing app behavior already treats `FullName` as identity everywhere, so this is not a regression.
- `UnreadCount` is reset to 0 on every sync (pre-existing behavior, unchanged by this plan).

## Implementation checklist
- [x] `Repositories/FolderRepository.cs` — rewrite `UpsertFolderAsync` as a single Dapper `ExecuteAsync`: `INSERT INTO Folders (Id, Name, UnreadCount, IsTrash, AccountId) VALUES (@Id, @Name, @UnreadCount, @IsTrash, @AccountId) ON CONFLICT (AccountId, Id) DO UPDATE SET Name = @Name, UnreadCount = @UnreadCount, IsTrash = @IsTrash`. Drop the call to the throwing `GetFolderByIdAsync`.
- [x] `Services/ImapSyncService.cs` — in `FoldersAsync`, after the upsert loop: collect remote `FullName`s into a `HashSet<string>`; if the set is empty, log a warning and skip pruning; otherwise load `GetAllFoldersAsync(_currentAccount.Id)` and for each local folder whose `Id` is not in the set: log the prune, call `_emailRepository.BulkDeleteEmailsAsync(accountId, folderId)` then `_folderRepository.DeleteFolderAsync(accountId, folderId)`.
- [x] `Services/ImapSyncService.cs` — in `EmailsAsync`, guard the null return of `GetFolderAsync` (line 66): throw `ApplicationException($"Folder {folderId} not found")`, consistent with `MoveEmailsToTrashAsync`/`EmptyFolderAsync`.
- [x] `ViewModels/MainViewModel.cs` — in `Connect` (line ~301) and `Refresh` (line ~445): after `SyncFoldersAsync()`, if `SelectedFolder != null` verify its `Id` still exists via `GetAllFoldersAsync(SelectedAccount.Id)`; if gone, set `SelectedFolder = null` and skip `SyncEmailsAsync` (so `LoadFoldersAndEmailsFromLocaAsync` picks the Inbox/first default); otherwise sync as today.
- [x] Commit the plan file together with the implementation (`.alta/plans/` is not git-ignored).

## Verification checklist
- [x] `dotnet build` — compiles without warnings (AGENTS.md lint rule). Imapster main project: 0 warnings. (3 pre-existing CS8602 warnings in Imapster.HtmlViewer.Tests, untouched.)
- [ ] Manual QA — new folder added: with an existing local DB (the user's account 1, server has 'Sent Messages'), press Connect → no "Connection failed", status shows connected, 'Sent Messages' appears in the folder list.
- [ ] Manual QA — stale folder pruned: delete a folder on the server via another mail client, press Refresh/Connect → folder disappears from the UI and from `Folders`; its rows are gone from `Emails` (verify with a SQLite client on `imapster_data.db` in the app's AppData directory — path is printed by `Database.Initialize`).
- [ ] Manual QA — selected folder deleted on server: with that folder selected, press Connect → no error, UI selects the default folder (Inbox or first).
- [ ] Manual QA — regressions: existing folders still listed, email sync of the selected folder works, Move popup still shows folders, Empty Trash still works.

## Handoff notes
- Blast radius: one repository method, one service method (+ small NRE guard), and the Connect/Refresh guard in MainViewModel. No XAML, DI, or schema changes.
- Do not delete `imapster_data.db` between QA runs — the stale-state DB is exactly what this fix targets.
- The user's repro case (account 1, 'Sent Messages') is the primary acceptance test.
