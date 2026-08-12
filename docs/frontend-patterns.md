# Nexus Dashboard Frontend Patterns (mandatory recipes)

**Audience:** humans and AI agents implementing UI under `Nexus.Dashboard/`, `Refactor/web/`, or future `web/`.  
**Status:** approved recipes — **copy these; do not invent competing patterns.**  
**Law:** [frontend-standards.md](frontend-standards.md). This file is the how-to companion.

When a new reusable recipe becomes canonical, add it here in the same change.

---

## 1. shadcn and visual consistency

### 1.1 Extending `components/ui`

When a needed primitive is missing:

1. add `Nexus.Dashboard/src/components/ui/<name>.tsx`
2. match the existing style and naming from files such as `button.tsx`, `select.tsx`, `tooltip.tsx`
3. use `cn()` and the same variant discipline used elsewhere
4. keep the primitive presentational; no feature data-fetching inside it
5. use semantic classes, not one-off hex values
6. do not install a competing component library

### 1.2 Tokens, `cn`, variants

```tsx
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'

<Button variant="outline" size="sm" className={cn(extra && 'opacity-80')}>
  Atualizar
</Button>
```

- Variants belong on the primitive, not copied ad hoc in every call site.
- Extend the token system in `Nexus.Dashboard/src/index.css` when a new semantic is needed.

### 1.3 Iconography

- One icon set: `lucide-react`
- Icon-only controls must have accessible names
- Prefer icon + text for primary actions

---

## 2. Revealing UI and flows

### 2.1 Standard list page

**Canonical:** use [`src/components/layout/list-page-layout.tsx`](../Nexus.Dashboard/src/components/layout/list-page-layout.tsx) with [`src/components/data/data-table.tsx`](../Nexus.Dashboard/src/components/data/data-table.tsx) (also under `Refactor/web/src/components/data/`).

**Canonical examples:**

- [`src/pages/admin/AdminOperationsPage.tsx`](../Nexus.Dashboard/src/pages/admin/AdminOperationsPage.tsx)
- [`src/pages/admin/AdminPaymentsPage.tsx`](../Nexus.Dashboard/src/pages/admin/AdminPaymentsPage.tsx)
- [`src/pages/admin/scripts/ScriptsCommandCenterPage.tsx`](../Nexus.Dashboard/src/pages/admin/scripts/ScriptsCommandCenterPage.tsx)
- [`Refactor/web/src/pages/AccountsPage.tsx`](../Refactor/web/src/pages/AccountsPage.tsx) — admin accounts table (refactor)

```text
PageHeader
Card
  Search
  Optional toolbar filters / create action
  Total label
  Table (DataTable, density compact by default)
  Pagination (ListPagination)
```

- Use this for index/list surfaces before inventing a new page scaffold.
- Empty, loading, and error states belong inside the list layout.
- **Do not ship button-lists or stacked cards as the primary representation of tabular collections.**

### 2.1.1 Dense data tables (mandatory)

Whenever the UI shows a collection of homogeneous records (accounts, payments, operations, logs, etc.):

1. Use `DataTable` + `Table` primitives — not ad-hoc `<button>` rows or card stacks.
2. Include a toolbar with **search**, **high-value filters**, and **page size** when the collection can grow.
3. Include `ListPagination` driven by server `limit`/`offset` (or equivalent) — do not dump unbounded lists.
4. Prefer **compact density**: short row height, truncated secondary text, badges for enums/status.
5. Maximize useful columns horizontally; put deep work in a detail pane/route on row click.
6. Fill vertical space: scrollable table body, sticky header, pagination pinned to the panel footer when the surface is tall.
7. Mobile: keep the table with horizontal scroll; do not replace it with a second competing list pattern unless the surface is inherently non-tabular.
8. Selected row must be visually obvious when list+detail share the viewport.

Port missing table primitives into the active frontend root (`components/ui/table`, `components/ui/select`, `components/data/data-table`, `components/data/list-pagination`) instead of inventing a one-off.

### 2.2 List -> detail drill-down

Use when the user must inspect or mutate one entity deeply.

```text
List route
  -> row click
Detail route
  -> overview
  -> peer facets via tabs or panels
  -> advanced / technical detail only when needed
```

**Canonical examples:**

- `AdminOperationsPage` -> `AdminOperationDetailPage`
- `AdminPaymentsPage` -> `AdminPaymentDetailPage`
- `ScriptsCommandCenterPage` -> `ScriptStudioPage`

Prefer a **route** for deep work. Use a Sheet only for light peek workflows.

### 2.3 Filterable admin list

**Canonical:** [`src/pages/admin/AdminPaymentsPage.tsx`](../Nexus.Dashboard/src/pages/admin/AdminPaymentsPage.tsx)

```text
ListPageLayout
  Search
  Inline toolbar filters (status, settlement, distribution, ...)
  Primary action
  DataTable
```

- Keep filters compact and task-oriented.
- Favor a few high-value filters over full advanced search forms on day one.

### 2.4 KPI + list command center

**Canonical:** [`src/pages/admin/scripts/ScriptsCommandCenterPage.tsx`](../Nexus.Dashboard/src/pages/admin/scripts/ScriptsCommandCenterPage.tsx)

```text
Page header
KPI cards
Filter bar
DataTable
Pagination
Create modal
```

Use when operators need a quick summary before acting on a collection.

### 2.5 Dense detail studio

**Canonical:** [`src/pages/admin/scripts/ScriptStudioPage.tsx`](../Nexus.Dashboard/src/pages/admin/scripts/ScriptStudioPage.tsx)

```text
Header + key metadata
Overview form / summary
Tabs for peer facets
Dialogs / drawers for focused mutations
Timeline / inspector / matrix where the domain needs it
Technical detail last
```

- Dense pages are allowed only when all parts serve one domain job.
- Prefer tabs, panels, and focused overlays over one endless column.

### 2.6 Revealing settings (summary + Advanced)

Use when a settings surface has a common path and rare options.

```text
[Section title + helper]
[Primary fields only]
[Primary save action]

Advanced
  [rare or edge-case fields]
```

- Advanced must not hide required fields for the primary path.
- If settings grow past one coherent job, split into multiple sections or routes.

### 2.7 Destructive confirm

Use `AlertDialog`.

```text
Title: plainly name the irreversible action
Body: consequence in one or two short sentences
Actions: Cancel | Confirm
```

Never use a silent icon click for destructive actions.

### 2.8 Empty / first-run coaching

Empty states should contain:

1. one sentence of context
2. one primary next action
3. optional secondary guidance, not a wall of text

### 2.9 Route steps vs Tabs vs disclosure

```text
Is it a gated multi-step sequence?
  YES -> flow / wizard
  NO  -> Are these peer facets of one job?
          YES -> Tabs
          NO  -> Is the detail rare?
                  YES -> Disclosure / Advanced
                  NO  -> Detail route
```

Never use Tabs to glue unrelated jobs onto one page.

### 2.10 Size and split decision tree

```text
Page growing beyond ~350 LOC or taking a second job?
  -> extract sections
  -> move complexity into feature widgets
  -> use nested routes or tabs when depth is real

Single component fetching + rendering + mutation + notifications?
  -> split orchestration from presentation
```

### 2.11 Auth surface (canonical for `Refactor/web`)

**Canonical:** [`../Refactor/web/src/pages/AuthPage.tsx`](../Refactor/web/src/pages/AuthPage.tsx)  
**Brand:** [`../Refactor/web/src/components/brand/BrandMark.tsx`](../Refactor/web/src/components/brand/BrandMark.tsx) (`BrandLockup`, `BrandMark`, `BrandGlyph`)

```text
Full-viewport atmosphere (NexusBackground)
One stage composition (not a lonely centered admin card):
  Brand plane          |  Interaction plane
  BrandLockup hero       Tabs: Entrar | Criar conta
  short support copy     Fields + primary CTA
```

Rules:

- Brand is a hero-level signal (`BrandLockup` size `hero`), never only an eyebrow above a tiny card.
- Auth remains **minimal**: sign-in / sign-up only; no KPI chrome, no dashboard widgets.
- One job per tab; account-type choice is a revealing control inside sign-up.
- Reuse semantic tokens and shadcn primitives; do not invent a second color system for auth.
- Shell chrome after login uses `BrandLockup` size `compact`.
- Prefer `components/brand/*` for mark/lockup — do not duplicate wordmarks ad hoc in pages.

Typography identity for `Refactor/web` (evolving; Dashboard may still use Inter until migrated):

| Role | Face |
|------|------|
| Display / brand | Sora (`font-display`) |
| UI / body | IBM Plex Sans (`font-sans`) |

---

## 3. Complex visualization recipes

### 3.1 Filterable table + row detail

```text
Toolbar: search / a few high-value filters
Table: essential columns only
Row click -> detail route or Sheet
Empty: coaching CTA
```

This is the default operational pattern in the dashboard.

### 3.2 Nested detail with peer facets

```text
Summary / overview
Tabs: peer facets of one entity or workflow
Each tab: focused content only
Technical details last
```

Use when one entity has multiple meaningful aspects.

### 3.3 KPI summary above actionable list

```text
KPI strip
Filter controls
Actionable table/list below
```

Use when collection health or inventory context materially helps prioritization.

### 3.4 Raw payload as last resort

When operators genuinely need raw payloads:

1. structured view first
2. technical detail collapsed below
3. never a full-page `<pre>` as the landing experience

---

## 4. Enrichment catalog

| Situation | Prefer |
|-----------|--------|
| Many entities | Table + filter + detail |
| One entity with several peer facets | Tabs |
| Long form with rare options | Revealing settings + Advanced |
| Dense operational collection | KPI strip + list |
| Destructive action | AlertDialog |
| Quick peek | Sheet |
| Empty surface | Coaching empty state |

If thin CRUD would hide structure, enrich it.

---

## 5. Interaction strip and feedback

Standardize mutations:

| Element | Behavior |
|---------|----------|
| Primary button | Verb-led label; disabled while pending |
| Success | Explicit confirmation |
| Error | Visible, actionable feedback with retry path when sensible |

Prefer in-page feedback for meaningful mutations; do not rely exclusively on ephemeral toasts.

---

## 6. Accessibility notes

- Dialog/Sheet must trap focus and restore it correctly
- Tables need proper header cells
- Tabs need clear labels
- Icon-only controls need accessible names

---

## 7. Where this sits

| Doc | Role |
|-----|------|
| [frontend-standards.md](frontend-standards.md) | Constitution (must / never) |
| **This file** | Recipes and decision trees |
| [architecture-blueprint.md](architecture-blueprint.md) | Backend/domain architecture boundaries |
| [`../Nexus.Dashboard/README.md`](../Nexus.Dashboard/README.md) | Dashboard app usage |
