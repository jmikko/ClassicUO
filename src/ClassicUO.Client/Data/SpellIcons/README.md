# Spell icons

Drop a PNG in here named after a spell and it replaces that spell's icon in the spellbook.

**This is the client's `Data` folder, not the server's.** Both trees have one, and the server's holds
`DnDSpells.xml` - so it is the natural place to guess. Icons are client-side art and the server
never sees them. Files here are copied into the build output, so they survive a publish; anything
dropped straight into `bin/dist` does not.

    Data/SpellIcons/Fireball.png
    Data/SpellIcons/Magic Missile.png
    Data/SpellIcons/Cure Wounds.png

The name must match the spell exactly as it appears in `Data/DnDSpells.xml` on the server, spaces
and all. Matching is case-insensitive. Anything without a PNG here falls back to Ultima's own art,
so you can add icons a handful at a time rather than all 230 at once.

## What the images should be

- **44 x 44 pixels.** That is the size the book draws them at, and anything else is scaled.
- **PNG with transparency.** The background behind an icon is the book page, not a solid colour.
- No particular palette. These are drawn as loaded, unlike Ultima art, which is hued.

## Where they come from otherwise

Without a PNG, the icon is either hand-picked from Ultima's art (about 170 spells where one
particular image is clearly right - a fireball should look like a fireball) or drawn from a pool
belonging to the spell's school, so Abjuration spells get protective-looking art and Necromancy
spells get grim art.

That pool is 176 icons across Magery, Necromancy, Chivalry, Ninjitsu, Bushido, Spellweaving,
Mysticism and Mastery. Some reuse across 230 spells is unavoidable, but it is now always within a
school rather than arbitrary.

## Checking your work

The client logs what it loaded the first time a spellbook opens:

    Loaded 12 bespoke spell icon(s) from Data/SpellIcons.

A file that fails to load is named individually rather than silently skipped, and the same pass
reports any mapped Ultima icon this installation has no art for.

Icons are cached, so adding a file needs a client restart to show up.

Source lives at `src/ClassicUO.Client/Data/SpellIcons/`; the build copies it to `bin/dist/Data/SpellIcons/`.
