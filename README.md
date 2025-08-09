SSRTMod GitHub Actions build repo
Generated: 2025-08-09T23:18:42.274611 UTC

How to use:
1. Create a new GitHub repository and push this repo (all files) to it.
2. Ensure you have a branch named 'main' or 'master' (workflow triggers on push to main/master).
3. The workflow will attempt to download the latest MelonLoader release and extract MelonLoader.dll into 'libs\' folder. If this fails (some releases have different packaging), you can manually upload MelonLoader.dll into the 'libs' folder in the repo.
4. On push, GitHub Actions will run on windows-latest, build SSRTMod.csproj in Release configuration, and upload the DLL as a workflow artifact named 'SSRTMod-Release'.
5. Download the artifact and place SSRTMod.dll into the game's Mods folder (after installing MelonLoader in the game).

Notes & troubleshooting:
- The workflow uses GitHub API to fetch the latest MelonLoader release and downloads the first asset matching 'MelonLoader' in its name. If the release asset naming changes, the script may not find the DLL automatically.
- If the build fails due to missing UnityEngine references (common for IL2CPP), follow MelonLoader IL2CPP build docs: include Il2CppAssemblyUnhollower or build in a Unity project and produce a DLL via Unity editor. I can help adapt the workflow for Il2CppAssemblyUnhollower if needed.
- If you prefer, upload MelonLoader.dll directly into 'libs/' in the repo before running the workflow.

If you want, I can also create a GitHub repository for you (if you grant access) or guide you step-by-step to push this repo to GitHub.
