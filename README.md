# Steam-Shortcuts
create a shortcuts to run a steam app or game in start menu.

---

## Notes

- This tool is currently Windows-only.

- uninstalling the game wont remove the shortcut unless you re-run this app.

- this app will delete and create `SteamAppShortcuts` folder everytime this app is run.

---

## What does it do?

1. Checks your registry for the Steam install folder.
2. Reads `steamapps/libraryfolders.vdf` and get all installed app ID.
3. For each app ID, now create the shortcuts to `steam://rungameid/{appid}` and set the icon.
4. Done! Note : uninstalling the game wont remove the shortcut unless you re-run this app.
