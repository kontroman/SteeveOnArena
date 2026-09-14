# Pixel portal

Texture: `Assets/Textures/Materials/Portal/portal_pixel.png`, generated with the built-in image_gen tool. Unity imports it at 64 × 64 with Point filtering, Repeat wrapping, mipmaps and no compression.

`Portal.mat` uses two counter-moving, texel-aligned layers at 12 animation updates per second, subtle violet glow and opacity pulsing. The surface renders on both sides. Material controls include animation speed, distortion, opacity and pixel resolution. The old atlas crop is replaced with full-tile UVs.

The exit window now includes Return. Opening it pauses game time; Return restores the previous time scale and player movement/attacks without applying completion rewards. The wave coroutine remains intact, including its countdown. Portal triggers accept another visit after all player colliders have left. Completion rewards and unlocking the next level occur when the player accepts completion.

Validation: Unity shader compilation and GPU renders at two animation times passed. Runtime/editor C# builds passed. Isolated logic checks cover repeated trigger callbacks, multiple player colliders, re-entry, dead-player rejection, pause/resume, no reward callbacks on return and window-creation failure recovery. Prefab references and localization JSON validated. Full gameplay/UI appearance still requires an in-scene Play Mode check.

## Generation prompt

Use case: stylized-concept. Asset type: seamless square diffuse portal texture for a Minecraft-style voxel game. One flat texture only, edge to edge, no portal frame and no scene. Beautiful Nether-inspired magical violet portal surface: deep plum and royal purple background with interlocking square spiral eddies and broken stepped lavender and magenta currents, luminous lilac accents, darker purple pools. Crisp consistent 64 by 64 logical pixel grid enlarged nearest-neighbor, restrained palette, medium contrast, every curve built from square pixel stair steps. Several small swirling eddies distributed evenly, not one centered vortex. Must tile seamlessly in both directions. Opaque RGB, no transparent holes, no black border, no white highlights, no objects, no text, no perspective, no smooth gradients or blur. Square 1024x1024.
