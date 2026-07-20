# Nexus Mods page copy

Paste into the Nexus description editor. Headings are marked with `[size]`-style
cues you can replace with the editor's own formatting.

---

## Short description (summary field)

Makes Realistic Battle Mod and Tournaments XPanded work together. Keep RBM's arena and item rules,
get TXP's prize list, prize selection and reroll back.

---

## Full description

**Realistic Battle Mod** and **Tournaments XPanded** both want to run your tournaments, and they
patch the same game methods to do it. Run them together and the tournament breaks in a quiet way:
you get a single prize instead of a list, and the reroll button does nothing. The usual advice is to
turn off RBM's arena module and lose its tournament combat entirely.

This is a small compatibility module that lets you keep both.

### What you get

**RBM keeps the arena, exactly as designed**
- Tier calculated from your gear and level
- Opponents matched to your tier
- Randomised tier- and culture-appropriate melee weapons, shields and bows
- Increased renown scaling with tier
- 1v1 fights varying between shielded and unshielded
- Lords only in the highest tier
- Companion wins still deliver the item to your party

**RBM keeps deciding what the items are**
- Every prize offered still comes from RBM's tier + town-culture filter
- RBM's positive-modifier roll (the "legendary" roll) still applies to whatever you win

**Tournaments XPanded gets its prize system back**
- Full prize pool with your configured number of options
- Prize selection — and the prize you pick is the prize you receive
- Working reroll

Betting, leaderboards, achievements, team tournaments and XP tweaks are untouched and continue to
work.

### Requirements

- Mount & Blade II: Bannerlord (built and tested against v1.4.7)
- Harmony
- Realistic Battle Mod, with its **tournament module enabled** in RBM Options
- Tournaments XPanded

Both mods are optional at runtime. If either is missing or RBM's tournament module is switched off,
this module loads and quietly does nothing.

### Installation

Extract into your Bannerlord folder, or install with Vortex. Enable **RBM x TXP Tournament Bridge**
in the launcher and make sure it loads **after** both RBM and Tournaments XPanded.

### Configuration

None required. Optionally create `RBM_TXP_Bridge.cfg` in
`Documents\Mount and Blade II Bannerlord\Logs\`:

```
SuppressRbmWarning=true
UnpinRbmPrizeForTxp=true
DeferPrizesToRbm=false
```

- `SuppressRbmWarning` — hides TXP's "disable RBM" popup, which is no longer accurate once this
  module is running.
- `UnpinRbmPrizeForTxp` — the core fix. Turning it off gives you one prize and a dead reroll button.
- `DeferPrizesToRbm` — fallback that hands the whole prize pipeline to RBM (no pool, no selection,
  no reroll). Only useful if something misbehaves.

A diagnostic log is written to `Documents\Mount and Blade II Bannerlord\Logs\RBM_TXP_Bridge.log`.
Please include it with any bug report.

### How it works, for the curious

TXP builds its prize pool by asking the game to regenerate the tournament prize several times and
collecting the results. RBM's tournament patch short-circuits that regeneration whenever the current
prize is already the correct tier — so every request returned the same item, the pool collapsed to a
single entry, and reroll had nothing to show you.

Lifting that short-circuit is safe, because RBM's tier rules are not enforced there. They live in a
separate patch that runs on every regeneration regardless, so each candidate is still an RBM-legal,
tier- and culture-correct item. And because RBM awards whatever prize is currently set rather than
re-picking its own, the prize you select is the prize you actually receive — with RBM's modifier roll
still applied on top.

So this module lifts RBM's pin only during the moments TXP is collecting candidates, and leaves RBM's
behaviour alone the rest of the time.

It also corrects a small detection bug in TXP, which checked RBM's combat-AI toggle instead of its
tournament toggle when deciding whether to warn you.

### Compatibility and known issues

- Tested on Bannerlord v1.4.7 with RBM v4.3.4 and Tournaments XPanded v5.13.15.2.
- No save-game data is added; safe to add or remove mid-campaign.
- This module contains no RBM or TXP code. It binds to both by reflection, so a future update to
  either mod may change the method names it looks for. If that happens it logs a warning and stops
  rather than breaking your game — please report it.

### Credits and permissions

Realistic Battle Mod by Philozoraptor. Tournaments XPanded by brandonm. All credit for the
tournament features themselves belongs to them.

This is an unofficial compatibility module, not affiliated with or endorsed by either author. It
redistributes no files from either mod. Source is MIT licensed and linked above; if either author
would prefer this handled inside their own mod instead, I am glad to hand the work over.
