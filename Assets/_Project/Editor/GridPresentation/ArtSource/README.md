# Prototype block art sources

Generated with the built-in imagegen tool on 2026-09-03 (no CLI/API fallback). These are complete isometric blocks, not flat top/side textures. Prototype validation art, not final commercial art. The Editor directory excludes these high-resolution authoring sources from players.

- GrassBlockSource.png: selected complete block, after geometry and transparency edits.
- GrassBlockVariationSource.png: subtle surface variation of the selected block, same canvas/framing. The generated RGB variant has a black exterior; the Editor tool reuses the base source's genuine alpha silhouette before resizing. A later extraction attempt introduced a glow and was not selected.
- Final saved sprites: Assets/_Project/Art/Environment/Tiles/Prototype/GrassBlock.png and GrassBlockVariation.png, with matching Tile assets and import .meta files.

BattlePrototypeBuilder shares the generated base alpha with the matching variant, crops the alpha bounds and point-resizes each whole block to a 128x96 silhouette on a 128x128 canvas (bottom 32px transparent). No top/side reprojection, procedural recoloring, or runtime textures. Pivot (0.5,0.75) aligns the top diamond center. The source art has small organic pixel-edge deviations; inspect actual adjacent tiles when replacing either source.

## Base generation prompt

Use case: stylized-concept
Asset type: production isometric pixel-art terrain sprite for a Unity mobile tactical RPG.
Primary request: ONE complete grass-and-earth block tile, designed as an isometric asset from the outset, top plus both visible side faces connected in one sprite.
Scene/backdrop: genuinely transparent alpha background, no backdrop, no ground shadow.
Style: clean readable hand-authored pixel art, refined tactical JRPG prototype quality, calm bright sage grass with sparse intentional pixel clusters, ochre earth and a few subtle embedded stone chips. Restrained dark moss/brown one-pixel-style edge definition, top lighter, left side medium, right side darker. Flat buildable grass surface, no props or tufts protruding beyond edges.
Geometry is critical: exact 2:1 diamond top, straight parallel edges, orthographic view, equal left and right halves. Think a native 128x128 pixel square canvas: top vertex (64,0), left corner (0,32), right corner (128,32), front/top corner (64,64). Vertical dirt wall depth 32 pixels. Bottom vertices left (0,64), front (64,96), right (128,64). Thus silhouette is 128 wide and 96 high, with 32 pixels transparent padding BELOW. Output may use a larger integer scale but preserve those proportions and positions precisely.
Composition: one block only, aligned to the entire canvas width, no margin above/left/right, empty transparent bottom quarter.
Constraints: no perspective convergence, no top-down texture sheet, no split components, no sprite sheet, no tall cube, no rounded or broken corners, no bevel shrinking the top surface, no blur, no gradients, no text, no watermark. Tiny low-contrast grass markings so adjacent copies feel coherent and cell boundaries remain legible. Genuine transparent alpha.

## Geometry edit prompt

Use case: precise-object-edit
Input image 1: edit target, the generated complete grass block sprite.
Change only the block's vertical dirt wall depth and canvas placement. Keep the top diamond width-to-height EXACTLY 2:1, its grass palette and pixel-art treatment unchanged. Keep both side faces connected, left lighter than right. Make vertical dirt wall edges exactly HALF the diamond's total height, i.e. one QUARTER of the block width. The current walls are too shallow. Do not change the top diamond slope. This is a short rectangular block, not a tall cube.
Precise normalized SQUARE canvas coordinates: top (50%,0%); left top corner(0%,25%); right top corner(100%,25%); front top corner(50%,50%); bottom left(0%,50%); bottom right(100%,50%); bottom front(50%,75%). Leave bottom25% transparent. One entire block, no exploded parts. Truly transparent alpha background, no shadow, text, sheet, labels, or extra objects. Pixel-art clean straight hard edges.

## Selected base: transparency/geometry correction prompt

Use case: background-extraction
Input image 1: edit target, grass block.
Remove the entire checkerboard background and output the grass block as a genuinely transparent RGBA PNG cutout. Checker squares must NOT be drawn into the image; all exterior pixels must have alpha zero. Keep the block art, palette, three connected faces, hard pixel edges, no halo. No shadow. One complete block sprite, not a texture sheet. Keep rectangular block geometry. Normalize its top diamond to exact 2:1 width/height and walls to exactly 1/4 of the total block WIDTH. Total visible silhouette width to height must be 4:3 (128 wide, 96 high equivalent). Fill a square 128x128-equivalent canvas from y0 through y96, bottom32 pixels transparent; do not add borders.

## Variation edit prompt

Use case: precise-object-edit
Input image 1: edit target, complete isometric grass block sprite.
Create GrassBlockVariation for the same tactical tile set. Change ONLY a few small surface grass pixel clusters and tiny bare-earth patches, keep the top's average color and brightness very close. Keep both dirt/rock sides UNCHANGED. Preserve exactly the original silhouette, all corners, block proportions, framing, palette, pixel-art style, lighting and genuinely transparent alpha background. This is a quiet alternate texture, not a new biome, not more decorative. No props, flowers, raised grass tufts, outlines outside the current silhouette, shadows, labels, text, watermark, or checkerboard background. One complete block sprite only.
