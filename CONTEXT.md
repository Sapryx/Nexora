# Nexora

A desktop audio player: it loads the user's music library, plays tracks from a playback queue and edits track tags.

## Language

### Playback

**Playback state**:
Whether the player is Stopped, Playing a track or Paused on a track, as the user asked for it. Briefly releasing the playing track's file to save its tags does not change it, and reaching the end of the last track leaves it Paused on that track.
_Avoid_: player status, IsPlaying

**Playing track**:
The track the playback state is Playing or Paused on; there is none while Stopped.
_Avoid_: now playing, current track, active track
