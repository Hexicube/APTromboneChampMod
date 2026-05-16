# Archipelago Trombone Champ Mod

A mod for playing Trombone Champ in Archipelago randomisers.

## APWorld / Manual Client

The APWorld and a Manual Client can be found here: https://github.com/Hexicube/APTromboneChamp/

## Current features

What the mod currently does:

- Indicates the mod is loaded with an AP logo top-left, and if connected (grey vs coloured)
- Connection UI and basic information by pressing F1
- Currently missing items and hinting UI by pressing F2
- Three collections that populate based on the AP server
  - Archipelago: All tracks included in the world, based on your yaml settings
  - Archipelago Checks: All tracks that are both unlocked and have remaining checks
  - Archipelago Locked: All tracks that have an unmet requirement
- Track descriptions are replaced with useful information
  - Goal track is indicated if enabled in yaml
  - Current track status (locked/available/beaten)
  - If unlocked, indicates remaining checks, and their items if hinted
  - If locked, indicates what items are required to unlock it, and where they are if hinted (except for Hot Dogs to prevent excessive text)
- Prevents playing tracks not currently unlocked or outside the AP

What it does not currently do:

- No blocking on turbo/practice mode, these options don't impact rating and can be used to adjust difficulty on the fly
  - Turbo mode is particularly useful if you know beating a track has no useful item, as there's no rating requirement
- No chat feed, so there's no indication of what items were received or sent as they happen
  - The collections and F1/F2 UIs do update in real-time


## TODO

- Add somewhere for chat messages
  - Add errors to this chat
- Make it possible to hint directly from the track list rather than needing a UI
- Show current difficulty unlocks and hot dog progress somewhere on-screen similar to the AP logo
- Notify the player in some way when the goal track becomes available
- Add traps (blind/deaf, out of breath, etc.)