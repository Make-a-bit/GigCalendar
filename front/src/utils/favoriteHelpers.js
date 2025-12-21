/**
 * Check if an event artist is in the favorites list
 * @param {string} eventArtist - The artist name from the event
 * @param {Array<string>} favorites - Array of favorite artist names
 * @returns {boolean} - True if artist is a favorite
 */
export const isFavoriteArtist = (eventArtist, favorites) => {
  if (!eventArtist || !favorites || favorites.length === 0) {
    return false;
  }

  const normalizedEventArtist = eventArtist.trim().toLowerCase();

  return favorites.some((favorite) => {
    const normalizedFavorite = favorite.trim().toLowerCase();
    return normalizedEventArtist.includes(normalizedFavorite);
  });
};

/**
 * Count how many favorite artists have events
 * @param {Array} events - All events
 * @param {Array<string>} favorites - Array of favorite artist names
 * @returns {number} - Count of favorites with events
 */
export const countFavoriteEvents = (events, favorites) => {
  if (!events || !favorites || favorites.length === 0) {
    return 0;
  }

  return events.filter((event) => isFavoriteArtist(event.event_artist, favorites)).length;
};
