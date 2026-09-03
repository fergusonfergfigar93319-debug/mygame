# Image 2 Art Assets

The seven assets in `Assets/Resources/PenguinRun/StorybookUI/Image2/` were generated with `gpt-image-2` through the project-approved image relay.

| Asset | Runtime use |
| --- | --- |
| `camp_panorama_image2.png` | Main lobby camp panorama (`CampVisualAssetCatalog.Panorama`) |
| `chapter_card_image2.png` | Low-alpha vignette behind the next-chapter card |
| `ice_run_backdrop_image2.png` | Loading page and in-run pause card (`CampVisualAssetCatalog.RunBackdrop`) |
| `result_celebration_image2.png` | Lobby result overlay and in-run death card |
| `codex_boss_gallery_image2.png` | Boss gallery reliefs behind the codex progress banner |
| `shop_supply_board_image2.png` | Top-down expedition supply table behind shop categories |
| `journal_map_image2.png` | Daily journal card and the profile stats sheet |

Generation constraints for all three: premium hand-painted storybook game illustration, arctic penguin adventure setting, no text, no logos, no watermark.

Prompt set:

1. Wide polar explorer camp at blue hour, snowy pines framing warm lantern-lit tents, icy bay and layered mountains, clear center for mascot and UI.
2. Readable three-lane ice runway toward the horizon, snow banks, distant ice cliffs, wind flags and cyan/magenta aurora, uncluttered near lane for gameplay.
3. Turquoise-lit ice cave entrance with lantern trail and carved signposts, snow-dusted rock frame, open low-detail center for chapter UI.
4. Joyful finish line with coral flags, lantern, fish-snack trail, and calm center for score text.
5. Arctic boss gallery with six distinct carved silhouettes around an open parchment progress area.
6. Top-down supply table with maps, compass, tins, potions, fabric, and lantern framing a clean center band.
7. Open explorer journal with route map, compass rose, pine sprig, fish snacks, and wax seal, leaving the left page calm for copy.

The images are imported as Unity Sprites with `Resources.Load`-compatible paths. Existing premium mascot, companion, building atlas, smoke, and glow assets remain unchanged.
