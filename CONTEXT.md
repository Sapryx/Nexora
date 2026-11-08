# Nexora

A desktop audio player: it loads the user's music library, plays tracks from a playback queue and edits track tags.

## Language

### Library

**Library**:
The tracks loaded from the music folder at startup, one track per audio file; the library playlist lists them in title order.
_Avoid_: global playlist, collection

**Track**:
One audio file and its tags; every playlist and the artist index share the same track.
_Avoid_: song, audio track item

### Playback

**Playback state**:
Whether the player is Stopped, Playing a track or Paused on a track, as the user asked for it. Briefly releasing the playing track's file to save its tags does not change it, and reaching the end of the last track leaves it Paused on that track.
_Avoid_: player status, IsPlaying

**Playing track**:
The track the playback state is Playing or Paused on; there is none while Stopped.
_Avoid_: now playing, current track, active track

### Tags

**Tag write**:
Saving a track's title, artists or loudness into its audio file. Writes to one file happen one at a time; if it is the playing track's file, the player releases it during the write.
_Avoid_: metadata save, tag update
