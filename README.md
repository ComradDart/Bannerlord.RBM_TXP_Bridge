# RBM × TXP Tournament Bridge

Makes **Realistic Battle Mod** and **Tournaments XPanded** work together in Mount & Blade II: Bannerlord.

Normally these two fight over tournaments: RBM's arena module and TXP's prize system patch the same
methods, so running both leaves you with a single prize and a reroll button that does nothing. This
module reconciles them — you keep RBM's arena and RBM's item rules, and you get TXP's prize list,
prize selection and reroll back.

## What you get

- **RBM keeps the arena**: tier-matched opponents, randomised tier/culture gear, scaled renown,
  1v1 variance, lords only in the top tier, companion prizes.
- **RBM keeps the item rules**: every prize offered is still drawn from RBM's tier + town-culture
  filter, and RBM's positive-modifier ("legendary") roll still applies to whatever you win.
- **TXP gets its prize system back**: full prize pool, prize selection, and working reroll.

## Requirements

- Mount & Blade II: Bannerlord (tested on v1.4.7)
- [Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)
- Realistic Battle Mod (tested v4.3.4), with its tournament module **enabled**
- Tournaments XPanded (tested v5.13.15.2)

Both mods are optional at runtime — with either missing, this module loads and does nothing.

## Install

Drop the `Modules/RBM_TXP_Bridge` folder into your Bannerlord directory and enable it in the
launcher, **loaded after both RBM and TournamentsXPanded**.

## Configuration

Optional. Create `RBM_TXP_Bridge.cfg` next to the log at
`Documents/Mount and Blade II Bannerlord/Logs/`:

```ini
# Suppress TXP's "disable RBM" popup, which is wrong once this bridge is active.
SuppressRbmWarning=true

# The core fix. Off means TXP shows one prize and reroll does nothing.
UnpinRbmPrizeForTxp=true

# Fallback: hand the whole prize pipeline to RBM (no pool, no selection, no reroll).
DeferPrizesToRbm=false
```

A diagnostic log is written to `Documents/Mount and Blade II Bannerlord/Logs/RBM_TXP_Bridge.log`.

## How it works

TXP already has a per-tournament compatibility gate (`TournamentCompatibilityService`) used to stand
down for other tournament overhauls, but RBM was never wired into it. That gate is the hook.

The real defect was subtler. TXP builds its prize pool by calling `UpdateTournamentPrize` repeatedly
and harvesting a different prize each time. RBM's prefix short-circuits that call whenever the
current prize is already the correct tier, so every call returned the same item and the pool deduped
down to one entry.

Lifting that short-circuit is safe because RBM's tier enforcement does not live there — it lives in
its `GetTournamentPrize` postfix, which runs on every regeneration regardless. And RBM's
`GivePrizeToWinner` awards `tournament.Prize` as-is rather than re-picking, so the prize you select
is the prize you receive. This module therefore lifts RBM's pin *only* while TXP is harvesting
candidates, and leaves it in place the rest of the time.

Everything is done by reflection against both mods' compiled assemblies. This module contains no
RBM or TXP code.

## Credits

Realistic Battle Mod by Philozoraptor. Tournaments XPanded by brandonm. This is an unofficial
compatibility module and is not affiliated with or endorsed by either author.

## License

MIT — see [LICENSE](LICENSE).
