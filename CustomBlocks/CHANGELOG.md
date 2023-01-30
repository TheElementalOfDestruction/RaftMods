**v4.0.0**
* Dedicated server compatibility! You can now use this on dedicated servers with no issues (none that I could find, anyways).
* Significantly reduced freezing for every operation.
* Fixed lag spikes that would happen on world load (it was worse the more blocks you had from this mod).
* Changed internal code so that clients don't store the full image data all the time even though it only gets used once for them.

**v3.2.0**
* *Finally* fixed the issue causing sails and flags to not flap around as they should.
* *Finally* fixed the issue causing posters to look really messed up.
* Improved some code execution time and memory usage by caching mipmap enabled versions of the default materials.
* Changed shader for some blocks to fix visual bugs.

**v3.1.3**
* Partially fixed some lighting issues with the posters (some issues still exist, but it's a little better).
* Adjusted code to allow original normal maps to be preserved on select blocks.
* Adjusted code so sail and bed normals map are preserved. This should make them look a lot better, although you *may* need to refresh the image if you see issues on the texture.

**v3.1.2**
* Fixed bug that prevented custom sails from working properly as a sail (open, close, and rotate failed to transmit).
* Fixed bug that prevented custom sails from having their textures transmit.

**v3.1.1**
* Finished fixing the memory issue (mod no longer takes multiple gigabytes of memory, should properly free it's resources when it is done with them, textures are no longer all stored in memory all the time, etc.).
* Minor code fixes.

**v3.1.0**
* Fixed a *major* memory issue related to posters (the mod can get a bit memory intensive, but shouldn't cause problems anymore).
* Added a few new poster variants.
* All existing posters have had their resolutions cut in half on each axis, improving memory, speed, and load time for the posters. Made sure that old saves with posters will automatically downsize without clearing the poster data.
* Fixed transparency not working properly.
* Updated code for making the custom block textures, making it faster. This should help with the game freezing.
* Significantly improved load times and game responsiveness during load (it's likely to lag rather than freezing now).
* Fixed the interaction box for posters (including the box to tells the game if they can be placed) not being in the right spots.
* Fixed possible issues for games with more than 2 users.
* Fixed textures not properly being deleted (memory leak).
* Fixed escape causing the text input of the editor window to submit.
* Fixed issue that could cause the editor to throw a (harmless) exception.

**v3.0.0**
* Added 4 new custom blocks: the posters. These are the first blocks that don't use models from the base game.
* Many internal code updates.

**v2.1.1**
* Fixed a bug that caused the curtains to show up using the wrong name.

**v2.1.0**
* Added on option to allow you to choose whether the custom blocks will respect the game's texture settings or not. Default is on and will cause the blocks to use the in-game texture quality. Turning it off will use full texture quality (the default before the update).
* Added a folder to `ModData` (found in the `Mods` folder) called `CustomBlocks` where you can place images. You can then just use the file name (i.e. `myImage.png` instead of typing a full path to the file. The mod will create these folders if they do not already exist. You can also simply place a file next to the `raft.exe` and use it's name too. All other files need a relative or absolute path.

**v2.0.0**
* Changed name (not slug) to Custom Blocks.
* Added 6 new custom blocks: Bed, curtains (2 types), rugs (2 types), and sail.
* Added option to allow multiplayer flags but disable flag editing. This means other people can see your flags (if you are the host) but their changes will not be seen by anyone except them.
* Added option to change the key used for opening the customization menu.
* Added option to entirely disable the image editor (helps with a known bug that causes already interactable blocks to have their messages and the custom blocks messages interfere on the screen to only show one or the other).
* Fixed bug that prevented multiplayer from working *at all*.
* Fixed bug that (presumably) prevented anyone from ever seeing your custom flags, even on world load.
* Ensured that the "Disable Multiplayer Blocks" option would be respected on world load.
* Fixed issues with the flag texture not quite being the right size for the flag.
* Changed save system to use raw color data to protect against malicious save data being transmitted on world load. Old flags will be converted to the new save system and should show up without much issue.
* Fixed a bug that would allow you to duplicate items by removing them before they were actually placed.

**v1.0.1**
* Attempt to patch an issue with the input field causing a NullReferenceException.

**v1.0.0**
* Initial release.
