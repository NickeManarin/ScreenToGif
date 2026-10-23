# Ticket store

Venue: files
Root: tickets/
Efforts: one directory per effort, with a README.md describing its destination and context.
Open tickets: numbered Markdown files directly within each effort directory.
Completed tickets: the effort's done/ directory.
Types: bug, feature, refactor, docs, chore.

Claims apply only within the same checkout. They do not coordinate separate clones, worktrees, or branches. Run automated tickets serially in a shared checkout.

Grant unattended execution only through an explicit Auto line naming its grantor on the ticket. Use rebase for integration; never merge.
