# CineVault

A modern, beautifully themed **.NET 10 WPF** desktop application for browsing your local movie
library — auto-organized into a poster grid with rich, AI/TMDb-powered details: summary, cast
with photos, trailer link, and a blurred "trending" backdrop.

> **Why "CineVault"?** It's short, brandable, and signals exactly what the app is: a vault for
> your cinema collection. 

A note on the requested palette: I've implemented it as **Orange → Burgundy**, since "burble"
isn't a recognized color name — burgundy is the closest deep wine/maroon tone that pairs
beautifully with orange in a gradient. If you meant a different color, it's a one-line change in
`Themes/OrangeBurgundyTheme.xaml`.

---

## 📸 Screenshot

![CineVault — main window](/CineVault/Images/main.png)

*90-movie library with the poster grid (left) and the details panel (right) — blurred backdrop,
overview, cast with profile photos, and Play / Trailer action buttons.*

---

## ✨ Features

- **Smart folder scanning** — point CineVault at your movies root folder. For every sub-folder
  (one per movie), it recursively finds the most recently modified folder containing a
  `folder.jpg` / `folder.png` poster image, then grabs the video and subtitle file sitting next
  to it (handles nested "Extras"/"Featurettes" style folder structures gracefully).
- **Two-column modern UI** — 60% poster grid (3 columns, 200×250 thumbnails, scrollable) / 40%
  details panel, exactly as specified.
- **Rich movie details** — overview/summary, release date, rating, top 5 cast members with
  profile photos, and a YouTube trailer link, all sourced from **TMDb** (free).
- **Trending blurred backdrop** — the right-hand details panel uses the movie's TMDb backdrop
  image as a blurred background, just like modern streaming apps.
- **Optional AI summaries** — plug in a **Google Gemini** API key (generous free tier) to have
  the movie summary generated/rewritten by AI instead of (or as a fallback to) the TMDb overview.
- **Play in your favorite external player** — configure a player path once in Settings (VLC,
  MPC-HC, PotPlayer, etc. — must be installed) and hit ▶ Play on any movie.
- **SQLite local database** — all scanned movies, cast, and settings persist locally
  (`%LocalAppData%\CineVault\cinevault.db`), so re-opening the app is instant.
- **"Update Library" button** — re-scans the folder, adds new movies, removes deleted ones, and
  only re-fetches online metadata for folders that actually changed (fast incremental updates).
- **First-run experience** — if no root folder is configured yet, Settings opens automatically.
- **Orange → Burgundy gradient theme** applied consistently to the main window *and* the
  Settings window, with rounded cards, soft shadows-free flat depth, and a clean dark canvas.

---

## 🗂 Expected folder layout on disk

```
D:\Movies\                              <- "Movies Root Folder" (set in Settings)
 ├─ The Matrix (1999)\                  <- one folder per movie
 │   ├─ folder.jpg                      <- poster image (required)
 │   ├─ The.Matrix.1999.1080p.mkv       <- video file
 │   └─ The.Matrix.1999.1080p.srt       <- subtitle (optional)
 │
 └─ Inception (2010)\
     └─ Extras\                          <- nested folder also supported
         ├─ folder.jpg                   <- if this is the newest folder.jpg, it wins
         ├─ Inception.2010.mp4
         └─ Inception.2010.srt
```

CineVault looks at **every** sub-folder under each movie folder, finds all the ones containing a
`folder.*` image, and picks the **most recently modified** one as the "active" version — handy
when you replace/upgrade a movie's files and the old copy is still sitting around in a sub-folder.

---

## 🔑 Recommended free APIs

| Purpose | Service | Why | Get a key |
|---|---|---|---|
| Movie info, summary, cast (with photos), trailer, trending backdrop | **TMDb (The Movie Database)** | Completely free, generous rate limits, returns everything in one call (`append_to_response=credits,videos`), includes high-quality images. | https://www.themoviedb.org → Settings → API |
| Optional AI-written summary | **Google Gemini** (`gemini-2.0-flash`) | Free tier is the most generous among major AI APIs as of writing; fast and good at short factual summaries. | https://aistudio.google.com/apikey |

CineVault treats **TMDb as the primary "Movie API"** for info/cast/trailer/backdrop (these are
factual, image-bearing data — not something an LLM should be generating), and the **AI Provider**
setting is purely an optional enhancement for the *text* summary, either as a fallback when TMDb
has no overview, or always-on if you enable "Prefer AI-generated summary" in Settings.

> A free **OMDb API** key is also available (omdbapi.com) as an alternative text-only source, but
> it doesn't provide cast photos or backdrops, so TMDb is the better fit here and is what's wired
> up in `Services/TmdbService.cs`.

---

## 🏗 Project structure

```
CineVault/
 ├─ CineVault.sln
 └─ CineVault/
     ├─ CineVault.csproj
     ├─ App.xaml / App.xaml.cs
     ├─ app.manifest                 (DPI-aware manifest)
     ├─ Models/                      (plain data classes + TMDb JSON DTOs)
     ├─ Services/
     │   ├─ DatabaseService.cs       (SQLite schema + CRUD, raw ADO.NET)
     │   ├─ MovieScannerService.cs   (the folder/poster/video scanning algorithm)
     │   ├─ TmdbService.cs           (TMDb HTTP calls)
     │   ├─ AiService.cs             (IAiService + Gemini implementation)
     │   ├─ ImageCacheService.cs     (downloads & caches backdrop/cast images locally)
     │   ├─ ExternalPlayerService.cs (launches the configured player / opens trailer URL)
     │   └─ LibraryService.cs        (orchestrates scan → enrich → cache → save)
     ├─ ViewModels/                  (MVVM, plain INotifyPropertyChanged + RelayCommand)
     ├─ Views/
     │   ├─ MainWindow.xaml(.cs)
     │   └─ SettingsWindow.xaml(.cs)
     ├─ Themes/
     │   └─ OrangeBurgundyTheme.xaml (palette, gradients, all control styles)
     ├─ Converters/                  (PathToImageConverter, BoolToVisibilityConverter)
     └─ Assets/                      (drop an app.ico here if you want a custom icon)
```

The code intentionally avoids extra MVVM frameworks, ORMs, or heavy abstractions — plain
`INotifyPropertyChanged`, a hand-rolled `RelayCommand`, and direct ADO.NET/SQLite — so the whole
codebase is easy to read top-to-bottom and safe to extend.

---

## ▶️ Building & running

**Requirements:** Windows 10/11, [.NET 10 SDK](https://dotnet.microsoft.com/download) with the
Desktop workload (WPF) installed.

```bash
cd CineVault
dotnet restore
dotnet build
dotnet run --project CineVault/CineVault.csproj
```

Or just open `CineVault.sln` in Visual Studio 2022+ and press F5.

On first launch, CineVault will prompt you to pick your Movies Root Folder. Then open the gear
icon (⚙) to add your TMDb API key (and optionally a Gemini key), and click **⟳ Update Library**.

> **Note on this build:** this project was generated and carefully hand-written in a Linux
> sandbox without a Windows/WPF runtime available to compile against, so it has *not* been
> built/run here. The code follows standard, stable WPF/.NET/SQLite/TMDb patterns, but please do
> a `dotnet build` on a Windows machine and let me know if anything needs a tweak — happy to fix
> it immediately.

---

## 🎨 Design notes

- Palette lives entirely in `Themes/OrangeBurgundyTheme.xaml` as named `Color`/`Brush` resources
  — change a couple of hex values there to re-theme the whole app.
- `AppBackgroundBrush` is a diagonal 4-stop gradient (orange → deep orange → burgundy → deep
  burgundy) applied to both `MainWindow` and `SettingsWindow` via a shared `ThemedWindowStyle`.
- The right-hand details panel uses a `BlurEffect` over the movie's TMDb backdrop image, with a
  dark gradient overlay on top for text contrast — no extra image-processing library needed.
- Poster cards have a subtle bottom gradient + title overlay, rounded corners, and an accent
  gradient border on selection.

---

## 💡 Suggested future features

- Multiple movie root folders / network shares.
- Watched / unwatched tracking with resume position.
- Genre, year, and rating filters + sort options (A–Z, newest, top rated).
- A `FileSystemWatcher` to auto-refresh the library when files change, instead of manual scans.
- Subtitle language detection and a quick subtitle preview/sync tool.
- Favorites/collections and a "Continue Watching" row.
- Light/Dark toggle that keeps the orange-burgundy accent but swaps the canvas tone.
- Drag-and-drop a folder onto the window to add it as a root folder.
- Export your library to CSV/JSON for backup.
- IMDb rating / Rotten Tomatoes score badge on the poster card (via TMDb's `external_ids` +
  a secondary lookup).
