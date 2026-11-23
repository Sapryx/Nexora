# Cache list and grid cover thumbnails on disk as named PNGs

Reading a cover means parsing the audio file's tags and decoding a full-size image, which is too slow to repeat every
time a cover drops out of the in-memory cache in a library of thousands of tracks. Each track therefore gets two PNG
thumbnails in the OS cache directory, one for the list and one for the grid, named after the audio file and the pixel
size (`Metallica - Sad But True 240x240.png`). A thumbnail is regenerated when its audio file is newer, tracks without
a cover are remembered so their files aren't read again, a background task creates all missing or outdated thumbnails
after the library loads, and thumbnails of tracks no longer in the library are removed.

## Considered Options

- Keying thumbnails by the cover's content hash, so tracks with the same cover share one file: rejected in favor of
  readable names; the music directory is flat, so audio file names are already unique.
- Capping the cache size on disk and evicting the least recently used thumbnails: the background task creates
  thumbnails for the whole library anyway, so removing those of deleted tracks keeps the cache bounded by library size.
- Storing thumbnails in the app data directory: the OS cache directory is where users and the OS expect data that is
  safe to delete.

## Consequences

- The pixel size in the name means each display scale gets its own files, and old ones stay until their track is
  removed.
- Renaming an audio file orphans its thumbnails until the next library load prunes them.
