# Prototype tile art sources

Generated with the built-in imagegen tool on 2026-09-03. Temporary prototype art, not final commercial art. Original PNGs remain here for reproducible local asset preparation; the Editor directory excludes these authoring sources from players.

`BattlePrototypeBuilder` samples the originals on a 64-pixel grid, projects them into exact 128×64 PNG silhouettes, and adds edge/facing shading. No runtime texture generation or external API is needed to rebuild. Final sprites and Tile assets are under `Assets/_Project/Art/Environment/Tiles/Prototype`.

## GrassTexture.png — final prompt

Use case: stylized-concept. Asset type: seamless source texture for a Unity pixel-art isometric terrain tile. Create ONE flat top-down square grass surface texture, full-bleed without margins, intended to be projected into a 128x64 diamond by an Editor asset generator. Clean readable hand-pixelled tactical RPG prototype art, subtly anime-inspired fantasy grassland. Soft sage and fresh moss greens, small clustered blades, tiny patches of bare warm soil, sparse restrained yellow-green highlights. Texture must be modest and low-contrast so dozens of adjacent tiles form a coherent field. Crisp deliberately placed pixel clusters, apparent source grid around 64x64, nearest-neighbor enlarged, no anti-aliasing or painterly blur, no big objects, no perspective, no tile silhouette, no cast shadow, no border, no text or watermark. Seamless on all edges. Opaque grass texture fills entire square.

## CliffTexture.png — final prompt

Use case: stylized-concept. Asset type: seamless source texture for Unity pixel-art cliff faces. Create ONE flat orthographic square rock-and-earth wall texture filling the entire image. Clean readable hand-pixelled tactical RPG prototype texture, to accompany sage/moss grassland. Muted warm umber soil and cool slate-brown layered rock, small irregular stone clusters and restrained horizontal strata. Clearly darker than a bright grassy top. Moderate low-contrast detail, readable at 64 pixels across. Crisp pixel clusters at apparent 64x64 resolution enlarged by nearest neighbor. Seamless repeating texture on all edges. No grass top surface, no 3D cube or diamond silhouette, no scenery, no objects, no cast shadow, no text, no labels, no watermark, no gradients or blur. Opaque texture fills the entire square.
