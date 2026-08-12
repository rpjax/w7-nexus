# Nexus Dashboard Frontend Standards (mandatory)

**Audience:** humans and AI agents changing Nexus frontend apps.  
**Status:** constitution — not optional guidance.  
**If this conflicts with convenience, this wins.**

Applies to these frontend roots:

- `Nexus.Dashboard/` — current production dashboard
- `Refactor/web/` — refactor frontend
- `web/` — future post-refactor frontend root

Read this **before** any UI, UX, layout, or React feature work under those trees. Recipes live in [frontend-patterns.md](frontend-patterns.md); this file defines the **non-negotiable contract**. Backend architecture law remains [architecture-blueprint.md](architecture-blueprint.md).

---

## 0. Agent preamble

| Step | Document |
|------|----------|
| 1 | This file |
| 2 | [frontend-patterns.md](frontend-patterns.md) — approved UI/UX recipes |
| 3 | [`../Nexus.Dashboard/README.md`](../Nexus.Dashboard/README.md) — local dev and app entry |
| 4 | [`../Nexus.Dashboard/src/App.tsx`](../Nexus.Dashboard/src/App.tsx) — route source of truth |
| 5 | [`../Nexus.Dashboard/src/layouts/DashboardLayout.tsx`](../Nexus.Dashboard/src/layouts/DashboardLayout.tsx) — shell/layout source of truth |

**Hard bans before you type:**

- Do not introduce a second UI kit. **Use only the existing shadcn-style primitives.**
- Do not ship god pages or god components.
- Do not present complex data or procedures as raw JSON/text walls as the primary UI.
- Do not saturate the default viewport; use **revealing UI**.
- Do not invent a competing visual language beside the existing token system.
- Do not bypass the shared list/detail/layout patterns when they already fit the job.
- Do not add raw hex colors in TSX when semantic tokens can express the intent.

---

## 1. Product surfaces and route model

One authenticated SPA is the dashboard product. Today it lives under `Nexus.Dashboard/` and is routed from [`src/App.tsx`](../Nexus.Dashboard/src/App.tsx). The same constitution applies to `Refactor/web/` and the future `web/` root when those trees are active.

### Main surfaces

| Surface | Routes | Primary job | Density |
|---------|--------|-------------|---------|
| Auth | `/auth` | Sign in / sign up | Minimal — brand + form stage ([recipe 2.11](frontend-patterns.md#211-auth-surface-canonical-for-refactorweb)) |
| Dashboard home | `/dashboard` | Entry and navigation | Light |
| Operator work | `/dashboard/operations`, `/dashboard/payments`, `/dashboard/gateways`, `/dashboard/olx/ads` | Execute daily operational tasks | Medium |
| Operation admin work | `/dashboard/operation-admin/*` | Manage scoped operations and teams | Medium |
| Team leader work | `/dashboard/team-leader/*` | Track and manage led teams | Medium |
| Global admin | `/dashboard/admin/*`, `/dashboard/accounts` | Global administration, scripts, API docs, payments, operations | Rich, drill-down heavy |
| Straw man | `/dashboard/straw-man/*` | Self-service payment/settings tasks | Medium |

### Shell contract

The default app shell is [`src/layouts/DashboardLayout.tsx`](../Nexus.Dashboard/src/layouts/DashboardLayout.tsx):

- sidebar navigation
- sticky top bar
- outlet-driven content
- account menu in the shell footer

Do not invent competing page shells unless a surface has a strong reason to break from the dashboard chrome.

### Stack lock

| Layer | Choice |
|-------|--------|
| Framework | React + TypeScript |
| Build | Vite |
| Styling | Tailwind CSS + tokens in `Nexus.Dashboard/src/index.css` |
| Primitives | shadcn-style under `Nexus.Dashboard/src/components/ui/` |
| Icons | `lucide-react` |
| Routing | `react-router-dom` |
| Imports | `@/` → `src/*` |

---

## 2. Visual system and primitive law

### 2.1 Primitive source of truth

Use the existing shadcn-style primitive layer under [`Nexus.Dashboard/src/components/ui/`](../Nexus.Dashboard/src/components/ui/).

When a primitive is missing:

1. add or extend it in `components/ui`
2. match the existing style (`cn()`, CVA-style variants, named exports)
3. compose feature widgets on top

Do not invent a second button, input, dialog, card, table, or sheet style.

### 2.2 Tokens and styling

- Reuse semantic tokens from `src/index.css`.
- Prefer semantic classes (`bg-card`, `text-muted-foreground`, `border-border`, etc.).
- No one-off color system in feature files.
- Inline styles should be rare and justified.

### 2.3 Icons

- One icon set: `lucide-react`
- Icon-only controls must have an accessible name
- Prefer icon + text for primary or destructive actions

---

## 3. UX constitution

Agents and humans must follow these principles:

1. **Primary path first** — the default viewport should show the main job, not every edge case.
2. **Progress on interaction** — detail appears via drill-down, tabs, sheet, accordion, or route depth.
3. **Self-explanatory UI** — labels, helpers, empty states, and inline feedback must replace manual instructions.
4. **Zero unnecessary operator busywork** — sensible defaults, prefilled values, and direct actions beat ritual forms.
5. **One job per view** — if a page starts to do multiple unrelated jobs, split it.
6. **Enrich where value is high** — do not ship thin CRUD when the domain benefits from structure.

**Revealing UI** means the page starts calm and earns density as the user engages.

---

## 4. Information architecture

### Limits

| Limit | Rule |
|-------|------|
| Soft cap | ~350 LOC per page file before you strongly consider extraction |
| Investigate | >500 LOC should be treated as a split signal |
| Route rule | One primary job per route |

### Default patterns

- Use **route depth** for deep work: list -> detail -> advanced
- Use **Tabs** for peer facets of one job
- Use **Sheet/Dialog** for quick actions or transient flows
- Use **Accordion/Advanced disclosure** for rare options

Do not use Tabs as a dumping ground for unrelated jobs.

---

## 5. Component architecture

**Composition stack:**

```text
Page / Route
  -> Section or flow
    -> Feature widget
      -> components/ui primitive
```

### Existing shared patterns to prefer

- [`src/components/layout/list-page-layout.tsx`](../Nexus.Dashboard/src/components/layout/list-page-layout.tsx) for list/index pages
- [`src/components/data/data-table.tsx`](../Nexus.Dashboard/src/components/data/data-table.tsx) for tables
- `src/components/data/list-pagination.tsx` for paginated lists
- detail shells such as:
  - `src/features/operations/OperationDetailShell.tsx`
  - `src/features/teams/TeamDetailShell.tsx`
  - `src/features/payments/PaymentDetailShell.tsx`

### Rules

- One component should answer one user question or encapsulate one reusable interaction.
- Split fetching/orchestration from presentational sections when the file starts carrying too many jobs.
- Prefer feature-local widgets over giant page-local JSX blobs.
- Do not centralize every behavior into a single page file.

---

## 6. Complex data and interaction

Complex data must be structured, not dumped.

| Situation | Preferred facilitator |
|-----------|------------------------|
| Large lists | Table + filters + pagination + row drill-down (**dense DataTable**; never card-stacks as the primary list) |
| Entity management | List page -> detail route |
| Many peer facets of one entity | Tabs |
| Rare or advanced options | Disclosure / accordion |
| Destructive flows | AlertDialog |
| Long-running admin mutation | Inline status + disabled action + retry path |
| Dense operational summary | KPI cards + list/table below |

### Rules

- Summarize first, expand second.
- Row click may open detail route or a quick sheet depending on depth of work.
- Raw payloads belong in **Technical details** and never as the landing experience.
- If the operator needs to understand state transitions, show structure and labels, not bare API output.

---

## 7. Interaction and state contracts

Every interactive surface must define UI for:

| State | Expectation |
|-------|-------------|
| Idle | Clear primary action |
| Loading | Disabled controls + visible progress |
| Empty | Helpful next step |
| Success | Explicit confirmation |
| Error | Visible, actionable feedback |

### Required behavior

- Forms use controlled inputs when the surface is stateful or save-oriented.
- Save/submit actions disable while pending.
- Errors must be visible in-page; toast-only error handling is insufficient.
- Destructive actions require explicit confirmation.
- Silent `catch` blocks are forbidden.

---

## 8. Accessibility and keyboard

- Focus order follows visual order
- Visible focus states must be preserved
- Every control needs an accessible name
- Status cannot rely on color alone
- Dialog/Sheet interactions must restore focus correctly

---

## 9. Copy and content

- Product UI language follows the current product convention already present in the app
- Prefer plain language
- Buttons should be verb-led
- Empty states should teach the next action in one sentence when possible
- Avoid leaking internal jargon into operator-facing labels

---

## 10. React and code structure

- Match the existing route and feature organization instead of inventing a new frontend architecture mid-stream.
- Prefer local state and existing hooks before introducing new global state.
- Reuse existing feature helpers (`usePaginatedQuery`, notification context, path helpers, column factories) when they match the job.
- Do not redesign unrelated surfaces during focused feature work.

---

## 11. Canonical examples in this codebase

Use these as current reference implementations:

| Pattern | Canonical file |
|---------|----------------|
| Dashboard shell | [`src/layouts/DashboardLayout.tsx`](../Nexus.Dashboard/src/layouts/DashboardLayout.tsx) |
| Standard list page | [`src/components/layout/list-page-layout.tsx`](../Nexus.Dashboard/src/components/layout/list-page-layout.tsx) |
| Shared table | [`src/components/data/data-table.tsx`](../Nexus.Dashboard/src/components/data/data-table.tsx) |
| Admin operations list | [`src/pages/admin/AdminOperationsPage.tsx`](../Nexus.Dashboard/src/pages/admin/AdminOperationsPage.tsx) |
| Admin payments list with filters | [`src/pages/admin/AdminPaymentsPage.tsx`](../Nexus.Dashboard/src/pages/admin/AdminPaymentsPage.tsx) |
| Advanced command center list | [`src/pages/admin/scripts/ScriptsCommandCenterPage.tsx`](../Nexus.Dashboard/src/pages/admin/scripts/ScriptsCommandCenterPage.tsx) |
| Complex detail page | [`src/pages/admin/scripts/ScriptStudioPage.tsx`](../Nexus.Dashboard/src/pages/admin/scripts/ScriptStudioPage.tsx) |

---

## 12. Explicit anti-patterns

| Ban | Why |
|-----|-----|
| Second UI kit | Breaks visual consistency |
| God page | Hard to review and evolve |
| God component | Untestable and unreusable |
| Raw JSON/text wall as primary UI | Hides structure |
| Thin CRUD where the domain needs drill-down | Wastes operator attention |
| Settings dump with every field visible at once | Violates revealing UI |
| Tabs for unrelated jobs | Fake IA |
| Toast-only errors | Easy to miss |
| Silent catch | Breaks trust |
| Raw hex drift in feature TSX | Breaks token discipline |

---

## 13. Merge checklist

- [ ] Used only existing shadcn-style primitives or extended them coherently
- [ ] Primary path is visible without saturating the viewport
- [ ] Complex data/flows use a facilitator, not a dump
- [ ] Page/component split is still healthy
- [ ] Loading / empty / success / error states are explicit
- [ ] a11y basics are covered
- [ ] New reusable recipe was added to [frontend-patterns.md](frontend-patterns.md) if it became canonical
- [ ] [`../Nexus.Dashboard/README.md`](../Nexus.Dashboard/README.md) updated if app usage or structure changed
- [ ] Frontend checks were run when the change warranted them

---

## 14. Where detail lives

| Concern | Canonical doc |
|---------|----------------|
| Frontend constitution | This file |
| UX recipes / decision trees | [frontend-patterns.md](frontend-patterns.md) |
| Backend architecture boundaries | [architecture-blueprint.md](architecture-blueprint.md) |
| Dashboard app entry / dev | [`../Nexus.Dashboard/README.md`](../Nexus.Dashboard/README.md) |

This file and [frontend-patterns.md](frontend-patterns.md) are the permanent frontend law for `Nexus.Dashboard/`, `Refactor/web/`, and future `web/`.
