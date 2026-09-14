# Pixel water

Texture: `Assets/Textures/Blocks/Grass/water_pixel.png`.
Generated with the built-in image_gen tool. Unity imports the source as a 64 × 64 color texture with Point filtering, Repeat wrapping, mipmaps, and no texture compression.

Both `Water.mat` and `WaterWorld.mat` use this texture. The shader projects ripples in world XZ coordinates (one tile per four world units), keeps the original UV-based shoreline alpha mask, and animates two texel-aligned flows at 12 updates per second. Speed, tile density, ripple amplitude, tint, and opacity remain adjustable on the materials. Animation uses Unity game time and needs no component or Animator.

## Generation prompt

Use case: stylized-concept. Asset type: seamless square tileable diffuse water texture for a Minecraft-style voxel game, not a scene or mockup. Create one beautiful flat top-down pixel-art water tile filling the entire square edge to edge. Crisp uniform square pixel grid, apparent 64 by 64 logical pixels enlarged with nearest-neighbor edges. Rich medium azure blue base, restrained turquoise and lighter cornflower-blue stepped ripple clusters, subtle darker blue patches beneath. Calm luminous clean water, balanced low-to-medium contrast, small scattered broken angular highlights, no large white outlines, no foam, no shore, no objects, no perspective, no lighting gradient, no border, no text. All four edges must tile seamlessly. Opaque RGB color texture; transparency will be controlled by the game shader. Every shape follows the pixel grid; no smooth curves, no blur, no photorealism. Output square 1024x1024.
