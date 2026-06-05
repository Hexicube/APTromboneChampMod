# Archipelago Trombone Champ Mod

A mod for playing Trombone Champ in Archipelago randomisers.

## APWorld / Manual Client

The APWorld and a Manual Client can be found here: https://github.com/Hexicube/APTromboneChamp/

## Current features

What the mod currently does:

- Indicates the mod is loaded with an AP logo bottom-left, and if connected (grey vs coloured)
- Bottom-left also has a bunch of information icons
  - If goaled, or the goal track is available when using it in settings, none of the following icons appear
  - If Hot Dogs are enabled, shows a hot dog icon (grey if more are required)
  - If Rank Reduction items exist, shows the current rating requirement (grey if not all reductions have been found)
  - If difficulty gating is on, shows what difficulties require an item (grey difficulties are locked)
  - If DeathLink is on (and not set to immediate), shows the current death counter if non-zero
  - Shows received traps in the order they will activate, including the currently active trap
- Connection UI, or basic information and some toggles by pressing F1
- Currently missing items and hinting UI by pressing F2 (unless not connected, in which case the connection UI opens instead)
- Three collections that populate based on the AP server
  - Archipelago: All tracks included in the world, based on your yaml settings
  - Archipelago Checks: All tracks that are both unlocked and have remaining checks
  - Archipelago Locked: All tracks that have an unmet requirement
- Track descriptions are replaced with useful information
  - Goal track is indicated if enabled in yaml
  - Current track status (locked/available/beaten)
  - If unlocked, indicates remaining checks, and their items if hinted
  - If locked, indicates what items are required to unlock it, and where they are if hinted (except for Hot Dogs to prevent excessive text)
- Prevents playing tracks not currently unlocked or outside the AP (disabled after goaling)
- DeathLink support, with separate toggles for inbound and outbound DeathLink
  - Can be enabled in the F1 UI
  - Inbound can be set to Disabled, Enabled, Cumulative, or Immediate
    - When not disabled, any inbound death increases the death counter by 1
    - When set to Enabled, completing a track with a non-zero counter prevents collecting its locations and resets the counter to 0
    - When set to Cumulative, completing a track instead only reduces the counter by 1
    - When set to Immediate, received deaths instead immediately restart the current track (or are ignored if not possible)
  - If outbound DeathLink is enabled, failing to beat a track sends a death, unless DeathLink itself caused this
- Toggle to enable hinting missed locations
  - Can be enabled in the F1 UI
  - When failing to beat a track, the beat track location is hinted
  - If DeathLink blocks a track completion, both play and beat track locations are hinted
  - Tracks must be completed (reach score screen) for this hinting to occur
  - Missed location hinting does not cost hint points

What it does not currently do:

- No blocking on turbo/practice mode, these options don't impact rating and can be used to adjust difficulty on the fly
  - Turbo mode is particularly useful if you know beating a track has no useful item, as there's no rating requirement
  - Turbo mode is also useful if you have inbound DeathLink enabled and need to use up received deaths
- No in-game chat feed, so there's no indication of what items were received or sent as they happen
  - The collections and F1/F2 UIs do update in real-time
  - Chat messages are sent to the console by default


## TODO

- Add somewhere for chat messages
  - Add errors to this chat
- Make it possible to hint directly from the track list rather than needing a UI
- Notify the player in some way when the goal track becomes available
