import glob
import json
import os
import pathlib
import zipfile


DEFAULT_KEY_VALUES = {
    'slug': None,
    'ignoreFiles': [],
    'ignoreFolders': [],
}


def createSettings(jsonDict : dict, modFolder : str) -> dict:
    for x in DEFAULT_KEY_VALUES:
        if x not in jsonDict:
            if DEFAULT_KEY_VALUES[x] is None:
                raise KeyError(f'Setting "{x}" is required, but was not found in "{modFolder}".')
            jsonDict[x] = DEFAULT_KEY_VALUES[x]

    return jsonDict


def main():
    # Find the mods to build and get their settings.
    mods = {}
    for path in pathlib.Path('.').glob('*/build_settings.json'):
        with open(path, 'r') as f:
            mods[path.parent] = createSettings(json.load(f), path.parent.name)

    # Start producing the mods using the build settings.
    for mod in mods:
        settings = mods[mod]
        print(f"Building {mod}");
        # This is used to allow us to cut out the path of the path that is
        # important for the name.
        pathLen = len(mod.parts)
        with zipfile.ZipFile(f'build/{settings["slug"]}.rmod', 'w', zipfile.ZIP_DEFLATED, compresslevel = 4) as zf:
            # Go through each file for the mod.
            for modFileName in mod.glob('**/*'):
                # Skip non-files and the build settings.
                if modFileName.is_file() and modFileName.name != 'build_settings.json':
                    filePath = '/'.join(modFileName.parts[pathLen:])
                    # Starting to use continutes here instead of nesting further
                    # and further.
                    if filePath in settings['ignoreFiles']:
                        continue
                    if any(filePath.startswith(x) for x in settings['ignoreFolders']):
                        continue
                    # Open the file in the zip file.
                    with zf.open(filePath, 'w') as zmf:
                        # Copy the data.
                        zmf.write(modFileName.read_bytes())

    # Create the build folder.
    os.makedirs('build', exist_ok = True)


if __name__ == '__main__':
    main()
